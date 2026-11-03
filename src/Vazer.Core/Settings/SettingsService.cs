using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vazer.Core.Json;

namespace Vazer.Core.Settings;

/// <summary>
/// File-based settings store. File nằm dưới <see cref="MinecraftOptions.RootPath"/>
/// (<c>{RootPath}/settings.json</c>) — cùng gốc với accounts/instances để backup
/// và di chuyển thư mục dữ liệu chỉ cần một chỗ.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private const int MinRamFloorMb = 1024;
    private const int MaxRamCeilingMb = 16384;
    private const int RamStepMb = 512;
    private const int MinWindowWidth = 640;
    private const int MaxWindowWidth = 3840;
    private const int MinWindowHeight = 480;
    private const int MaxWindowHeight = 2160;

    private static readonly string[] Languages = { "vi", "en" };
    private static readonly string[] LaunchActions = { "hide", "minimize", "keep", "close" };
    private static readonly string[] Themes = { "dark", "light", "obsidian" };
    private static readonly string[] AccentColors = { "emerald", "cyan", "violet", "amber", "rose" };
    private static readonly string[] DownloadMirrors = { "cloudflare", "modrinth", "curseforge", "fastly" };
    private static readonly string[] BandwidthLimits = { "unlimited", "25mb", "10mb", "5mb" };
    private static readonly string[] JvmPresets = { "standard", "aikar", "zgc", "custom" };
    private static readonly int[] DownloadThreadCounts = { 2, 4, 8, 16 };

    private static readonly JsonTypeInfo<GlobalSettings> SettingsJsonTypeInfo =
        VazJsonContext.Settings.GlobalSettings;

    private readonly string _filePath;
    private readonly string _defaultDataPath;
    private readonly string _defaultBackupPath;
    private readonly ILogger<SettingsService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SettingsService(IOptions<MinecraftOptions> options, ILogger<SettingsService> logger)
    {
        _logger = logger;
        _defaultDataPath = options.Value.RootPath;
        _defaultBackupPath = Path.Combine(_defaultDataPath, "backups");
        _filePath = Path.Combine(_defaultDataPath, "settings.json");
    }

    /// <inheritdoc />
    public string SettingsFilePath => _filePath;

    /// <inheritdoc />
    public GlobalSettings CreateDefault() => GlobalSettings.CreateDefault(_defaultDataPath, _defaultBackupPath);

    /// <inheritdoc />
    public async Task<GlobalSettings> LoadAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            GlobalSettings? loaded = null;

            if (File.Exists(_filePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_filePath, ct).ConfigureAwait(false);
                    loaded = JsonSerializer.Deserialize(json, SettingsJsonTypeInfo);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "settings.json không hợp lệ — dùng mặc định");
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex, "Không đọc được settings.json — dùng mặc định");
                }
            }

            return Normalize(loaded);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<GlobalSettings> SaveAsync(GlobalSettings settings, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var normalized = Normalize(settings);

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(normalized, SettingsJsonTypeInfo);
            var tmpPath = _filePath + ".tmp";
            await File.WriteAllTextAsync(tmpPath, json, ct).ConfigureAwait(false);

            // Ghi atomic: move đè file thật, tránh file cụt khi app bị kill giữa chừng.
            File.Move(tmpPath, _filePath, true);

            return normalized;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Chuẩn hóa mọi giá trị về dải/giá trị hợp lệ theo contract SET-AC-02.
    /// Giá trị lạ → default (không throw).
    /// </summary>
    private GlobalSettings Normalize(GlobalSettings? source)
    {
        var s = source ?? new GlobalSettings();

        var result = new GlobalSettings
        {
            Language = NormalizeChoice(s.Language, "vi", Languages),
            LaunchAction = NormalizeChoice(s.LaunchAction, "hide", LaunchActions),
            CloseToTray = s.CloseToTray,
            AutoCheckUpdates = s.AutoCheckUpdates,

            JavaPath = (s.JavaPath ?? string.Empty).Trim(),
            RamMb = NormalizeRamMb(s.RamMb, MaxRamCeilingMb),
            JvmPreset = NormalizeChoice(s.JvmPreset, "standard", JvmPresets),
            JvmArgs = string.IsNullOrWhiteSpace(s.JvmArgs) ? GlobalSettings.DefaultJvmArgs : s.JvmArgs.Trim(),
            WindowWidth = s.WindowWidth <= 0 ? 1280 : Math.Clamp(s.WindowWidth, MinWindowWidth, MaxWindowWidth),
            WindowHeight = s.WindowHeight <= 0 ? 720 : Math.Clamp(s.WindowHeight, MinWindowHeight, MaxWindowHeight),
            StartFullscreen = s.StartFullscreen,

            Theme = NormalizeChoice(s.Theme, "dark", Themes),
            AccentColor = NormalizeChoice(s.AccentColor, "emerald", AccentColors),
            ReduceMotion = s.ReduceMotion,
            UiSoundEnabled = s.UiSoundEnabled,

            DownloadThreads = DownloadThreadCounts.Contains(s.DownloadThreads) ? s.DownloadThreads : 8,
            DownloadMirror = NormalizeChoice(s.DownloadMirror, "cloudflare", DownloadMirrors),
            BandwidthLimit = NormalizeChoice(s.BandwidthLimit, "unlimited", BandwidthLimits),

            DiscordRpc = s.DiscordRpc,
            DiscordStatus = string.IsNullOrWhiteSpace(s.DiscordStatus)
                ? GlobalSettings.DefaultDiscordStatus
                : s.DiscordStatus.Trim(),
            DiscordShowInstance = s.DiscordShowInstance,
            AnonymousTelemetry = s.AnonymousTelemetry,

            DataPath = string.IsNullOrWhiteSpace(s.DataPath) ? _defaultDataPath : s.DataPath.Trim(),
            BackupPath = string.IsNullOrWhiteSpace(s.BackupPath) ? _defaultBackupPath : s.BackupPath.Trim(),
            CacheSizeMb = Math.Clamp(s.CacheSizeMb, 0, 1_048_576)
        };

        // Min RAM không được vượt Max RAM (SET-AC-02) và phải là bội số 512 MB.
        result.MinRamMb = NormalizeRamMb(s.MinRamMb, result.RamMb);

        return result;
    }

    /// <summary>
    /// Clamp rồi làm tròn về bội số 512 MB (half away from zero). Slider/NumberBox của web
    /// bước 512 nên mọi giá trị lưu xuống file đều phải là bội số (SET-AC-13).
    /// </summary>
    private static int NormalizeRamMb(int value, int max)
    {
        var clamped = Math.Clamp(value, MinRamFloorMb, max);
        var rounded = (int)(Math.Round(clamped / (double)RamStepMb, MidpointRounding.AwayFromZero) * RamStepMb);
        return Math.Clamp(rounded, MinRamFloorMb, max);
    }

    private static string NormalizeChoice(string? value, string fallback, string[] allowed)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var trimmed = value.Trim();
        foreach (var option in allowed)
        {
            if (string.Equals(option, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return option;
            }
        }

        return fallback;
    }
}
