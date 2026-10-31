namespace Vaz.WinUI.Models;

/// <summary>
/// Trạng thái nút cài đặt của thẻ mod trong Mod Browser.
/// </summary>
public enum ModInstallState
{
    /// <summary>Chưa cài đặt -> Nút Cài (Download icon).</summary>
    Idle,

    /// <summary>Đang cài đặt -> Spinner ProgressRing ngay trên thẻ.</summary>
    Installing,

    /// <summary>Đã cài đặt trên đĩa -> Icon Verify (Dấu tích xanh).</summary>
    Installed
}
