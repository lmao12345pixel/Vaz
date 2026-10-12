using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vazer.Core.Abstractions;

namespace Vazer.Core.Java;

/// <summary>
/// Java resolver with Adoptium Temurin auto-download.
/// </summary>
public sealed class JavaResolver : IJavaResolver
{
    private readonly ILogger<JavaResolver> _logger;
    private readonly MinecraftOptions _options;
    private readonly IAdoptiumClient _adoptium;

    public JavaResolver(
        IHttpClientFactory httpClientFactory,
        ILogger<JavaResolver> logger,
        IOptions<MinecraftOptions> options)
        : this(
            new AdoptiumClient(
                httpClientFactory,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<AdoptiumClient>.Instance,
                options),
            logger,
            options)
    {
    }

    internal JavaResolver(
        IAdoptiumClient adoptium,
        ILogger<JavaResolver> logger,
        IOptions<MinecraftOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _adoptium = adoptium;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JavaInstallation>> DiscoverAsync(CancellationToken ct = default)
    {
        var results = new List<JavaInstallation>();

        // 1. Check bundled Java
        var bundledDir = Path.Combine(_options.RootPath, "java");
        if (Directory.Exists(bundledDir))
        {
            foreach (var dir in Directory.GetDirectories(bundledDir))
            {
                var javaExe = Path.Combine(dir, "bin", "java.exe");
                if (File.Exists(javaExe))
                {
                    var probed = await TryProbeAsync(javaExe, ct);
                    if (probed != null)
                    {
                        results.Add(probed with { Source = "bundled" });
                    }
                }
            }
        }

        // 2. Check system PATH
        var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [];
        foreach (var pathDir in pathDirs)
        {
            try
            {
                var javaExe = Path.Combine(pathDir, "java.exe");
                if (File.Exists(javaExe))
                {
                    var probed = await TryProbeAsync(javaExe, ct);
                    if (probed != null)
                    {
                        results.Add(probed with { Source = "system" });
                    }
                }
            }
            catch { }
        }

        // 3. Check common install locations
        var commonPaths = new[]
        {
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Eclipse Adoptium"),
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Java"),
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Java"),
            Environment.ExpandEnvironmentVariables(@"%LocalAppData%\Programs\Eclipse Adoptium"),
        };

        foreach (var commonPath in commonPaths)
        {
            if (Directory.Exists(commonPath))
            {
                foreach (var dir in Directory.GetDirectories(commonPath, "*", SearchOption.AllDirectories))
                {
                    var javaExe = Path.Combine(dir, "bin", "java.exe");
                    if (File.Exists(javaExe))
                    {
                        var probed = await TryProbeAsync(javaExe, ct);
                        if (probed != null)
                        {
                            results.Add(probed with { Source = "system" });
                        }
                    }
                }
            }
        }

        // Deduplicate by path
        return results
            .GroupBy(j => j.Path, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Where(j => j.MajorVersion >= _options.MinJavaVersion && j.MajorVersion <= _options.MaxJavaVersion)
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<JavaInstallation> EnsureAsync(int requiredMajor, IProgress<InstallProgress>? progress = null, CancellationToken ct = default)
    {
        if (requiredMajor < _options.MinJavaVersion || requiredMajor > _options.MaxJavaVersion)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredMajor), $"Java version must be between {_options.MinJavaVersion} and {_options.MaxJavaVersion}");
        }

        // Step 1: Check bundled first
        var bundledPath = Path.Combine(_options.RootPath, "java", $"temurin-{requiredMajor}-x64", "bin", "java.exe");
        if (File.Exists(bundledPath))
        {
            var probed = await TryProbeAsync(bundledPath, ct);
            if (probed != null && probed.MajorVersion == requiredMajor)
            {
                _logger.LogInformation("Found bundled Java {Major} at {Path}", requiredMajor, bundledPath);
                return probed with { Source = "bundled" };
            }
        }

        // Step 2: Query Adoptium API
        _logger.LogInformation("Querying Adoptium API for Java {Major}", requiredMajor);
        var asset = await _adoptium.GetLatestJdkAsync(requiredMajor, ct);

        // Step 3: Download
        var downloadDir = Path.Combine(_options.RootPath, "java", "downloads");
        Directory.CreateDirectory(downloadDir);
        var zipPath = Path.Combine(downloadDir, $"temurin-{requiredMajor}.zip");
        var partialPath = zipPath + ".partial";

        await _adoptium.DownloadFileAsync(asset.DownloadUrl, partialPath, progress, ct);

        // Step 4: Verify and rename
        if (!VerifyChecksum(partialPath, asset.Sha256Checksum))
        {
            File.Delete(partialPath);
            throw new InvalidDataException("Checksum mismatch after download");
        }

        File.Move(partialPath, zipPath, true);

        // Step 5: Extract
        var extractDir = Path.Combine(_options.RootPath, "java", $"temurin-{requiredMajor}-x64.tmp");
        var finalDir = Path.Combine(_options.RootPath, "java", $"temurin-{requiredMajor}-x64");

        if (Directory.Exists(finalDir))
        {
            Directory.Delete(finalDir, true);
        }
        if (Directory.Exists(extractDir))
        {
            Directory.Delete(extractDir, true);
        }

        Directory.CreateDirectory(extractDir);
        try
        {
            ZipFile.ExtractToDirectory(zipPath, extractDir);
        }
        catch
        {
            // Corrupt archive must not leave a .tmp dir behind.
            try { Directory.Delete(extractDir, true); } catch { }
            throw;
        }

        // Adoptium zip has single root folder (jdk-XX.Y.Z+W), move contents up
        var subDirs = Directory.GetDirectories(extractDir);
        if (subDirs.Length == 1)
        {
            var rootDir = subDirs[0];
            foreach (var item in Directory.GetFileSystemEntries(rootDir))
            {
                var dest = Path.Combine(extractDir, Path.GetFileName(item));
                if (Directory.Exists(item))
                {
                    MoveDirectory(item, dest);
                }
                else
                {
                    File.Move(item, dest);
                }
            }
            Directory.Delete(rootDir, true);
        }

        Directory.Move(extractDir, finalDir);

        // Cleanup zip
        File.Delete(zipPath);

        // Step 6: Probe final
        var finalJavaExe = Path.Combine(finalDir, "bin", "java.exe");
        var probedResult = await TryProbeAsync(finalJavaExe, ct);
        if (probedResult == null || probedResult.MajorVersion != requiredMajor)
        {
            if (Directory.Exists(finalDir))
            {
                Directory.Delete(finalDir, true);
            }
            throw new InvalidOperationException($"Downloaded Java {requiredMajor} failed probe");
        }

        _logger.LogInformation("Successfully installed Java {Major} from Adoptium", requiredMajor);
        return probedResult with { Source = "adoptium" };
    }

