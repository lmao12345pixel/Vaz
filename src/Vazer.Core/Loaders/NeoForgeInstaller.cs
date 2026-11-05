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
/// NeoForge loader qua official Maven + headless installer JAR (CmlLib.Core 4.0.6 không hỗ trợ NeoForge).
/// Implement cả <see cref="INeoForgeInstaller"/> (tương thích InstanceManager/MrpackImporter)
/// và <see cref="ILoaderInstaller"/> (strategy pattern cho InstanceCreationService).
/// </summary>
public sealed class NeoForgeInstaller : INeoForgeInstaller, ILoaderInstaller
{
    internal const string MavenMetadataUrl =
        "https://maven.neoforged.net/releases/net/neoforged/neoforge/maven-metadata.xml";

    private const string InstallerUrlTemplate =
        "https://maven.neoforged.net/releases/net/neoforged/neoforge/{0}/neoforge-{0}-installer.jar";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NeoForgeInstaller> _logger;
    private readonly MinecraftOptions _options;
    private readonly IJavaResolver? _javaResolver;

    public NeoForgeInstaller(
        IHttpClientFactory httpClientFactory,
        ILogger<NeoForgeInstaller> logger,
        IOptions<MinecraftOptions> options,
        IJavaResolver? javaResolver = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
        _javaResolver = javaResolver;
    }

    /// <inheritdoc />
    public string LoaderType => "neoforge";

    /// <inheritdoc />
    public bool SupportsVersion(string mcVersion)
    {
        try
        {
            _ = McToNeoForgePrefix(mcVersion);
            return true;
        }
        catch (NotSupportedException)
        {
            return false;
        }
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

        progress?.Report(new InstallProgress { TotalBytes = 1, DownloadedBytes = 1, Status = "installing" });
        await RunInstallerAsync(instance.Path, instance.MinecraftVersion, instance.LoaderVersion, progress: null, ct);
        progress?.Report(new InstallProgress { TotalBytes = 1, DownloadedBytes = 1, Status = "done" });
    }

    /// <inheritdoc />
    public async Task InstallAsync(
        string instancePath,
        string mcVersion,
        string? loaderVersion,
        IProgress<InstallerProgress>? progress,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        progress?.Report(new InstallerProgress
        {
            Stage = InstallerStage.Preparing,
            Percent = -1,
            Message = "Đang chuẩn bị cài NeoForge..."
        });

        var version = string.IsNullOrWhiteSpace(loaderVersion)
            ? await ResolveLatestVersionAsync(mcVersion, ct)
            : loaderVersion;

        await RunInstallerAsync(instancePath, mcVersion, version, progress, ct);

        progress?.Report(new InstallerProgress
        {
            Stage = InstallerStage.Done,
            Percent = 100,
            Message = $"Hoàn tất cài NeoForge {version}"
        });
    }

    /// <summary>
    /// Logic cài chung: tải installer jar (nếu chưa có), đảm bảo launcher_profiles.json,
    /// chạy headless installer, xác minh version json được tạo.
    /// </summary>
    private async Task RunInstallerAsync(
        string instancePath,
        string mcVersion,
        string version,
        IProgress<InstallerProgress>? progress,
        CancellationToken ct)
    {
        var javaExe = await ResolveJavaAsync(mcVersion, ct);

        var downloadDir = Path.Combine(_options.RootPath, "java", "downloads");
        Directory.CreateDirectory(downloadDir);
        var installerJar = Path.Combine(downloadDir, $"neoforge-{version}-installer.jar");

        if (!File.Exists(installerJar))
        {
            progress?.Report(new InstallerProgress
            {
                Stage = InstallerStage.Downloading,
                Percent = -1,
                Message = $"Đang tải NeoForge installer {version}..."
            });
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

        progress?.Report(new InstallerProgress
        {
            Stage = InstallerStage.Installing,
            Percent = -1,
            Message = $"Đang cài NeoForge {version} (có thể mất vài phút)..."
        });

        // NeoForge installer BẮT BUỘC phải có launcher_profiles.json trong thư mục game,
        // nếu thiếu -> in "There is no minecraft launcher profile in ..." và exit code 1.
        // Tạo file tối thiểu (không ghi đè nếu đã có).
        EnsureLauncherProfiles(instancePath);

        var psi = new ProcessStartInfo
        {
            FileName = javaExe,
            Arguments = $"-jar \"{installerJar}\" --installClient \"{instancePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            // Installer ghi log ra file {jar}.log theo CWD — đặt CWD cạnh jar để path đoán được.
            WorkingDirectory = downloadDir
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start NeoForge installer process.");
        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        _logger.LogDebug("NeoForge installer stdout: {Out}", stdout);
        if (process.ExitCode != 0)
        {
            var reason = ExtractInstallerError(stdout, stderr);
            var logPath = installerJar + ".log";
            _logger.LogError("NeoForge installer failed (exit {Code}): {Reason}. Log: {Log}", process.ExitCode, reason, logPath);
            throw new InvalidOperationException(
                $"Không thể cài đặt NeoForge. {reason} Chi tiết: {logPath}. Vui lòng kiểm tra Java 21 đã được cài đặt.");
        }

        progress?.Report(new InstallerProgress
        {
            Stage = InstallerStage.Finalizing,
            Percent = -1,
            Message = "Đang xác minh kết quả cài đặt..."
        });

        // Verify: installer must produce versions/neoforge-<version>/ with a version json.
        var versionsDir = Path.Combine(instancePath, "versions");
        var versionJson = Directory.Exists(versionsDir)
            ? Directory.GetFiles(versionsDir, "*.json", SearchOption.AllDirectories)
                .FirstOrDefault(f => f.Contains(version, StringComparison.OrdinalIgnoreCase))
            : null;

        if (versionJson == null)
        {
            throw new InvalidOperationException(
                $"NeoForge installer finished but no version json for {version} found under {versionsDir}.");
        }

        _logger.LogInformation("Installed NeoForge {Version} into {Path}", version, instancePath);
    }

    /// <summary>
    /// Tạo launcher_profiles.json tối thiểu trong thư mục game nếu chưa có.
    /// NeoForge installer (headless, --installClient) yêu cầu file này; thiếu -> exit code 1
    /// với thông điệp "There is no minecraft launcher profile in &lt;dir&gt;".
    /// Không ghi đè file đã tồn tại. Trả về true nếu đã tạo mới.
    /// </summary>
    internal static bool EnsureLauncherProfiles(string gameDir)
    {
        var path = Path.Combine(gameDir, "launcher_profiles.json");
        if (File.Exists(path))
        {
            return false;
        }

        Directory.CreateDirectory(gameDir);
        File.WriteAllText(path, """{"profiles":{},"settings":{},"version":3}""");
        return true;
    }

    /// <summary>
    /// Lấy dòng lỗi "đáng chú ý nhất" từ output của installer để hiển thị cho user.
    /// Ưu tiên dòng chứa từ khóa lỗi; fallback về dòng cuối.
    /// </summary>
    internal static string ExtractInstallerError(string? stdout, string? stderr)
    {
        var lines = ((stdout ?? string.Empty) + "\n" + (stderr ?? string.Empty))
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        if (lines.Count == 0)
        {
            return "Installer không in ra thông tin lỗi.";
        }

        var keyLine = lines.LastOrDefault(l =>
            l.Contains("error", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("no minecraft launcher profile", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("exception", StringComparison.OrdinalIgnoreCase) ||
            l.Contains("failed", StringComparison.OrdinalIgnoreCase));

        return keyLine ?? lines[^1];
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
