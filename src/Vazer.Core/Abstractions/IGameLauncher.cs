using System.Threading;

namespace Vazer.Core.Abstractions;

/// <summary>
/// Tham số cho việc launch Minecraft.
/// </summary>
public sealed record LaunchParameters
{
    /// <summary>
    /// Instance ID (thư mục instance).
    /// </summary>
    public required string InstanceId { get; init; }

    /// <summary>
    /// Instance path (full path to instance directory).
    /// </summary>
    public required string InstancePath { get; init; }

    /// <summary>
    /// Tài khoản dùng để launch.
    /// </summary>
    public required MinecraftAccount Account { get; init; }

    /// <summary>
    /// Java installation dùng để chạy.
    /// </summary>
    public required JavaInstallation Java { get; init; }

    /// <summary>
    /// Maximum RAM (MB).
    /// </summary>
    public int MaxRamMb { get; init; } = 4096;

    /// <summary>
    /// JVM arguments tùy chỉnh.
    /// </summary>
    public string? JvmArguments { get; init; }

    /// <summary>
    /// Game directory override (mặc định dùng instance folder).
    /// </summary>
    public string? GameDirectory { get; init; }

    /// <summary>
    /// Window width.
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Window height.
    /// </summary>
    public int? Height { get; init; }

    /// <summary>
    /// Fullscreen.
    /// </summary>
    public bool Fullscreen { get; init; } = false;

    /// <summary>
    /// Custom main class (cho mod loader đặc biệt).
    /// </summary>
    public string? MainClass { get; init; }
}

/// <summary>
/// Kết quả launch.
/// </summary>
public sealed record LaunchResult
{
    /// <summary>
    /// Process ID của Minecraft process.
    /// </summary>
    public required int ProcessId { get; init; }

    /// <summary>
    /// Process handle (để wait/exit).
    /// </summary>
    public required IProcessHandle Process { get; init; }
}

/// <summary>
/// Wrapper cho Process để abstract khỏi System.Diagnostics.Process.
/// Dispose để giải phóng handle hệ điều hành (không kill process).
/// </summary>
public interface IProcessHandle : IDisposable
{
    /// <summary>
    /// Process ID.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Process đã thoát chưa.
    /// </summary>
    bool HasExited { get; }

    /// <summary>
    /// Exit code (chỉ hợp lệ sau khi HasExited = true).
    /// </summary>
    int ExitCode { get; }

    /// <summary>
    /// Wait for exit async.
    /// </summary>
    Task<int> WaitForExitAsync(CancellationToken ct = default);

    /// <summary>
    /// Kill process.
    /// </summary>
    void Kill();
}

/// <summary>
/// Interface để launch Minecraft.
/// </summary>
public interface IGameLauncher
{
    /// <summary>
    /// Launch Minecraft với các tham số cho trước.
    /// </summary>
    /// <param name="parameters">Launch parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Launch result chứa process info.</returns>
    Task<LaunchResult> LaunchAsync(LaunchParameters parameters, CancellationToken ct = default);
}
