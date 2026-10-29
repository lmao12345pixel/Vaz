namespace Vazer.Core.Settings;

/// <summary>
/// Cài đặt chung của launcher (Settings page).
/// Giá trị mặc định bám theo executable spec web
/// <c>docs/design-preview/app.js</c> — <c>DEFAULT_GLOBAL_SETTINGS</c>.
/// </summary>
public sealed class GlobalSettings
{
    /// <summary>Preset JVM mặc định (Standard / G1GC).</summary>
    public const string DefaultJvmArgs = "-XX:+UseG1GC -XX:+ParallelRefProcEnabled";

    /// <summary>Trạng thái Discord mặc định.</summary>
    public const string DefaultDiscordStatus = "Đang sinh tồn ở thế giới mới";

    // Chung / Launcher
    public string Language { get; set; } = "vi";

    public string LaunchAction { get; set; } = "hide";

    public bool CloseToTray { get; set; } = true;

    public bool AutoCheckUpdates { get; set; } = true;

    // Java & Minecraft
    public string JavaPath { get; set; } = string.Empty;

    public int MinRamMb { get; set; } = 2048;

    public int RamMb { get; set; } = 4096;

    public string JvmPreset { get; set; } = "standard";

    public string JvmArgs { get; set; } = DefaultJvmArgs;

    public int WindowWidth { get; set; } = 1280;

    public int WindowHeight { get; set; } = 720;

    public bool StartFullscreen { get; set; }

    // Giao diện / Appearance
    public string Theme { get; set; } = "dark";

    public string AccentColor { get; set; } = "emerald";

    public bool ReduceMotion { get; set; }

    public bool UiSoundEnabled { get; set; } = true;

    // Mạng & Tải xuống / Network
    public int DownloadThreads { get; set; } = 8;

    public string DownloadMirror { get; set; } = "cloudflare";

    public string BandwidthLimit { get; set; } = "unlimited";

    // Discord & Riêng tư
    public bool DiscordRpc { get; set; } = true;

    public string DiscordStatus { get; set; } = DefaultDiscordStatus;

    public bool DiscordShowInstance { get; set; } = true;

    public bool AnonymousTelemetry { get; set; } = true;

    // Lưu trữ & Dữ liệu
    public string DataPath { get; set; } = string.Empty;

    public string BackupPath { get; set; } = string.Empty;

    public int CacheSizeMb { get; set; } = 248;

    /// <summary>
    /// Tạo bộ giá trị mặc định. Khi không truyền đường dẫn, dùng thư mục
    /// <c>%APPDATA%/Vazer/Minecraft</c> (trùng <c>MinecraftOptions.RootPath</c> mặc định).
    /// </summary>
    public static GlobalSettings CreateDefault(string? dataPath = null, string? backupPath = null)
    {
        var root = string.IsNullOrWhiteSpace(dataPath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Vazer",
                "Minecraft")
            : dataPath;

        return new GlobalSettings
        {
            DataPath = root,
            BackupPath = string.IsNullOrWhiteSpace(backupPath)
                ? Path.Combine(root, "backups")
                : backupPath
        };
    }
}