    /// <inheritdoc />
    public JavaInstallation? SelectBest(string minecraftVersion, IReadOnlyList<JavaInstallation> available)
    {
        var required = RequiredJava(minecraftVersion);
        _logger.LogDebug("SelectBest: MC={MC}, RequiredJava={Required}, Available={Count}", minecraftVersion, required, available.Count);

        // Filter compatible
        var compatible = available.Where(j => j.MajorVersion >= required).ToList();
        if (compatible.Count == 0)
        {
            return null;
        }

        // Prefer: exact match > higher version > bundled > adoptium > system
        return compatible
            .OrderBy(j => j.MajorVersion == required ? 0 : 1)  // exact match first
            .ThenBy(j => j.MajorVersion)                        // lower version first (closer to required)
            .ThenBy(j => j.Source == "bundled" ? 0 : j.Source == "adoptium" ? 1 : 2)
            .First();
    }

    /// <inheritdoc />
    public int RequiredJava(string minecraftVersion)
    {
        // Parse version
        var parts = minecraftVersion.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return 17; // default

        var major = int.TryParse(parts[0], out var m) ? m : 1;
        var minor = int.TryParse(parts[1], out var n) ? n : 0;

        // Version -> Java mapping
        return (major, minor) switch
        {
            (1, <= 16) => 8,    // 1.8 - 1.16
            (1, 17) => 16,      // 1.17
            (1, 18) => 17,      // 1.18
            (1, >= 19 and <= 20) => 17, // 1.19 - 1.20
            (1, >= 21) => 21,   // 1.21+
            _ => 17
        };
    }

