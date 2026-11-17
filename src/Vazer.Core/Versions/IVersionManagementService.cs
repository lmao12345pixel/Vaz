using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Vazer.Core.Versions;

/// <summary>
/// Dịch vụ quản lý các phiên bản Minecraft đã cài đặt:
/// quét thư mục versions/, thống kê dung lượng, xóa phiên bản và dọn dẹp phiên bản mồ côi (orphan).
/// </summary>
public interface IVersionManagementService
{
    /// <summary>
    /// Liệt kê tất cả các phiên bản đã cài đặt trong thư mục versions/,
    /// kèm thông tin dung lượng và các instance đang dùng.
    /// </summary>
    Task<IReadOnlyList<InstalledVersionInfo>> ListInstalledVersionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Xóa một phiên bản đã cài đặt.
    /// Chặn xóa nếu phiên bản đang được dùng bởi ít nhất 1 instance.
    /// </summary>
    Task<DeleteResult> DeleteVersionAsync(string versionId, CancellationToken ct = default);

    /// <summary>
    /// Thống kê dung lượng sử dụng trên đĩa (versions, libraries, assets).
    /// </summary>
    Task<DiskUsageSummary> GetDiskUsageAsync(CancellationToken ct = default);

    /// <summary>
    /// Dọn dẹp tất cả các phiên bản orphan (không có instance nào dùng) và file tạm.
    /// </summary>
    Task<CleanupResult> CleanupOrphanVersionsAsync(CancellationToken ct = default);
}
