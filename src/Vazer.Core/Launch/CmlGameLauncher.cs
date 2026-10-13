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
            // Ignore cancellation, process might still be running
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

        // Ensure Java is available
        var java = parameters.Java;
        var requiredMajor = _javaResolver.RequiredJava("1.21.1");

        if (java.MajorVersion < requiredMajor)
        {
            _logger.LogInformation("Java {Current} < required {Required}, auto-downloading", java.MajorVersion, requiredMajor);
            java = await _javaResolver.EnsureAsync(requiredMajor, null, ct);
            _logger.LogInformation("Auto-downloaded Java {Major} from Adoptium", java.MajorVersion);
        }

        // Get version
        var version = await _launcher.GetVersionAsync(parameters.InstanceId, ct);
        if (version == null)
        {
            throw new InvalidOperationException($"Version not found for instance {parameters.InstanceId}");
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

    private static System.Collections.Generic.IEnumerable<MArgument> ParseJvmArguments(string jvmArgs)
    {
        var parts = jvmArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            yield return new MArgument(part);
        }
    }
}