    /// <inheritdoc />
    public async Task<JavaInstallation?> TryProbeAsync(string javaPath, CancellationToken ct = default)
    {
        if (!IsValidPeExecutable(javaPath)) return null;
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = javaPath,
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return null;

            var outputTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(ct);
            var output = await outputTask;

            // Parse version from stderr (java -version outputs to stderr)
            var lines = output.Split('\n');
            var versionLine = lines.FirstOrDefault(l => l.Contains("version"));
            if (versionLine == null) return null;

            var versionMatch = System.Text.RegularExpressions.Regex.Match(versionLine, @"version\s+""([^""]+)""");
            if (!versionMatch.Success) return null;

            var versionStr = versionMatch.Groups[1].Value;
            var vendor = "Unknown";
            if (output.Contains("Temurin", StringComparison.OrdinalIgnoreCase)) vendor = "Eclipse Temurin";
            else if (output.Contains("AdoptOpenJDK", StringComparison.OrdinalIgnoreCase)) vendor = "AdoptOpenJDK";
            else if (output.Contains("Oracle", StringComparison.OrdinalIgnoreCase)) vendor = "Oracle";
            else if (output.Contains("Azul", StringComparison.OrdinalIgnoreCase)) vendor = "Azul";

            var majorVersion = ParseMajorVersion(versionStr);

            return new JavaInstallation
            {
                Path = javaPath,
                MajorVersion = majorVersion,
                Version = versionStr,
                Vendor = vendor,
                Architecture = Environment.Is64BitProcess ? "x64" : "x86",
                IsJdk = File.Exists(Path.Combine(Path.GetDirectoryName(javaPath)!, "javac.exe")),
                Source = "unknown"
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Probe failed for {Path}", javaPath);
            return null;
        }
    }

    private bool VerifyChecksum(string filePath, string expectedChecksum)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        var actual = Convert.ToHexString(hash).ToLowerInvariant();
        return actual == expectedChecksum.ToLowerInvariant();
    }

    private void MoveDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
        {
            File.Move(file, Path.Combine(destination, Path.GetFileName(file)), true);
        }
        foreach (var dir in Directory.GetDirectories(source))
        {
            MoveDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
        }
        Directory.Delete(source, true);
    }

    private int ParseMajorVersion(string versionStr)
    {
        var parts = versionStr.Split('.', '_');
        if (parts.Length >= 2 && parts[0] == "1")
        {
            return int.TryParse(parts[1], out var minor) ? minor : 8;
        }
        return int.TryParse(parts[0], out var major) ? major : 0;
    }

    private static bool IsValidPeExecutable(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            if (fs.Length < 64) return false;
            Span<byte> dos = stackalloc byte[2];
            if (fs.Read(dos) != 2) return false;
            if (dos[0] != 0x4D || dos[1] != 0x5A) return false;
            fs.Seek(0x3C, SeekOrigin.Begin);
            Span<byte> lfanew = stackalloc byte[4];
            if (fs.Read(lfanew) != 4) return false;
            int peOffset = BitConverter.ToInt32(lfanew);
            if (peOffset < 0 || peOffset > fs.Length - 4) return false;
            fs.Seek(peOffset, SeekOrigin.Begin);
            Span<byte> pe = stackalloc byte[4];
            if (fs.Read(pe) != 4) return false;
            return pe[0] == 0x50 && pe[1] == 0x45 && pe[2] == 0 && pe[3] == 0;
        }
        catch { return false; }
    }
}

