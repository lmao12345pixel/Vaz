using System.Diagnostics;
using System.Threading;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vazer.Core.Abstractions;

namespace Vazer.Core.Launch;

/// <summary>
/// Process wrapper for CmlLib process.
/// </summary>
internal sealed class CmlProcessHandle : IProcessHandle
{
    private readonly Process _process;
    private bool _disposed;

    public CmlProcessHandle(Process process)
    {
        _process = process;
    }

    public int Id => _process.Id;

    public bool HasExited => _process.HasExited;

    public int ExitCode => _process.HasExited ? _process.ExitCode : throw new InvalidOperationException("Process has not exited");

    public async Task<int> WaitForExitAsync(CancellationToken ct = default)
    {
        try
        {
            await _process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Caller huy wait nhung process co the van dang chay: tra -1 theo hop dong
            // (HasExited=false). Khong log duoc: handle nay khong giu ILogger de tranh
            // phu thuoc thua trong lop wrapper mong.
        }
        return _process.HasExited ? _process.ExitCode : -1;
    }

    public void Kill()
    {
        if (!_process.HasExited)
        {
            _process.Kill(true);
        }
    }

    /// <summary>
    /// Giải phóng Process handle. Không kill process đang chạy -
    /// gọi <see cref="Kill"/> trước nếu muốn dừng game.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _process.Dispose();
    }
}

/// <summary>
/// Game launcher using CmlLib.Core.
/// </summary>
public sealed class CmlGameLauncher : IGameLauncher
{
    private readonly ILogger<CmlGameLauncher> _logger;
    private readonly MinecraftOptions _options;
    private readonly IJavaResolver _javaResolver;
    private readonly MinecraftLauncher _launcher;

    public CmlGameLauncher(
        ILogger<CmlGameLauncher> logger,
        IOptions<MinecraftOptions> options,
        IJavaResolver javaResolver)
    {
        _logger = logger;
        _options = options.Value;
        _javaResolver = javaResolver;
        _launcher = new MinecraftLauncher(new MinecraftPath(Path.Combine(_options.RootPath, "instances")));
    }

    /// <inheritdoc />
    public async Task<LaunchResult> LaunchAsync(LaunchParameters parameters, CancellationToken ct = default)
    {
        _logger.LogInformation("Launching instance {InstanceId} with account {Account}", parameters.InstanceId, parameters.Account.DisplayName);

        // CmlLib tra version theo TÊN VERSION, không phải instance id. Dùng
        // LaunchVersionName của instance; sai tên này gây lỗi
        // "Cannot find {id}" khi bấm Chơi (instance cũ trước 11/2026).
        if (string.IsNullOrEmpty(parameters.LaunchVersionName))
        {
            throw new InvalidOperationException(
                $"Instance {parameters.InstanceId}: chưa xác định được tên version để khởi chạy (LaunchVersionName trống). Mở lại instance hoặc tạo instance mới để tự động lưu tên version.");
        }

        // NEOFORGE/OPTIFINE v.v. kế thừa vanilla base — đảm bảo version gốc
        // đã có ở root trước khi merge (no-op nếu đã cài).
        if (!string.IsNullOrEmpty(parameters.MinecraftVersion) &&
            parameters.LaunchVersionName != parameters.MinecraftVersion)
        {
            await InstallWithRetryAsync(parameters.MinecraftVersion, ct);
        }

        // Tra version theo tên (đã migrate nếu thiếu) rồi mới resolve object.
        if (!await EnsureVersionResolvableAsync(parameters, ct))
        {
            throw new InvalidOperationException(
                $"Không tìm thấy version '{parameters.LaunchVersionName}' cho instance {parameters.InstanceId}. Thử tạo lại instance hoặc kiểm tra thư mục {parameters.InstancePath}\\versions.");
        }

        var version = await _launcher.GetVersionAsync(parameters.LaunchVersionName, ct);
        if (version == null)
        {
            throw new InvalidOperationException(
                $"Không tìm thấy version '{parameters.LaunchVersionName}' cho instance {parameters.InstanceId}. Thử tạo lại instance hoặc kiểm tra thư mục {parameters.InstancePath}\\versions.");
        }

        var mcVersion = string.IsNullOrEmpty(parameters.MinecraftVersion)
            ? version.Id
            : parameters.MinecraftVersion;

        // Ensure Java is available
        var java = parameters.Java;
        var requiredMajor = _javaResolver.RequiredJava(mcVersion);

        if (java.MajorVersion < requiredMajor)
        {
            _logger.LogInformation("Java {Current} < required {Required} for MC {Mc}, auto-downloading", java.MajorVersion, requiredMajor, mcVersion);
            java = await _javaResolver.EnsureAsync(requiredMajor, null, ct);
            _logger.LogInformation("Auto-downloaded Java {Major} from Adoptium", java.MajorVersion);
        }

        // Build launch parameters - use MLaunchOption with correct properties for CmlLib.Core 4.0.6
        var launchOption = new MLaunchOption
        {
            Path = new MinecraftPath(parameters.InstancePath),
            MaximumRamMb = parameters.MaxRamMb,
            Session = CreateSession(parameters.Account),
            ScreenWidth = parameters.Width ?? 854,
            ScreenHeight = parameters.Height ?? 480,
            FullScreen = parameters.Fullscreen
        };

        if (!string.IsNullOrEmpty(parameters.JvmArguments))
        {
            launchOption.ExtraJvmArguments = ParseJvmArguments(parameters.JvmArguments);
        }

        // Build process
        var process = _launcher.BuildProcess(version, launchOption);
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = false;

        // Start process
        var started = process.Start();
        if (!started)
        {
            throw new InvalidOperationException("Failed to start Minecraft process");
        }

        _logger.LogInformation("Minecraft process started with PID {Pid}", process.Id);

        return new LaunchResult
        {
            ProcessId = process.Id,
            Process = new CmlProcessHandle(process)
        };
    }

