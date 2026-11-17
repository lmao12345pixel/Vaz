using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vazer.Core.Abstractions;
using Vazer.Core.Instances;
using Vazer.Core.Json;

namespace Vazer.Core.Loaders;

/// <summary>
/// Delegate thực thi process để có thể unit-test / mock lệnh java -jar.
/// </summary>
public delegate Task<ProcessRunResult> ProcessRunnerFunc(ProcessStartInfo psi, CancellationToken ct);

/// <summary>
/// Kết quả chạy process.
/// </summary>
public sealed record ProcessRunResult(int ExitCode, string Stdout, string Stderr);

/// <summary>
/// Cài đặt Minecraft Forge qua official Maven installer JAR.
/// Forge installer hỗ trợ headless `--installClient` từ Minecraft 1.13+.
/// </summary>
public sealed class ForgeInstaller : ILoaderInstaller
{
    private const string PromotionsUrl =
        "https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json";

    private const string InstallerUrlTemplate =
        "https://maven.minecraftforge.net/net/minecraftforge/forge/{0}/forge-{0}-installer.jar";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ForgeInstaller> _logger;
    private readonly MinecraftOptions _options;
    private readonly IJavaResolver? _javaResolver;
    private readonly ProcessRunnerFunc _processRunner;

    public string LoaderType => "forge";

    /// <summary>Khởi tạo ForgeInstaller.</summary>
    /// <param name="httpClientFactory">Factory tạo HTTP client cho download installer.</param>
    /// <param name="logger">Logger cho installer.</param>
    /// <param name="options">Cấu hình Minecraft options.</param>
    /// <param name="javaResolver">Nullable: khi null, installer tự động fallback quét Java runtime từ biến môi trường PATH hệ thống.</param>
    /// <param name="processRunner">Nullable: process runner function tùy chỉnh hoặc mock cho unit test.</param>
    public ForgeInstaller(
        IHttpClientFactory httpClientFactory,
        ILogger<ForgeInstaller> logger,
        IOptions<MinecraftOptions> options,
        IJavaResolver? javaResolver = null,
        ProcessRunnerFunc? processRunner = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
        _javaResolver = javaResolver;
        _processRunner = processRunner ?? DefaultProcessRunnerAsync;
    }

    /// <inheritdoc />
    public bool SupportsVersion(string mcVersion) => IsHeadlessSupported(mcVersion);

    /// <summary>
    /// Kiểm tra phiên bản Minecraft có hỗ trợ cài Forge headless (--installClient) hay không.
    /// Forge chỉ hỗ trợ headless từ Minecraft 1.13 trở lên.
    /// </summary>
    public static bool IsHeadlessSupported(string minecraftVersion)
    {
        if (string.IsNullOrWhiteSpace(minecraftVersion)) return false;
        var parts = minecraftVersion.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        if (parts[0] == "1")
        {
            if (parts.Length >= 2 && int.TryParse(parts[1], out var minor))
            {
                return minor >= 13;
            }
            return false;
        }

        if (int.TryParse(parts[0], out var major) && major >= 2)
        {
            return true;
        }

        return false;
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

        if (!IsHeadlessSupported(mcVersion))
        {
            throw new NotSupportedException(
                $"Forge headless chỉ hỗ trợ Minecraft 1.13+. Các phiên bản cũ hơn ({mcVersion}) cần cài đặt thủ công.");
        }

        progress?.Report(new InstallerProgress
        {
            Stage = InstallerStage.Preparing,
            Percent = -1,
            Message = "Đang chuẩn bị cài Forge..."
        });

        var version = string.IsNullOrWhiteSpace(loaderVersion)
            ? await ResolveLatestVersionAsync(mcVersion, ct)
            : loaderVersion;

        var fullVersion = version.StartsWith(mcVersion + "-", StringComparison.OrdinalIgnoreCase)
            ? version
            : $"{mcVersion}-{version}";

        var javaExe = await ResolveJavaAsync(mcVersion, ct);

        var downloadDir = Path.Combine(_options.RootPath, "java", "downloads");
        Directory.CreateDirectory(downloadDir);
        var installerJar = Path.Combine(downloadDir, $"forge-{fullVersion}-installer.jar");

        if (!File.Exists(installerJar))
        {
            progress?.Report(new InstallerProgress
            {
                Stage = InstallerStage.Downloading,
                Percent = -1,
                Message = $"Đang tải Forge installer {fullVersion}..."
            });
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(10);
            var url = string.Format(InstallerUrlTemplate, fullVersion);
            _logger.LogInformation("Downloading Forge installer {Version} from {Url}", fullVersion, url);
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
            Message = $"Đang cài Forge {fullVersion} (có thể mất vài phút)..."
        });

        EnsureLauncherProfiles(instancePath);

        var psi = new ProcessStartInfo
        {
            FileName = javaExe,
            Arguments = $"-jar \"{installerJar}\" --installClient \"{instancePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = downloadDir
        };

