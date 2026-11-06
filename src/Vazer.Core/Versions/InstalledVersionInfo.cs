using System;
using System.Collections.Generic;

namespace Vazer.Core.Versions;

/// <summary>
/// Thông tin một phiên bản Minecraft đã được cài đặt trên máy.
/// </summary>
public sealed record InstalledVersionInfo
{
    /// <summary>ID của phiên bản (tên thư mục con trong versions/, VD: "1.20.1", "fabric-loader-0.19.5-1.21.1").</summary>
    public required string VersionId { get; init; }

    /// <summary>Loại phiên bản (Vanilla, Fabric, Quilt, NeoForge, Forge, Snapshot...).</summary>
    public required string Type { get; init; }

    /// <summary>Dung lượng trên đĩa (bytes).</summary>
    public required long SizeBytes { get; init; }

    /// <summary>Thời điểm cài đặt (thời điểm tạo thư mục).</summary>
    public required DateTimeOffset InstalledAt { get; init; }

    /// <summary>Danh sách tên các instance đang sử dụng phiên bản này.</summary>
    public IReadOnlyList<string> UsedByInstances { get; init; } = Array.Empty<string>();

    /// <summary>True nếu đang có ít nhất 1 instance sử dụng.</summary>
    public bool IsInUse => UsedByInstances.Count > 0;

    /// <summary>Chuỗi hiển thị dung lượng (KB, MB, GB).</summary>
    public string SizeDisplay => FormatBytes(SizeBytes);

    /// <summary>Chuỗi tóm tắt tình trạng sử dụng.</summary>
    public string UsageSummary => IsInUse
        ? $"Đang dùng bởi {UsedByInstances.Count} instance ({string.Join(", ", UsedByInstances)})"
        : "Không có instance nào sử dụng (Orphan)";

    public static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }
}

/// <summary>
/// Kết quả xóa một phiên bản.
/// </summary>
public sealed record DeleteResult
{
    public bool Success { get; init; }
    public bool Blocked { get; init; }
    public string? Reason { get; init; }
    public long FreedBytes { get; init; }
    public string VersionId { get; init; } = "";

    public static DeleteResult Succeeded(string versionId, long freedBytes) => new()
    {
        Success = true,
        Blocked = false,
        VersionId = versionId,
        FreedBytes = freedBytes
    };

    public static DeleteResult Block(string versionId, string reason) => new()
    {
        Success = false,
        Blocked = true,
        VersionId = versionId,
        Reason = reason
    };

    public static DeleteResult Failed(string versionId, string reason) => new()
    {
        Success = false,
        Blocked = false,
        VersionId = versionId,
        Reason = reason
    };
}

/// <summary>
/// Thống kê dung lượng đĩa của launcher.
/// </summary>
public sealed record DiskUsageSummary
{
    public long TotalBytes => VersionsBytes + LibrariesBytes + AssetsBytes;
    public long VersionsBytes { get; init; }
    public long LibrariesBytes { get; init; }
    public long AssetsBytes { get; init; }
    public int InstalledVersionsCount { get; init; }

    public string TotalDisplay => InstalledVersionInfo.FormatBytes(TotalBytes);
    public string VersionsDisplay => InstalledVersionInfo.FormatBytes(VersionsBytes);
    public string LibrariesDisplay => InstalledVersionInfo.FormatBytes(LibrariesBytes);
    public string AssetsDisplay => InstalledVersionInfo.FormatBytes(AssetsBytes);
}

/// <summary>
/// Kết quả dọn dẹp các version không dùng (orphan).
/// </summary>
public sealed record CleanupResult
{
    public bool Success { get; init; }
    public int CleanedCount { get; init; }
    public long FreedBytes { get; init; }
    public IReadOnlyList<string> DeletedVersionIds { get; init; } = Array.Empty<string>();
    public string? Message { get; init; }

    public string FreedDisplay => InstalledVersionInfo.FormatBytes(FreedBytes);
}
