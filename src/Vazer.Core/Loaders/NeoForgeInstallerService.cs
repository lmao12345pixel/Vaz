using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vazer.Core.Abstractions;

namespace Vazer.Core.Loaders;

/// <summary>
/// Option B implementation: NeoForge via official Maven + headless installer JAR.
/// </summary>
public sealed class NeoForgeInstallerService : INeoForgeInstallerService
{
    internal const string MavenMetadataUrl =
        "https://maven.neoforged.net/releases/net/neoforged/neoforge/maven-metadata.xml";

    private const string InstallerUrlTemplate =
        "https://maven.neoforged.net/releases/net/neoforged/neoforge/{0}/neoforge-{0}-installer.jar";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NeoForgeInstallerService> _logger;
    private readonly MinecraftOptions _options;
    private readonly IJavaResolver? _javaResolver;

    public NeoForgeInstallerService(
        IHttpClientFactory httpClientFactory,
        ILogger<NeoForgeInstallerService> logger,
        IOptions<MinecraftOptions> options,
        IJavaResolver? javaResolver = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
        _javaResolver = javaResolver;
    }

    /// <summary>
    /// Maps a Minecraft version to its NeoForge version prefix.
    /// NeoForge scheme mirrors MC: 1.20.1 -> "20.1.", 1.21.1 -> "21.1.".
    /// </summary>
    internal static string McToNeoForgePrefix(string minecraftVersion)
    {
        var parts = minecraftVersion.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2
            && parts[0] == "1"
            && int.TryParse(parts[1], out var minor)
            && minor >= 20)
        {
            var patch = 0;
            if (parts.Length >= 3)
            {
                // Strip pre-release suffixes ("1-rc1"); non-numeric -> unsupported.
                var digits = new string(parts[2].TakeWhile(char.IsDigit).ToArray());
                if (!int.TryParse(digits, out patch))
                {
                    throw new NotSupportedException(
                        $"NeoForge does not support Minecraft '{minecraftVersion}' (pre-release/snapshot builds unsupported).");
                }
            }
            return $"{minor}.{patch}.";
        }

        throw new NotSupportedException(
            $"NeoForge does not support Minecraft '{minecraftVersion}'. " +
            "NeoForge requires 1.20.1+ (no snapshots or legacy versions).");
    }

    /// <inheritdoc />
    public async Task<string> ResolveLatestVersionAsync(string minecraftVersion, CancellationToken ct = default)
    {
        var prefix = McToNeoForgePrefix(minecraftVersion);

        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(_options.HttpTimeoutSeconds);
        var xml = await client.GetStringAsync(MavenMetadataUrl, ct);

        var versions = XDocument.Parse(xml)
            .Descendants("version")
            .Select(e => e.Value.Trim())
            .Where(v => v.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        if (versions.Count == 0)
        {
            throw new InvalidOperationException(
                $"No NeoForge build published for Minecraft {minecraftVersion} (prefix '{prefix}').");
        }

        var latest = versions
            .OrderByDescending(v => v, new NumericVersionComparer())
            .First();

        _logger.LogInformation("Resolved NeoForge {Version} for MC {MC}", latest, minecraftVersion);
        return latest;
    }

    /// <inheritdoc />
    public async Task InstallAsync(MinecraftInstance instance, IProgress<InstallProgress>? progress = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(instance.LoaderVersion))
        {
            throw new ArgumentException("Instance LoaderVersion must be set before NeoForge install.", nameof(instance));
        }

        var version = instance.LoaderVersion;
        var javaExe = await ResolveJavaAsync(instance.MinecraftVersion, ct);

        var downloadDir = Path.Combine(_options.RootPath, "java", "downloads");
        Directory.CreateDirectory(downloadDir);
        var installerJar = Path.Combine(downloadDir, $"neoforge-{version}-installer.jar");

        if (!File.Exists(installerJar))
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(10);
            var url = string.Format(InstallerUrlTemplate, version);
            _logger.LogInformation("Downloading NeoForge installer {Version}", version);
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            await using var webStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = File.Create(installerJar);
            await webStream.CopyToAsync(fileStream, ct);
        }

        progress?.Report(new InstallProgress { TotalBytes = 1, DownloadedBytes = 1, Status = "installing" });

        var psi = new ProcessStartInfo
        {
            FileName = javaExe,
            Arguments = $"-jar \"{installerJar}\" --installClient \"{instance.Path}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start NeoForge installer process.");
        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        _logger.LogDebug("NeoForge installer stdout: {Out}", stdout);
        if (process.ExitCode != 0)
        {
            _logger.LogError("NeoForge installer failed: {Err}", stderr);
            throw new InvalidOperationException(
                $"NeoForge installer exited with code {process.ExitCode}. See log for details.");
        }

        // Verify: installer must produce versions/neoforge-<version>/ with a version json.
        var versionsDir = Path.Combine(instance.Path, "versions");
        var versionJson = Directory.Exists(versionsDir)
            ? Directory.GetFiles(versionsDir, "*.json", SearchOption.AllDirectories)
                .FirstOrDefault(f => f.Contains(version, StringComparison.OrdinalIgnoreCase))
            : null;

        if (versionJson == null)
        {
            throw new InvalidOperationException(
                $"NeoForge installer finished but no version json for {version} found under {versionsDir}.");
        }

        progress?.Report(new InstallProgress { TotalBytes = 1, DownloadedBytes = 1, Status = "done" });
        _logger.LogInformation("Installed NeoForge {Version} into {Path}", version, instance.Path);
    }

    private async Task<string> ResolveJavaAsync(string minecraftVersion, CancellationToken ct)
    {
        if (_javaResolver != null)
        {
            var required = _javaResolver.RequiredJava(minecraftVersion);
            var java = await _javaResolver.EnsureAsync(required, null, ct);
            return java.Path;
        }

        // Fallback when no resolver was injected (e.g. default-constructed in InstanceManager):
        // rely on java on PATH with a clear error otherwise.
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim(), "java.exe");
                if (File.Exists(candidate)) return candidate;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Bo qua loi khi quet java.exe trong PATH dir");
            }
        }

        throw new InvalidOperationException(
            "Java not found on PATH. Install a JDK 17+ or provide IJavaResolver to auto-download Temurin.");
    }

    internal sealed class NumericVersionComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            var xs = (x ?? "").Split('.');
            var ys = (y ?? "").Split('.');
            for (var i = 0; i < Math.Max(xs.Length, ys.Length); i++)
            {
                var xv = i < xs.Length && int.TryParse(xs[i], out var a) ? a : 0;
                var yv = i < ys.Length && int.TryParse(ys[i], out var b) ? b : 0;
                var c = xv.CompareTo(yv);
                if (c != 0) return c;
            }
            return 0;
        }
    }
}