    private MSession CreateSession(MinecraftAccount account)
    {
        // MSession constructor in CmlLib.Core 4.0.6 (from CmlLib.Core.Commons): username, accessToken, uuid
        if (account.Kind == AccountKind.Offline)
        {
            return new MSession(account.DisplayName, "offline", account.Uuid ?? account.Id);
        }

        return new MSession(account.DisplayName, account.AccessToken ?? "", account.Uuid ?? account.Id);
    }

    /// <summary>
    /// Tải tài nguyên phiên bản Minecraft gốc qua CmlLib với cơ chế tự động thử lại (Retry Policy).
    /// Tối đa 3 lần, exponential backoff (2s, 4s), bắt các lỗi mạng gián đoạn.
    /// Tôn trọng CancellationToken khi người dùng hủy.
    /// </summary>
    private async Task InstallWithRetryAsync(string versionName, CancellationToken ct)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await _launcher.InstallAsync(versionName, ct);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (IsNetworkException(ex))
            {
                if (attempt >= maxAttempts)
                {
                    _logger.LogError(ex, "Tải tài nguyên version {Version} thất bại sau {MaxAttempts} lần thử", versionName, maxAttempts);
                    throw;
                }

                var delaySeconds = (int)Math.Pow(2, attempt); // Lần 1 chờ 2s, lần 2 chờ 4s
                _logger.LogWarning(ex, "Lỗi mạng khi tải version {Version} (lần {Attempt}/{MaxAttempts}). Thử lại sau {Delay}s...",
                    versionName, attempt, maxAttempts, delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);
            }
        }
    }

    /// <summary>
    /// Kiểm tra ngoại lệ có bắt nguồn từ lỗi kết nối mạng / timeout hay không.
    /// </summary>
    public static bool IsNetworkException(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is System.Net.Http.HttpRequestException or
                System.Net.Sockets.SocketException or
                System.IO.IOException or
                TimeoutException)
            {
                return true;
            }

            if (current is TaskCanceledException tce && !tce.CancellationToken.IsCancellationRequested)
            {
                return true;
            }

            if (current.Message.Contains("An error occurred while sending the request", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Tra version theo tên; nếu root chưa có (instance cũ cài version json
    /// ngay trong thư mục instance như "test fabric/versions/fabric-loader-*"),
    /// copy folder version từ instance lên root rồi tra lại. Trả về true nếu
    /// version đã resolvable qua _launcher.GetVersionAsync(name) (không null,
    /// không ném lỗi "Cannot find").
    /// </summary>
    private async Task<bool> EnsureVersionResolvableAsync(LaunchParameters parameters, CancellationToken ct)
    {
        var name = parameters.LaunchVersionName!;
        try
        {
            var found = await _launcher.GetVersionAsync(name, ct);
            if (found != null) return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Version {Name} not found at root, trying instance folder migration", name);
        }

        MigrateInstanceVersionToRoot(parameters);

        try
        {
            return await _launcher.GetVersionAsync(name, ct) != null;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Version {Name} still not resolvable after migration", name);
            return false;
        }
    }

    /// <summary>
    /// Copy folder version (json + jar) từ {InstancePath}/versions/{name}/ lên
    /// {root instances}/versions/{name}/ nếu root còn thiếu — giúp instance cũ
    /// CmlLib tra được ở root. Không đè folder đã tồn tại ở root.
    /// </summary>
    private void MigrateInstanceVersionToRoot(LaunchParameters parameters)
    {
        var name = parameters.LaunchVersionName;
        if (string.IsNullOrEmpty(name)) return;

        var sourceDir = Directory.EnumerateDirectories(Path.Combine(parameters.InstancePath, "versions"), "*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(d => string.Equals(Path.GetFileName(d), name, StringComparison.OrdinalIgnoreCase));
        if (sourceDir == null) return;

        var rootVersions = Path.Combine(_options.RootPath, "instances", "versions");
        if (string.Equals(Path.Combine(parameters.InstancePath, "versions"), Path.GetFullPath(rootVersions), StringComparison.OrdinalIgnoreCase))
        {
            return; // instance và root trùng nơi — không cần copy
        }

        var targetDir = Path.Combine(rootVersions, name);
        if (Directory.Exists(targetDir))
        {
            _logger.LogInformation("Version {Name} already exists at root versions, skip migration", name);
            return;
        }

        Directory.CreateDirectory(rootVersions);
        CopyDirectory(sourceDir, targetDir);
        _logger.LogInformation("Migrated version folder {Name} from instance {InstanceId} to root versions", name, parameters.InstanceId);
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, file);
            var target = Path.Combine(targetDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private static System.Collections.Generic.IEnumerable<MArgument> ParseJvmArguments(string jvmArgs)
    {
        var parts = jvmArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            yield return new MArgument(part);
        }
    }
}
