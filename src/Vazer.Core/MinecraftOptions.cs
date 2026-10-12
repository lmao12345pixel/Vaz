namespace Vazer.Core;

/// <summary>
/// Cấu hình cho Minecraft launcher.
/// </summary>
public sealed class MinecraftOptions
{
    /// <summary>
    /// Thư mục gốc chứa data (instances, accounts, java, auth cache).
    /// Mặc định: {AppData}/Vazer/Minecraft
    /// </summary>
    public string RootPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Vazer", "Minecraft");

    /// <summary>
    /// Default max RAM (MB) cho instance mới.
    /// </summary>
    public int DefaultMaxRamMb { get; set; } = 4096;

    /// <summary>
    /// Minimum Java major version cho phép.
    /// </summary>
    public int MinJavaVersion { get; set; } = 8;

    /// <summary>
    /// Maximum Java major version cho phép.
    /// </summary>
    public int MaxJavaVersion { get; set; } = 21;

    /// <summary>
    /// Microsoft Entra (Azure AD) Client ID cho OAuth.
    /// Phải đăng ký ở Azure Portal: "Mobile and desktop applications" redirect URI: http://localhost
    /// </summary>
    public string MsalClientId { get; set; } = "";

    /// <summary>
    /// Nếu true, cho phép offline demo mode (không cần Microsoft account).
    /// Dùng cho development/testing.
    /// </summary>
    public bool OfflineDemoMode { get; set; } = true;

    /// <summary>
    /// User-Agent header for Modrinth API (bắt buộc theo TOS của Modrinth).
    /// </summary>
    public string ModrinthUserAgent { get; set; } = "Vazer/1.0.0 (+https://vazer.app)";

    /// <summary>
    /// User-Agent header cho OptiFine requests.
    /// </summary>
    public string OptifineUserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Vazer/1.0.0";

    /// <summary>
    /// Timeout cho Modrinth API calls (giây).
    /// </summary>
    public int ModrinthApiTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Timeout cho Modrinth file downloads (giây).
    /// </summary>
    public int ModrinthDownloadTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Timeout cho OptiFine requests (giây).
    /// </summary>
    public int OptifineTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// TTL cache catalog OptiFine (phút). Trang /downloads tĩnh, cache lâu được.
    /// </summary>
    public int OptifineCatalogCacheTtlMinutes { get; set; } = 360;

    /// <summary>
    /// Timeout HTTP cho các API call (seconds).
    /// </summary>
    public int HttpTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Số lần retry cho HTTP 5xx.
    /// </summary>
    public int HttpMaxRetries { get; set; } = 3;
}
