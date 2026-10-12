namespace Vazer.Core.Abstractions;

/// <summary>
/// Loại tài khoản Minecraft được hỗ trợ.
/// </summary>
public enum AccountKind
{
    /// <summary>
    /// Tài khoản Microsoft (Xbox Live / Microsoft Entra).
    /// </summary>
    Microsoft,

    /// <summary>
    /// Tài khoản offline (không cần internet, dùng cho LAN/single-player).
    /// </summary>
    Offline
}