        var result = await _processRunner(psi, ct);

        _logger.LogDebug("Forge installer stdout: {Out}", result.Stdout);
        if (result.ExitCode != 0)
        {
            var reason = ExtractInstallerError(result.Stdout, result.Stderr);
            var logPath = installerJar + ".log";
            _logger.LogError("Forge installer failed (exit {Code}): {Reason}. Log: {Log}", result.ExitCode, reason, logPath);
            throw new InvalidOperationException(
                $"Không thể cài đặt Forge. {reason} Chi tiết: {logPath}. Vui lòng kiểm tra Java đã được cài đặt.");
        }

        progress?.Report(new InstallerProgress
        {
            Stage = InstallerStage.Finalizing,
            Percent = -1,
            Message = "Đang xác minh kết quả cài đặt..."
        });

        var versionsDir = Path.Combine(instancePath, "versions");
        var versionJson = Directory.Exists(versionsDir)
            ? Directory.GetFiles(versionsDir, "*.json", SearchOption.AllDirectories)
                .FirstOrDefault(f => f.Contains("forge", StringComparison.OrdinalIgnoreCase))
            : null;

        if (versionJson == null)
        {
            throw new InvalidOperationException(
                $"Forge installer finished but no version json found under {versionsDir}.");
        }

        // Tạo instance.json nếu chưa có (khi gọi độc lập ngoài InstanceCreationService)
        var metaPath = Path.Combine(instancePath, "instance.json");
        if (!File.Exists(metaPath))
        {
            var instanceId = Path.GetFileName(instancePath);
            var launchVersionName = InstanceManager.DiscoverVersionName(instancePath);
            var instance = new MinecraftInstance
            {
                Id = instanceId,
                Name = instanceId,
                MinecraftVersion = mcVersion,
                LoaderType = "forge",
                LoaderVersion = fullVersion,
                LaunchVersionName = launchVersionName,
                Path = instancePath,
                CreatedAt = DateTimeOffset.UtcNow
            };
            var metaJson = JsonSerializer.Serialize(instance, VazJsonContext.CamelCase.MinecraftInstance);
            await File.WriteAllTextAsync(metaPath, metaJson, ct);
        }

        progress?.Report(new InstallerProgress
        {
            Stage = InstallerStage.Done,
            Percent = 100,
            Message = $"Hoàn tất cài Forge {fullVersion}"
        });

        _logger.LogInformation("Installed Forge {Version} into {Path}", fullVersion, instancePath);
    }

    public async Task<string> ResolveLatestVersionAsync(string mcVersion, CancellationToken ct = default)
    {
        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(_options.HttpTimeoutSeconds);
        var json = await client.GetStringAsync(PromotionsUrl, ct);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("promos", out var promosEl))
        {
            throw new InvalidOperationException("Forge promotions JSON missing 'promos' object");
        }

        var recKey = $"{mcVersion}-recommended";
        var latestKey = $"{mcVersion}-latest";

        if (promosEl.TryGetProperty(recKey, out var recProp) && recProp.ValueKind == JsonValueKind.String)
        {
            var v = recProp.GetString();
            if (!string.IsNullOrWhiteSpace(v)) return $"{mcVersion}-{v}";
        }

        if (promosEl.TryGetProperty(latestKey, out var latestProp) && latestProp.ValueKind == JsonValueKind.String)
        {
            var v = latestProp.GetString();
            if (!string.IsNullOrWhiteSpace(v)) return $"{mcVersion}-{v}";
        }

        throw new InvalidOperationException($"Không tìm thấy phiên bản Forge cho Minecraft {mcVersion}.");
    }

    internal static bool EnsureLauncherProfiles(string gameDir)
    {
        return NeoForgeInstaller.EnsureLauncherProfiles(gameDir);
    }

    internal static string ExtractInstallerError(string? stdout, string? stderr)
    {
        return NeoForgeInstaller.ExtractInstallerError(stdout, stderr);
    }

    private static async Task<ProcessRunResult> DefaultProcessRunnerAsync(ProcessStartInfo psi, CancellationToken ct)
    {
        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start Forge installer process.");
        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return new ProcessRunResult(process.ExitCode, stdout, stderr);
    }

    private async Task<string> ResolveJavaAsync(string minecraftVersion, CancellationToken ct)
    {
        if (_javaResolver != null)
        {
            var required = _javaResolver.RequiredJava(minecraftVersion);
            var java = await _javaResolver.EnsureAsync(required, null, ct);
            return java.Path;
        }

        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim(), "java.exe");
                if (File.Exists(candidate)) return candidate;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Bỏ qua lỗi khi quét java.exe trong PATH dir");
            }
        }

        throw new InvalidOperationException(
            "Java not found on PATH. Install a JDK 17+ or provide IJavaResolver to auto-download Temurin.");
    }
}
