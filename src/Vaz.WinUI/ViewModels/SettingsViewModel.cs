using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Vaz.WinUI.Helpers;
using Vaz.WinUI.Services;
using Vazer.Core.Abstractions;
using Vazer.Core.Settings;

namespace Vaz.WinUI.ViewModels;

/// <summary>Một lựa chọn trong ComboBox/ListView của Settings (value máy đọc + label hiển thị).</summary>
public sealed record SettingsOption(string Value, string Label, string? Glyph = null, string? ColorHex = null);

/// <summary>
/// ViewModel cho Settings page (SET-AC-01..SET-AC-10).
/// Bám executable spec web: DEFAULT_GLOBAL_SETTINGS, JVM_PRESETS, ACCENT_PALETTES,
/// syncGlobalSettingsUI / applyAccentColor / detectInstalledJava / exportDiagnosticReport.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private const string DetectedJavaPath =
        @"C:\Program Files\Eclipse Adoptium\jdk-21.0.4.7-hotspot\bin\javaw.exe";

    private const int RamStepMb = 512;

    private static readonly Dictionary<string, string> JvmPresetArgs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["standard"] = GlobalSettings.DefaultJvmArgs,
        ["aikar"] = "-XX:+UseG1GC -XX:+ParallelRefProcEnabled -XX:MaxGCPauseMillis=200 " +
                   "-XX:+UnlockExperimentalVMOptions -XX:+DisableExplicitGC -XX:+AlwaysPreTouch",
        ["zgc"] = "-XX:+UseZGC -XX:+ZGenerational -XX:+UnlockExperimentalVMOptions",
        ["custom"] = string.Empty
    };

    private readonly ISettingsService _settings;
    private readonly IInstanceManager _instances;
    private readonly ActiveInstanceService _activeInstance;
    private readonly ImageSource _discordArtNormal;
    private readonly ImageSource _discordArtGrayscale;
    private readonly ImageSource _discordAvatarNormal;
    private readonly ImageSource _discordAvatarGrayscale;
    private bool _isApplying;

    public ObservableCollection<SettingsOption> Categories { get; } = new()
    {
        new("all", "Tất cả", "\uE71D"),
        new("launcher", "Chung", "\uE770"),
        new("java", "Java & RAM", "\uE950"),
        new("appearance", "Giao diện", "\uE790"),
        new("network", "Mạng & Tải", "\uE896"),
        new("discord", "Discord", "\uE717"),
        new("storage", "Lưu trữ", "\uE8B7"),
        new("about", "Thông tin", "\uE946")
    };

    public ObservableCollection<SettingsOption> Languages { get; } = new()
    {
        new("vi", "Tiếng Việt (Việt Nam)"),
        new("en", "English (US)")
    };

    public ObservableCollection<SettingsOption> LaunchActions { get; } = new()
    {
        new("hide", "Ẩn launcher (Khuyên dùng)"),
        new("minimize", "Thu nhỏ xuống taskbar"),
        new("keep", "Giữ nguyên cửa sổ launcher"),
        new("close", "Đóng hoàn toàn launcher")
    };

    public ObservableCollection<SettingsOption> Themes { get; } = new()
    {
        new("dark", "Tối hiện đại (Dark)"),
        new("light", "Sáng thanh lịch (Light)"),
        new("obsidian", "Huyền bí Obsidian 2026")
    };

    public ObservableCollection<SettingsOption> Accents { get; } = new()
    {
        new("emerald", "Emerald", ColorHex: "#10B981"),
        new("cyan", "Cyan", ColorHex: "#06B6D4"),
        new("violet", "Violet", ColorHex: "#8B5CF6"),
        new("amber", "Amber", ColorHex: "#F59E0B"),
        new("rose", "Rose", ColorHex: "#F43F5E")
    };

    public ObservableCollection<SettingsOption> JvmPresets { get; } = new()
    {
        new("standard", "Tiêu chuẩn (G1GC mặc định)"),
        new("aikar", "Aikar's Flags (Tối ưu server & client mượt nhất)"),
        new("zgc", "ZGC Low Latency (Java 21+ loại bỏ khựng lag)"),
        new("custom", "Tùy chỉnh thủ công")
    };

    public ObservableCollection<SettingsOption> ThreadOptions { get; } = new()
    {
        new("2", "2 luồng (Mạng chậm hoặc không ổn định)"),
        new("4", "4 luồng (Mặc định)"),
        new("8", "8 luồng (Mạng cáp quang nhanh)"),
        new("16", "16 luồng (Tải cực nhanh / Gigabit)")
    };

    public ObservableCollection<SettingsOption> Mirrors { get; } = new()
    {
        new("cloudflare", "Cloudflare Global Edge (Nhanh nhất tại VN)"),
        new("modrinth", "Modrinth Direct API"),
        new("curseforge", "CurseForge CDN Edge"),
        new("fastly", "Fastly Anycast")
    };

    public ObservableCollection<SettingsOption> BandwidthOptions { get; } = new()
    {
        new("unlimited", "Không giới hạn (Tối đa tốc độ)"),
        new("25mb", "25 MB/s (~200 Mbps)"),
        new("10mb", "10 MB/s (~80 Mbps)"),
        new("5mb", "5 MB/s (~40 Mbps)")
    };

    // === Cài đặt (giá trị máy đọc) ===
    [ObservableProperty] private string language = "vi";
    [ObservableProperty] private string launchAction = "hide";
    [ObservableProperty] private bool closeToTray = true;
    [ObservableProperty] private bool autoCheckUpdates = true;

    [ObservableProperty] private string javaPath = string.Empty;
    [ObservableProperty] private int minRamMb = 2048;
    [ObservableProperty] private int ramMb = 4096;
    [ObservableProperty] private string jvmPreset = "standard";
    [ObservableProperty] private string jvmArgs = GlobalSettings.DefaultJvmArgs;
    [ObservableProperty] private int windowWidth = 1280;
    [ObservableProperty] private int windowHeight = 720;
    [ObservableProperty] private bool startFullscreen;

    [ObservableProperty] private string theme = "dark";
    [ObservableProperty] private string accentColor = "emerald";
    [ObservableProperty] private bool reduceMotion;
    [ObservableProperty] private bool uiSoundEnabled = true;

    [ObservableProperty] private int downloadThreads = 8;
    [ObservableProperty] private string downloadMirror = "cloudflare";
    [ObservableProperty] private string bandwidthLimit = "unlimited";

    [ObservableProperty] private bool discordRpc = true;
    [ObservableProperty] private string discordStatus = GlobalSettings.DefaultDiscordStatus;
    [ObservableProperty] private bool discordShowInstance = true;
    [ObservableProperty] private bool anonymousTelemetry = true;

    [ObservableProperty] private string dataPath = string.Empty;
    [ObservableProperty] private string backupPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CacheSizeText))]
    private int cacheSizeMb = 248;

    // === Lựa chọn UI (ComboBox SelectedItem / ListView) ===
    [ObservableProperty] private SettingsOption? selectedLanguage;
    [ObservableProperty] private SettingsOption? selectedLaunchAction;
    [ObservableProperty] private SettingsOption? selectedJvmPreset;
    [ObservableProperty] private SettingsOption? selectedTheme;
    [ObservableProperty] private SettingsOption? selectedAccent;
    [ObservableProperty] private SettingsOption? selectedDownloadThreads;
    [ObservableProperty] private SettingsOption? selectedMirror;
    [ObservableProperty] private SettingsOption? selectedBandwidth;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedCategoryKey))]
    private SettingsOption? selectedCategory;

    // === Trạng thái / chẩn đoán ===
    [ObservableProperty] private string statusText = string.Empty;
    [ObservableProperty] private string notificationText = string.Empty;
    [ObservableProperty] private bool isNotificationOpen;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isBusy;

    [ObservableProperty] private string osSpec = string.Empty;
    [ObservableProperty] private string cpuSpec = string.Empty;
    [ObservableProperty] private string gpuSpec = string.Empty;
    [ObservableProperty] private string ramSpec = string.Empty;
    [ObservableProperty] private string javaSpec = string.Empty;

    /// <summary>Dòng instance trong thẻ preview Discord; web lấy từ instance hiện tại.</summary>
    [ObservableProperty] private string discordPreviewInstance = "Minecraft 1.21.1";

    /// <summary>Ảnh block Minecraft (lớn) của preview — đổi sang bản grayscale khi RPC tắt.</summary>
    [ObservableProperty] private ImageSource? discordPreviewLargeArt;

    /// <summary>Ảnh avatar skin (nhỏ) của preview — đổi sang bản grayscale khi RPC tắt.</summary>
    [ObservableProperty] private ImageSource? discordPreviewSmallArt;

    public SettingsViewModel(
        ISettingsService settings,
        IInstanceManager instances,
        ActiveInstanceService activeInstance)
    {
        _settings = settings;
        _instances = instances;
        _activeInstance = activeInstance;

        // Web dùng ảnh assets/blocks/grass_block.png + assets/skins/steve.png.
        // Bản grayscale 60% được bake sẵn (WinUI 3 không có filter grayscale runtime).
        _discordArtNormal = LoadBitmap("ms-appx:///Assets/blocks/grass_block.png");
        _discordArtGrayscale = LoadBitmap("ms-appx:///Assets/blocks/grass_block_gray.png");
        _discordAvatarNormal = LoadBitmap("ms-appx:///Assets/skins/steve.png");
        _discordAvatarGrayscale = LoadBitmap("ms-appx:///Assets/skins/steve_gray.png");
        DiscordPreviewLargeArt = _discordArtNormal;
        DiscordPreviewSmallArt = _discordAvatarNormal;

        SelectedCategory = Categories.First();
        UpdateSystemSpecs();
    }

    public string SelectedCategoryKey => SelectedCategory?.Value ?? "all";

    public string CacheSizeText => $"{CacheSizeMb} MB";

    public string DiscordPreviewStatus =>
        string.IsNullOrWhiteSpace(DiscordStatus) ? "Đang phiêu lưu trong Minecraft" : DiscordStatus;

    public double DiscordPreviewOpacity => DiscordRpc ? 1.0 : 0.45;

    public string VersionLabel => "Phiên bản 2.4.0 (Build 2026.10-release)";

    /// <summary>Nạp từ settings.json; lỗi → defaults đã được service xử lý, chỉ còn lỗi unexpected.</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            HasError = false;

            var settings = await _settings.LoadAsync();
            ApplyFrom(settings);
            StatusText = string.Empty;
            await UpdateDiscordPreviewInstanceAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            StatusText = "Không tải được cài đặt.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Validate + clamp + ghi settings.json, cập nhật status giống saveGlobalSettings().</summary>
    [RelayCommand]
    public async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            HasError = false;

            NormalizeEditorState();
            var saved = await _settings.SaveAsync(ToModel());
            ApplyFrom(saved);

            StatusText = "Đã lưu cài đặt chung.";
            ShowNotification("Đã lưu cài đặt.");
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            StatusText = "Lưu cài đặt thất bại.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Reset toàn bộ về default rồi ghi file. Dialog xác nhận nằm ở code-behind.</summary>
    [RelayCommand]
    public async Task ResetAsync()
    {
        try
        {
            IsBusy = true;
            HasError = false;

            var defaults = await _settings.SaveAsync(_settings.CreateDefault());
            ApplyFrom(defaults);

            StatusText = "Đã khôi phục cài đặt mặc định.";
            ShowNotification("Đã khôi phục cài đặt mặc định.");
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            StatusText = "Khôi phục cài đặt thất bại.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Dò Java mô phỏng theo detectInstalledJava() của web (không gọi Java thật).</summary>
    [RelayCommand]
    private void DetectJava()
    {
        JavaPath = DetectedJavaPath;
        StatusText = "Đã tự động chọn Java 21 LTS.";
        ShowNotification("Đã phát hiện: Java 21 LTS Adoptium (64-bit)");
    }

    /// <summary>Xóa cache mô phỏng: set 0 MB + status đúng contract. Dialog xác nhận ở code-behind.</summary>
    [RelayCommand]
    private void ClearCache()
    {
        CacheSizeMb = 0;
        StatusText = "Đã xóa cache tải xuống · giải phóng 248 MB.";
        ShowNotification("Đã xóa cache tải xuống.");
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        ShowNotification("Đang kiểm tra cập nhật...");
        await Task.Delay(350);

        StatusText = "Hệ thống đã cập nhật mới nhất (v2.4.0).";
        ShowNotification("V.A.Z v2.4.0 đang là phiên bản mới nhất!");
    }

    /// <summary>Xuất JSON chẩn đoán; path do FileSavePicker ở code-behind truyền vào.</summary>
    [RelayCommand]
    private async Task ExportDiagnosticAsync(string? targetPath)
    {
        try
        {
            IsBusy = true;
            HasError = false;

            var path = string.IsNullOrWhiteSpace(targetPath) ? BuildDefaultDiagnosticPath() : targetPath;
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var activeInstance = await GetActiveInstanceNameAsync();
            await File.WriteAllTextAsync(path, BuildDiagnosticJson(await CountInstancesAsync(), activeInstance));

            StatusText = "Đã xuất tệp báo cáo chẩn đoán.";
            ShowNotification($"Đã xuất báo cáo chẩn đoán ({Path.GetFileName(path)}).");
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            StatusText = "Xuất báo cáo chẩn đoán thất bại.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenFolder(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusText = "Chưa cấu hình đường dẫn.";
            return;
        }

        try
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
            StatusText = "Đã mở thư mục.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            StatusText = "Không mở được thư mục.";
        }
    }

    // === Đồng bộ / áp dụng ===

    private void ApplyFrom(GlobalSettings settings)
    {
        _isApplying = true;
        try
        {
            Language = settings.Language;
            LaunchAction = settings.LaunchAction;
            CloseToTray = settings.CloseToTray;
            AutoCheckUpdates = settings.AutoCheckUpdates;

            JavaPath = settings.JavaPath;
            MinRamMb = settings.MinRamMb;
            RamMb = settings.RamMb;
            JvmPreset = settings.JvmPreset;
            JvmArgs = settings.JvmArgs;
            WindowWidth = settings.WindowWidth;
            WindowHeight = settings.WindowHeight;
            StartFullscreen = settings.StartFullscreen;

            Theme = settings.Theme;
            AccentColor = settings.AccentColor;
            ReduceMotion = settings.ReduceMotion;
            UiSoundEnabled = settings.UiSoundEnabled;

            DownloadThreads = settings.DownloadThreads;
            DownloadMirror = settings.DownloadMirror;
            BandwidthLimit = settings.BandwidthLimit;

            DiscordRpc = settings.DiscordRpc;
            DiscordStatus = settings.DiscordStatus;
            DiscordShowInstance = settings.DiscordShowInstance;
            AnonymousTelemetry = settings.AnonymousTelemetry;

            DataPath = settings.DataPath;
            BackupPath = settings.BackupPath;
            CacheSizeMb = settings.CacheSizeMb;

            SelectedLanguage = Match(Languages, settings.Language);
            SelectedLaunchAction = Match(LaunchActions, settings.LaunchAction);
            SelectedJvmPreset = Match(JvmPresets, settings.JvmPreset);
            SelectedTheme = Match(Themes, settings.Theme);
            SelectedAccent = Match(Accents, settings.AccentColor);
            SelectedDownloadThreads = Match(ThreadOptions, settings.DownloadThreads.ToString());
            SelectedMirror = Match(Mirrors, settings.DownloadMirror);
            SelectedBandwidth = Match(BandwidthOptions, settings.BandwidthLimit);
        }
        finally
        {
            _isApplying = false;
        }

        UpdateSystemSpecs();

        // SET-AC-04/05: theme, accent và reduce-motion do AppThemeManager áp cho toàn shell
        // (kể cả NavigationView pane + dock), không chỉ riêng root của Settings page.
        AppThemeManager.SetTheme(Theme);
        AppThemeManager.SetAccent(AccentColor);
        AppThemeManager.SetReduceMotion(ReduceMotion);
        UpdateDiscordPreviewArt();

        OnPropertyChanged(nameof(CacheSizeText));
        OnPropertyChanged(nameof(DiscordPreviewStatus));
        OnPropertyChanged(nameof(DiscordPreviewOpacity));
    }

    private GlobalSettings ToModel() => new()
    {
        Language = Language,
        LaunchAction = LaunchAction,
        CloseToTray = CloseToTray,
        AutoCheckUpdates = AutoCheckUpdates,
        JavaPath = JavaPath,
        MinRamMb = MinRamMb,
        RamMb = RamMb,
        JvmPreset = JvmPreset,
        JvmArgs = JvmArgs,
        WindowWidth = WindowWidth,
        WindowHeight = WindowHeight,
        StartFullscreen = StartFullscreen,
        Theme = Theme,
        AccentColor = AccentColor,
        ReduceMotion = ReduceMotion,
        UiSoundEnabled = UiSoundEnabled,
        DownloadThreads = DownloadThreads,
        DownloadMirror = DownloadMirror,
        BandwidthLimit = BandwidthLimit,
        DiscordRpc = DiscordRpc,
        DiscordStatus = DiscordStatus,
        DiscordShowInstance = DiscordShowInstance,
        AnonymousTelemetry = AnonymousTelemetry,
        DataPath = DataPath,
        BackupPath = BackupPath,
        CacheSizeMb = CacheSizeMb
    };

    /// <summary>Clamp TRƯỚC khi ghi, khớp applyGlobalSettings() phía web; RAM bước 512 MB.</summary>
    private void NormalizeEditorState()
    {
        RamMb = RoundToRamStep(Math.Clamp(RamMb, 1024, 16384), 16384);
        MinRamMb = RoundToRamStep(Math.Clamp(MinRamMb, 1024, RamMb), RamMb);
        WindowWidth = Math.Clamp(WindowWidth, 640, 3840);
        WindowHeight = Math.Clamp(WindowHeight, 480, 2160);

        if (!ThreadOptions.Any(o => o.Value == DownloadThreads.ToString()))
        {
            DownloadThreads = 8;
            SelectedDownloadThreads = Match(ThreadOptions, "8");
        }
    }

    /// <summary>Làm tròn về bội số 512 MB gần nhất (half away from zero) sau khi clamp.</summary>
    private static int RoundToRamStep(int value, int max)
    {
        var rounded = (int)(Math.Round(value / (double)RamStepMb, MidpointRounding.AwayFromZero) * RamStepMb);
        return Math.Clamp(rounded, 1024, max);
    }

    private void UpdateSystemSpecs()
    {
        OsSpec = SystemSpecsProvider.Os;
        CpuSpec = SystemSpecsProvider.Cpu;
        GpuSpec = SystemSpecsProvider.Gpu;
        RamSpec = SystemSpecsProvider.Ram;
        JavaSpec = SystemSpecsProvider.Java(JavaPath);
    }

    /// <summary>Đổi ảnh preview sang bản grayscale 60% khi RPC tắt (khớp filter grayscale(0.6) của web).</summary>
    private void UpdateDiscordPreviewArt()
    {
        DiscordPreviewLargeArt = DiscordRpc ? _discordArtNormal : _discordArtGrayscale;
        DiscordPreviewSmallArt = DiscordRpc ? _discordAvatarNormal : _discordAvatarGrayscale;
    }

    private static ImageSource LoadBitmap(string uri) =>
        new BitmapImage { UriSource = new Uri(uri) };

    private static SettingsOption Match(IEnumerable<SettingsOption> options, string value)
    {
        var list = options.ToList();
        return list.FirstOrDefault(o => string.Equals(o.Value, value, StringComparison.OrdinalIgnoreCase))
               ?? list[0];
    }

    private void ShowNotification(string message)
    {
        NotificationText = message;
        IsNotificationOpen = true;
    }

    private static string BuildDefaultDiagnosticPath()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Vazer",
            "diagnostics");
        return Path.Combine(folder, $"vaz-diagnostic-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.json");
    }

    /// <summary>Report JSON không dùng reflection (an toàn trim) — giống exportDiagnosticReport() của web.</summary>
    private string BuildDiagnosticJson(int instancesCount, string? activeInstance)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("app", "V.A.Z Launcher");
            writer.WriteString("version", "2.4.0");
            writer.WriteString("timestamp", DateTimeOffset.UtcNow.ToString("O"));
            writer.WriteString("os", OsSpec);
            writer.WriteString("cpu", CpuSpec);
            writer.WriteString("gpu", GpuSpec);
            writer.WriteString("ram", RamSpec);
            writer.WriteString("java", JavaSpec);

            writer.WriteStartObject("settings");
            writer.WriteString("language", Language);
            writer.WriteString("launchAction", LaunchAction);
            writer.WriteBoolean("closeToTray", CloseToTray);
            writer.WriteBoolean("autoCheckUpdates", AutoCheckUpdates);
            writer.WriteString("javaPath", JavaPath);
            writer.WriteNumber("minRamMb", MinRamMb);
            writer.WriteNumber("ramMb", RamMb);
            writer.WriteString("jvmPreset", JvmPreset);
            writer.WriteString("jvmArgs", JvmArgs);
            writer.WriteNumber("windowWidth", WindowWidth);
            writer.WriteNumber("windowHeight", WindowHeight);
            writer.WriteBoolean("startFullscreen", StartFullscreen);
            writer.WriteString("theme", Theme);
            writer.WriteString("accentColor", AccentColor);
            writer.WriteBoolean("reduceMotion", ReduceMotion);
            writer.WriteBoolean("uiSoundEnabled", UiSoundEnabled);
            writer.WriteNumber("downloadThreads", DownloadThreads);
            writer.WriteString("downloadMirror", DownloadMirror);
            writer.WriteString("bandwidthLimit", BandwidthLimit);
            writer.WriteBoolean("discordRpc", DiscordRpc);
            writer.WriteString("discordStatus", DiscordStatus);
            writer.WriteBoolean("discordShowInstance", DiscordShowInstance);
            writer.WriteBoolean("anonymousTelemetry", AnonymousTelemetry);
            writer.WriteString("dataPath", DataPath);
            writer.WriteString("backupPath", BackupPath);
            writer.WriteNumber("cacheSizeMb", CacheSizeMb);
            writer.WriteEndObject();

            // P1: web report có activeInstance (tên instance đang chọn) trước instancesCount.
            if (string.IsNullOrWhiteSpace(activeInstance))
            {
                writer.WriteNull("activeInstance");
            }
            else
            {
                writer.WriteString("activeInstance", activeInstance);
            }

            writer.WriteNumber("instancesCount", instancesCount);

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private async Task<int> CountInstancesAsync()
    {
        try
        {
            var all = await _instances.ListAsync();
            return all.Count;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>Tên instance đang active (null nếu chưa chọn hoặc không còn tồn tại).</summary>
    private async Task<string?> GetActiveInstanceNameAsync()
    {
        try
        {
            var current = await _activeInstance.GetCurrentAsync();
            return current?.Name;
        }
        catch
        {
            return null;
        }
    }

    // === Property change hooks ===

    partial void OnRamMbChanged(int value)
    {
        if (value < 1024) { RamMb = 1024; return; }
        if (value > 16384) { RamMb = 16384; return; }
        if (MinRamMb > value) MinRamMb = value;
    }

    partial void OnMinRamMbChanged(int value)
    {
        if (value < 1024) { MinRamMb = 1024; return; }
        if (value > RamMb) MinRamMb = RamMb;
    }

    partial void OnThemeChanged(string value)
    {
        if (_isApplying) return;

        // SET-AC-04: theme phải áp cho cả shell (pane + dock), không chỉ riêng page.
        AppThemeManager.SetTheme(value);
    }

    partial void OnAccentColorChanged(string value)
    {
        if (_isApplying) return;

        AppThemeManager.SetAccent(value);
    }

    partial void OnJavaPathChanged(string value) => JavaSpec = SystemSpecsProvider.Java(value);

    partial void OnDiscordStatusChanged(string value) =>
        OnPropertyChanged(nameof(DiscordPreviewStatus));

    partial void OnDiscordRpcChanged(bool value)
    {
        OnPropertyChanged(nameof(DiscordPreviewOpacity));
        OnPropertyChanged(nameof(DiscordPreviewStatus));
        UpdateDiscordPreviewArt();

        // Feedback giống web: chỉ khi user thao tác (không phải lúc load settings).
        if (_isApplying) return;

        StatusText = value
            ? "Đã bật Discord Rich Presence."
            : "Đã tắt Discord Rich Presence.";
    }

    partial void OnReduceMotionChanged(bool value)
    {
        if (_isApplying) return;

        // SET-AC-05: best-effort (WinUI chỉ hỗ trợ tắt dependent animation).
        AppThemeManager.SetReduceMotion(value);
    }

    /// <summary>
    /// Web hiển thị "Tên instance · Loader Version" trong preview; ưu tiên instance đang active,
    /// fallback instance đầu tiên, cuối cùng giữ mặc định.
    /// </summary>
    private async Task UpdateDiscordPreviewInstanceAsync()
    {
        try
        {
            var current = await _activeInstance.GetCurrentAsync();
            var instance = current;
            if (instance == null)
            {
                var all = await _instances.ListAsync();
                instance = all.FirstOrDefault();
            }

            if (instance == null) return;

            DiscordPreviewInstance =
                $"{instance.Name} · {FormatLoaderLabel(instance.LoaderType)} {instance.MinecraftVersion}";
        }
        catch
        {
            // Preview chỉ là trang trí; không chặn load settings.
        }
    }

    /// <summary>fabric → Fabric, neoforge → NeoForge, forge → Forge, vanilla/trống → Vanilla.</summary>
    private static string FormatLoaderLabel(string? loaderType)
    {
        if (string.IsNullOrWhiteSpace(loaderType)) return "Vanilla";

        return loaderType.Trim().ToLowerInvariant() switch
        {
            "vanilla" => "Vanilla",
            "fabric" => "Fabric",
            "forge" => "Forge",
            "neoforge" => "NeoForge",
            "quilt" => "Quilt",
            _ => char.ToUpperInvariant(loaderType.Trim()[0]) + loaderType.Trim()[1..]
        };
    }

    partial void OnSelectedLanguageChanged(SettingsOption? value)
    {
        if (!_isApplying && value != null) Language = value.Value;
    }

    partial void OnSelectedLaunchActionChanged(SettingsOption? value)
    {
        if (!_isApplying && value != null) LaunchAction = value.Value;
    }

    partial void OnSelectedJvmPresetChanged(SettingsOption? value)
    {
        if (_isApplying || value == null) return;

        JvmPreset = value.Value;
        JvmArgs = JvmPresetArgs.TryGetValue(value.Value, out var args)
            ? args
            : GlobalSettings.DefaultJvmArgs;
    }

    partial void OnSelectedThemeChanged(SettingsOption? value)
    {
        if (!_isApplying && value != null) Theme = value.Value;
    }

    partial void OnSelectedAccentChanged(SettingsOption? value)
    {
        if (!_isApplying && value != null) AccentColor = value.Value;
    }

    partial void OnSelectedDownloadThreadsChanged(SettingsOption? value)
    {
        if (!_isApplying && value != null && int.TryParse(value.Value, out var threads))
        {
            DownloadThreads = threads;
        }
    }

    partial void OnSelectedMirrorChanged(SettingsOption? value)
    {
        if (!_isApplying && value != null) DownloadMirror = value.Value;
    }

    partial void OnSelectedBandwidthChanged(SettingsOption? value)
    {
        if (!_isApplying && value != null) BandwidthLimit = value.Value;
    }
}
