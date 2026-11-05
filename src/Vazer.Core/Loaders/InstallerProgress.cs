using System.Collections.Generic;

namespace Vazer.Core.Loaders;

/// <summary>Stage của quá trình cài loader / game vào instance.</summary>
public enum InstallerStage
{
    /// <summary>Chuẩn bị (resolve version, tạo thư mục, tải metadata).</summary>
    Preparing,

    /// <summary>Đang tải file (client, libraries, installer jar).</summary>
    Downloading,

    /// <summary>Đang cài đặt / patch / chạy installer.</summary>
    Installing,

    /// <summary>Ghi metadata, hoàn thiện instance.</summary>
    Finalizing,

    /// <summary>Hoàn tất.</summary>
    Done
}

/// <summary>
/// Tiến trình cài đặt nhất quán cho mọi loader (Strategy pattern).
/// Percent = -1 nghĩa là không xác định (indeterminate); 0-100 là determinate.
/// </summary>
public sealed record InstallerProgress
{
    /// <summary>Stage hiện tại.</summary>
    public InstallerStage Stage { get; init; } = InstallerStage.Preparing;

    /// <summary>Phần trăm hoàn thành (-1 = indeterminate).</summary>
    public double Percent { get; init; } = -1;

    /// <summary>Mô tả ngắn cho user (hiển thị dưới ProgressBar).</summary>
    public string Message { get; init; } = string.Empty;
}
