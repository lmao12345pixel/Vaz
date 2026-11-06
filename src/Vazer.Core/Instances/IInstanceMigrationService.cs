using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vazer.Core.Abstractions;
using Vazer.Core.Loaders;

namespace Vazer.Core.Instances;

/// <summary>
/// Kết quả sau khi thực hiện đổi phiên bản / mod loader cho instance.
/// </summary>
public sealed record MigrationResult
{
    /// <summary>True nếu đổi phiên bản thành công; false nếu thất bại.</summary>
    public bool Success { get; init; }

    /// <summary>Lý do thất bại nếu không thành công.</summary>
    public string? Reason { get; init; }

    /// <summary>Danh sách đường dẫn các file backup đã được tạo.</summary>
    public IReadOnlyList<string> BackupPaths { get; init; } = Array.Empty<string>();

    /// <summary>Instance đã được cập nhật (nếu thành công).</summary>
    public MinecraftInstance? Instance { get; init; }

    public static MigrationResult Ok(MinecraftInstance instance, IReadOnlyList<string> backupPaths) => new()
    {
        Success = true,
        Instance = instance,
        BackupPaths = backupPaths
    };

    public static MigrationResult Fail(string reason, IReadOnlyList<string> backupPaths) => new()
    {
        Success = false,
        Reason = reason,
        BackupPaths = backupPaths
    };
}

/// <summary>
/// Ngoại lệ ném ra khi đổi phiên bản thất bại (đã được rollback từ backup).
/// </summary>
public class MigrationException : InvalidOperationException
{
    public MigrationResult Result { get; }

    public MigrationException(string message, MigrationResult result, Exception? inner = null)
        : base(message, inner)
    {
        Result = result;
    }
}

/// <summary>
/// Dịch vụ đổi Minecraft version + Mod Loader cho một instance hiện có (in-place).
/// Tự động backup instance.json và mods.json trước khi cài đặt.
/// Nếu cài đặt thất bại hoặc bị hủy, tự động rollback về trạng thái ban đầu.
/// </summary>
public interface IInstanceMigrationService
{
    /// <summary>
    /// Đổi version / loader cho instance.
    /// </summary>
    Task<MigrationResult> ChangeVersionAsync(
        string instanceId,
        string newVersion,
        string newLoader,
        bool keepMods,
        CancellationToken ct = default);

    /// <summary>
    /// Đổi version / loader cho instance (kèm loaderVersion cụ thể và progress).
    /// </summary>
    Task<MigrationResult> ChangeVersionAsync(
        string instanceId,
        string newVersion,
        string newLoader,
        bool keepMods,
        string? loaderVersion,
        IProgress<InstallerProgress>? progress,
        CancellationToken ct = default);
}
