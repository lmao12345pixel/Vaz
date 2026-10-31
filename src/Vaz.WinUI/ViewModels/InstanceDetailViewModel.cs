using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Vaz.WinUI.Helpers;
using Vaz.WinUI.Messages;
using Vaz.WinUI.Models;
using Vaz.WinUI.Services;
using Vazer.Core.Abstractions;
using Vazer.Core.GameOptions;
using Vazer.Core.Json;
using Vazer.Core.Loaders.Optifine;
using Vazer.Core.Modpacks;
using Vazer.Core.Mods;

namespace Vaz.WinUI.ViewModels;

/// <summary>Row view cho mod đã cài kèm metadata chi tiết và trạng thái dependency.</summary>
public sealed partial class InstalledModRow : ObservableObject
{
    public required InstalledMod Mod { get; init; }
    [ObservableProperty] private bool hasUpdate;
    [ObservableProperty] private bool enabled;
    [ObservableProperty] private bool hasMissingDependencies;
    [ObservableProperty] private string missingDependenciesText = string.Empty;
    [ObservableProperty] private bool isMissingFile;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanToggle))]
    [NotifyPropertyChangedFor(nameof(CanUninstall))]
    [NotifyPropertyChangedFor(nameof(ToggleToolTip))]
    [NotifyPropertyChangedFor(nameof(UninstallToolTip))]
    private bool isGameRunning;

    public bool CanToggle => !IsMissingFile && !IsGameRunning;
    public bool CanUninstall => !IsGameRunning;

    public string? ToggleToolTip => IsGameRunning
        ? "Không thể thao tác khi Minecraft đang chạy."
        : (IsMissingFile ? "Không tìm thấy file jar trên đĩa" : null);

    public string UninstallToolTip => IsGameRunning
        ? "Không thể thao tác khi Minecraft đang chạy."
        : "Gỡ cài đặt mod";

    public void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(Enabled));
        OnPropertyChanged(nameof(CanToggle));
    }

    public string? ModId { get; set; }

    public string FileName => Mod.FileName;
    public string? VersionId => string.IsNullOrEmpty(Mod.VersionId) ? null : Mod.VersionId;
    public string Version => VersionId ?? "Không rõ";
    public string UpdateBadge => HasUpdate ? "↑ update" : string.Empty;
    public string WarningBadge => HasMissingDependencies ? "Thiếu dependency" : string.Empty;

    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LoadWarning { get; set; } = string.Empty;

    public bool HasLoadWarning => !string.IsNullOrEmpty(LoadWarning);
    public string LoadWarningText => HasLoadWarning ? "⚠ " + LoadWarning : string.Empty;
    public string Category { get; set; } = "other";
    public string CategoryLabel { get; set; } = "Khác";
    public string Loader { get; set; } = "Fabric";
    public string MinecraftVersion { get; set; } = "1.21.1";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IconImageSource))]
    private string iconUri = "ms-appx:///Assets/mods/default.png";
    public string ProjectUrl { get; set; } = string.Empty;

    /// <summary>
    /// Binding an toàn cho Image.Source — fallback placeholder khi IconUri rỗng/sai
    /// (lỗi crash.txt: "The value cannot be converted to type ImageSource").
    /// </summary>
    public Microsoft.UI.Xaml.Media.ImageSource IconImageSource
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(IconUri) &&
                Uri.TryCreate(IconUri, UriKind.RelativeOrAbsolute, out var uri))
            {
                return new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(uri);
            }

            return Vaz.WinUI.Helpers.UiPlaceholders.Image;
        }
    }

    /// <summary>Binding an toàn cho HyperlinkButton.NavigateUri (Uri, không rỗng — fallback "about:blank" theo yêu cầuHotfix).</summary>
    public Uri? ProjectLink
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ProjectUrl)) return Vaz.WinUI.Helpers.UiPlaceholders.FallbackLink;
            return Uri.TryCreate(ProjectUrl, UriKind.Absolute, out var uri) && uri is { } ? uri : Vaz.WinUI.Helpers.UiPlaceholders.FallbackLink;
        }
    }

    public bool HasProjectUrl => !string.IsNullOrWhiteSpace(ProjectUrl);

    public List<ModDependencyStatusItem> Dependencies { get; set; } = new();

    partial void OnHasMissingDependenciesChanged(bool value)
    {
        OnPropertyChanged(nameof(WarningBadge));
    }

    partial void OnHasUpdateChanged(bool value)
    {
        OnPropertyChanged(nameof(UpdateBadge));
    }

    partial void OnIsMissingFileChanged(bool value)
    {
        OnPropertyChanged(nameof(CanToggle));
    }
}

/// <summary>
/// ViewModel cho Instance Detail page: launch, danh sách mod, update,
/// mrpack, OptiFine, mở mods folder, settings (ReadOnly) + delete.
/// </summary>
public sealed partial class InstanceDetailViewModel : ObservableObject, IDisposable
{
    private static readonly System.Text.UTF8Encoding Utf8NoBom = new(false);
    private static readonly JsonSerializerOptions CamelCaseJsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    // Cache metadata jar theo (tên file, ngày sửa, size) — tránh đọc lại zip mỗi lần Load
    private readonly Dictionary<(string Name, DateTime LastWriteUtc, long Length), JarMetadata?> _jarMetaCache = new();
    private bool _diskCacheLoaded;
    private readonly IInstanceManager _instanceManager;
    private readonly IModInstallService _modInstall;
    private readonly IMrpackImporter _mrpackImporter;
    private readonly IOptifineInstallerService _optifine;
    private readonly ActiveInstanceService _activeInstance;
    private readonly MainViewModel _mainViewModel;
    private readonly IGameOptionsService _gameOptionsService;

    public ObservableCollection<InstalledModRow> Mods { get; } = new();
    public ObservableCollection<InstalledModRow> FilteredMods { get; } = new();

    [ObservableProperty] private string selectedCategory = "all";
    [ObservableProperty] private string modSummaryText = "Hiển thị 0 / 0 mods";

    [ObservableProperty] private bool isInspectorOpen;
    [ObservableProperty] private InstalledModRow? selectedModForInspector;
    [ObservableProperty] private string inspectorDepStatus = "Đầy đủ";
    [ObservableProperty] private bool inspectorDepStatusIsWarning;
    [ObservableProperty] private bool inspectorDepActionVisible;
    [ObservableProperty] private string inspectorDepActionText = string.Empty;
    public ObservableCollection<ModDependencyStatusItem> InspectorDependencies { get; } = new();

    [ObservableProperty] private bool hasDependencyWarning;
    [ObservableProperty] private string dependencyWarningMessage = string.Empty;

    [ObservableProperty] private MinecraftInstance? current;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private int progressPercent;
    [ObservableProperty] private int updateCount;
    [ObservableProperty] private string launchStatus = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanManageMods))]
    [NotifyPropertyChangedFor(nameof(GameRunningActionToolTip))]
    [NotifyPropertyChangedFor(nameof(MrpackToolTip))]
    private bool isGameRunning;

    public bool CanManageMods => !IsGameRunning;

    public string? GameRunningActionToolTip => IsGameRunning
        ? "Không thể thao tác khi Minecraft đang chạy."
        : null;

    public string MrpackToolTip => IsGameRunning
        ? "Không thể thao tác khi Minecraft đang chạy."
        : (Current == null ? "Chọn một instance để cài modpack" : "Cài đặt file .mrpack trực tiếp vào instance này");

    [ObservableProperty] private GameOptions gameOptions = new();
    [ObservableProperty] private string rawOptionsText = string.Empty;
    [ObservableProperty] private bool isRawViewMode;

    [ObservableProperty] private string logText = string.Empty;
    [ObservableProperty] private bool hasLogs;
    [ObservableProperty] private bool autoScroll = true;

    public event Action? LogAppended;

    private FileSystemWatcher? _logWatcher;
    private long _lastLogPosition;
    private readonly object _logLock = new();
    private string? _watchedInstancePath;

    public bool IsNotBusy => !IsBusy;
    public bool HasInstance => Current != null;
    public string FolderPath => Current?.Path ?? string.Empty;
    public string LoaderDisplay => Current == null
        ? string.Empty
        : (Current.LoaderVersion == null
            ? Current.LoaderType
            : $"{Current.LoaderType} {Current.LoaderVersion}");

    public InstanceDetailViewModel(
        IInstanceManager instanceManager,
        IModInstallService modInstall,
        IMrpackImporter mrpackImporter,
        IOptifineInstallerService optifine,
        ActiveInstanceService activeInstance,
        MainViewModel mainViewModel,
        IGameOptionsService gameOptionsService)
    {
        _instanceManager = instanceManager;
        _modInstall = modInstall;
        _mrpackImporter = mrpackImporter;
        _optifine = optifine;
        _activeInstance = activeInstance;
        _mainViewModel = mainViewModel;
        _gameOptionsService = gameOptionsService;
        _activeInstance.CurrentChanged += OnActiveInstanceChanged;
        _mainViewModel.PropertyChanged += OnMainViewModelPropertyChanged;
        UpdateGameRunningState();
    }

    private void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsRunning) ||
            e.PropertyName == nameof(MainViewModel.RunningInstanceId))
        {
            var dispatcher = App.Dispatcher;
            if (dispatcher != null && !dispatcher.HasThreadAccess)
            {
                dispatcher.TryEnqueue(UpdateGameRunningState);
            }
            else
            {
                UpdateGameRunningState();
            }
        }
    }

    public void UpdateGameRunningState()
    {
        var running = _mainViewModel.IsRunning &&
                      (string.IsNullOrEmpty(_mainViewModel.RunningInstanceId) ||
                       string.Equals(_mainViewModel.RunningInstanceId, Current?.Id, StringComparison.OrdinalIgnoreCase));

        if (IsGameRunning != running)
        {
            IsGameRunning = running;
            SyncModsGameRunningState();
        }
    }

    private void SyncModsGameRunningState()
    {
        foreach (var row in Mods)
        {
            row.IsGameRunning = IsGameRunning;
        }
    }

    private void OnActiveInstanceChanged(object? sender, EventArgs e)
    {
        var dispatcher = App.Dispatcher;
        if (dispatcher != null && !dispatcher.HasThreadAccess)
        {
            dispatcher.TryEnqueue(() => _ = LoadAsync());
        }
        else
        {
            _ = LoadAsync();
        }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            HasError = false;

            Current = await _activeInstance.GetCurrentAsync();
            UpdateGameRunningState();
            OnPropertyChanged(nameof(HasInstance));
            OnPropertyChanged(nameof(FolderPath));
            OnPropertyChanged(nameof(LoaderDisplay));

            Mods.Clear();
            UpdateCount = 0;

            if (Current != null)
            {
                var currentPath = Current.Path;
                var currentId = Current.Id;
                var currentLoader = Current.LoaderType;
                var currentMcVersion = Current.MinecraftVersion;

                var loadedRows = await Task.Run(async () =>
                {
                    EnsureDiskCacheLoaded(currentPath);

                    // Auto-discover jar files placed in mods folder that might not be in mods.json
                    var modsDir = Path.Combine(currentPath, "mods");
                    if (Directory.Exists(modsDir))
                    {
                        ModInstallService.CleanOrphanPartials(modsDir);
                        var jarFiles = Directory.GetFiles(modsDir, "*.jar");
                        if (jarFiles.Length > 0)
                        {
                            var manifestPath = Path.Combine(currentPath, "mods.json");
                            ModsManifest manifest = new();
                            if (File.Exists(manifestPath))
                            {
                                try
                                {
                                    var json = await File.ReadAllTextAsync(manifestPath);
                                    manifest = JsonSerializer.Deserialize<ModsManifest>(json, CamelCaseJsonOptions) ?? new();
                                }
                                catch { }
                            }

                            bool manifestChanged = false;
                            foreach (var jf in jarFiles)
                            {
                                var fn = Path.GetFileName(jf);
                                if (manifest.Mods.Any(m => string.Equals(m.FileName, fn, StringComparison.OrdinalIgnoreCase)))
                                {
                                    continue;
                                }

                                JarMetadata? meta;
                                try
                                {
                                    meta = ReadJarMetadataCached(jf);
                                }
                                catch
                                {
                                    meta = null; // jar hỏng — không crash
                                }

                                manifest.Mods.Add(new InstalledMod
                                {
                                    FileName = fn,
                                    ProjectId = string.IsNullOrWhiteSpace(meta?.ModId) ? fn : meta.ModId,
                                    VersionId = string.IsNullOrWhiteSpace(meta?.Version) ? "unknown" : meta.Version,
                                    Source = "custom",
                                    Enabled = true,
                                    InstalledAt = DateTimeOffset.UtcNow
                                });
                                manifestChanged = true;
                            }

                            if (manifestChanged)
                            {
                                var tmpPath = Path.Combine(currentPath, "mods.json.tmp");
                                var outJson = JsonSerializer.Serialize(manifest, CamelCaseJsonOptions);
                                await File.WriteAllTextAsync(tmpPath, outJson, Utf8NoBom);
                                File.Move(tmpPath, manifestPath, overwrite: true);
                            }
                        }
                    }

                    var mods = await _modInstall.ListAsync(currentId);
                    var rowList = new List<InstalledModRow>();

                    foreach (var mod in mods)
                    {
                        var slug = ModCatalogData.GetSlug(mod.FileName, mod.ProjectId);
                        var proj = ModCatalogData.FindProject(slug);

                        var row = new InstalledModRow
                        {
                            Mod = mod,
                            Enabled = mod.Enabled,
                            IsGameRunning = IsGameRunning,
                            Slug = slug,
                            DisplayName = proj?.Name ?? (string.IsNullOrEmpty(mod.ProjectId) ? mod.FileName : mod.ProjectId),
                            Author = proj?.Author ?? "Community",
                            Description = proj?.Description ?? "Mod Minecraft được cài đặt trong instance.",
                            Category = proj?.Category ?? "other",
                            CategoryLabel = proj?.CategoryLabel ?? "Khác",
                            Loader = currentLoader,
                            MinecraftVersion = currentMcVersion,
                            IconUri = proj?.Icon ?? "ms-appx:///Assets/mods/default.png",
                            ProjectUrl = proj != null ||
                                         string.Equals(mod.Source, "modrinth", StringComparison.OrdinalIgnoreCase)
                                ? $"https://modrinth.com/mod/{slug}"
                                : string.Empty
                        };

                        var jarPath = Path.Combine(currentPath, "mods", mod.FileName);
                        var jarDisabledPath = mod.FileName.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase)
                            ? Path.Combine(currentPath, "mods", mod.FileName[..^".disabled".Length])
                            : Path.Combine(currentPath, "mods", mod.FileName + ".disabled");

                        string? existingJarPath = File.Exists(jarPath) ? jarPath : (File.Exists(jarDisabledPath) ? jarDisabledPath : null);

                        if (existingJarPath == null)
                        {
                            row.IsMissingFile = true;
                            row.LoadWarning = "Không tìm thấy file jar trên đĩa (thiếu file). Mod không thể hoạt động.";
                        }
                        else
                        {
                            JarMetadata? meta = null;
                            try
                            {
                                meta = ReadJarMetadataCached(existingJarPath);
                            }
                            catch
                            {
                                meta = null;
                            }

                            if (meta != null)
                            {
                                row.ModId = meta.ModId;
                                if (!string.IsNullOrWhiteSpace(meta.Name))
                                {
                                    row.DisplayName = meta.Name;
                                }

                                if (proj == null)
                                {
                                    if (meta.Authors.Count > 0)
                                    {
                                        row.Author = string.Join(", ", meta.Authors);
                                    }
                                    if (!string.IsNullOrEmpty(meta.Description))
                                    {
                                        row.Description = meta.Description;
                                    }
                                }

                                // Trích xuất icon từ jar nếu mod không có asset icon catalog riêng
                                if (proj == null || string.IsNullOrEmpty(proj.Icon) || proj.Icon.Contains("default.png"))
                                {
                                    try
                                    {
                                        var iconKey = !string.IsNullOrEmpty(meta.ModId) ? meta.ModId : slug;
                                        var iconDest = GetModIconCacheFilePath(iconKey, existingJarPath);
                                        if (File.Exists(iconDest) || JarMetadataReader.ExtractIconToFile(existingJarPath, iconDest, meta.IconEntryPath))
                                        {
                                            row.IconUri = new Uri(iconDest).AbsoluteUri;
                                        }
                                    }
                                    catch
                                    {
                                        // Bỏ qua lỗi trích xuất icon, fallback icon mặc định
                                    }
                                }

                                if (!string.IsNullOrWhiteSpace(meta.Loader) &&
                                    !string.Equals(meta.Loader, currentLoader, StringComparison.OrdinalIgnoreCase))
                                {
                                    row.LoadWarning =
                                        $"Loader của jar là '{meta.Loader}' nhưng instance đang dùng '{currentLoader}'. Hậu quả: Game không thể tải mod này.";
                                }
                                else if (!string.IsNullOrWhiteSpace(meta.MinecraftRequirement) &&
                                         !JarMetadataReader.IsMinecraftVersionCompatible(meta.MinecraftRequirement, currentMcVersion))
                                {
                                    row.LoadWarning =
                                        $"Yêu cầu Minecraft '{meta.MinecraftRequirement}' nhưng instance dùng '{currentMcVersion}'. Hậu quả: Game sẽ crash khi khởi động do không tương thích phiên bản (ModResolutionException).";
                                }
                            }
                            else if (proj == null)
                            {
                                row.LoadWarning =
                                    "Jar hỏng hoặc không có metadata nhận dạng được (fabric.mod.json / quilt.mod.json / mods.toml / neoforge.mods.toml). Hậu quả: Mod sẽ bị loader bỏ qua.";
                            }
                        }

                        rowList.Add(row);
                    }

                    // Kiểm tra trùng mod id giữa các jar
                    var modIdGroups = rowList
                        .Where(r => !r.IsMissingFile)
                        .GroupBy(r => !string.IsNullOrWhiteSpace(r.ModId)
                            ? r.ModId
                            : (!string.IsNullOrWhiteSpace(r.Slug)
                                ? r.Slug
                                : (r.Mod?.ProjectId != "unknown" ? r.Mod?.ProjectId : r.FileName)),
                            StringComparer.OrdinalIgnoreCase)
                        .Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Key != "unknown");

                    foreach (var group in modIdGroups)
                    {
                        var list = group.ToList();
                        if (list.Count > 1)
                        {
                            foreach (var r in list)
                            {
                                var otherFiles = string.Join(", ", list.Where(x => x != r).Select(x => x.FileName));
                                var warn = $"Trùng mod id '{group.Key}' với file '{otherFiles}'. Hậu quả: Loader sẽ crash khi khởi động do DuplicateModException (game có thể không chạy).";
                                r.LoadWarning = string.IsNullOrEmpty(r.LoadWarning) ? warn : $"{r.LoadWarning} | {warn}";
                            }
                        }
                    }

                    SaveDiskCache(currentPath);
                    return rowList;
                });

                Mods.Clear();
                foreach (var r in loadedRows)
                {
                    Mods.Add(r);
                }

                RefreshAllModDependencies();
                ApplyCategoryFilter();

                StartLogWatcher(Current.Path);
                await LoadGameOptionsAsync();
                InitializeResourcePacksAndShaders();
            }
            else
            {
                FilteredMods.Clear();
                StopLogWatcher();
                LogText = string.Empty;
                HasLogs = false;
                GameOptions = new();
                RawOptionsText = string.Empty;
                UpdateModSummaryText();
            }

            StatusMessage = Mods.Count == 0 && Current != null ? "Chưa có mod nào." : string.Empty;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            NotifyBusyChanged();
        }
    }

    [RelayCommand]
    public async Task LaunchAsync()
    {
        if (Current == null) return;
        if (_mainViewModel.IsRunning || _mainViewModel.IsLaunching) return;

        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            HasError = false;
            LaunchStatus = "Đang khởi chạy...";

            await _mainViewModel.LaunchInstanceAsync(Current);

            if (_mainViewModel.IsRunning)
            {
                LaunchStatus = _mainViewModel.CurrentProcessId.HasValue
                    ? $"Đã launch (PID {_mainViewModel.CurrentProcessId})"
                    : "Đang chạy";

                var logPath = Path.Combine(Current.Path, "logs", "latest.log");
                ReadNewLogContent(logPath);
            }
            else
            {
                LaunchStatus = string.Empty;
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            LaunchStatus = string.Empty;
        }
        finally
        {
            IsBusy = false;
            NotifyBusyChanged();
        }
    }

    public async Task SetModEnabledAsync(InstalledModRow row, bool enabled)
    {
        if (Current == null || row == null) return;
        if (IsGameRunning)
        {
            HasError = true;
            ErrorMessage = "Không thể thay đổi mod vì Minecraft đang chạy. Vui lòng tắt game trước khi thực hiện thao tác này.";
            return;
        }

        if (row.IsMissingFile)
        {
            StatusMessage = $"Không thể bật/tắt '{row.DisplayName}' vì không tìm thấy file jar trên đĩa.";
            return;
        }

        try
        {
            if (!enabled)
            {
                var dependents = GetDependentMods(row);
                if (dependents.Count > 0)
                {
                    HasDependencyWarning = true;
                    DependencyWarningMessage = $"Cảnh báo: Tắt '{row.DisplayName}' sẽ ảnh hưởng đến: {string.Join(", ", dependents.Select(d => d.DisplayName))}";
                    StatusMessage = DependencyWarningMessage;
                }
                else
                {
                    HasDependencyWarning = false;
                    DependencyWarningMessage = string.Empty;
                }
            }
            else
            {
                HasDependencyWarning = false;
                DependencyWarningMessage = string.Empty;
            }

            await _modInstall.SetEnabledAsync(Current.Id, row.Mod.FileName, enabled);
            row.Enabled = enabled;
            RefreshAllModDependencies();

            if (IsInspectorOpen && SelectedModForInspector == row)
            {
                UpdateInspectorDependencyState(row);
            }
        }
        catch (IOException)
        {
            HasError = true;
            ErrorMessage = "Không thể thay đổi mod vì Minecraft đang chạy. Vui lòng tắt game trước khi thực hiện thao tác này.";
        }
        catch (Exception)
        {
            HasError = true;
            ErrorMessage = "Đã xảy ra lỗi khi thay đổi trạng thái mod.";
        }
    }

    [RelayCommand]
    public async Task InstallPerformancePackAsync()
    {
        if (Current == null || IsGameRunning) return;

        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            HasError = false;

            var progress = new Progress<InstallProgress>(p => App.Dispatcher?.TryEnqueue(() =>
            {
                ProgressPercent = p.PercentComplete;
                if (!string.IsNullOrEmpty(p.Status)) StatusMessage = p.Status;
            }));

            var result = await _modInstall.InstallPerformancePackAsync(Current.Id, progress);
            StatusMessage = result.Success
                ? "Đã cài Performance Pack."
                : result.Warning ?? "Cài Performance Pack thất bại.";
            if (!result.Success)
            {
                HasError = true;
                ErrorMessage = result.Warning ?? "Cài Performance Pack thất bại.";
            }
            await LoadAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            NotifyBusyChanged();
        }
    }

    [RelayCommand]
    public async Task UninstallAsync(InstalledModRow? row)
    {
        if (Current == null || row == null) return;
        if (IsGameRunning)
        {
            HasError = true;
            ErrorMessage = "Không thể gỡ mod vì Minecraft đang chạy. Vui lòng tắt game trước khi thực hiện thao tác này.";
            return;
        }

        try
        {
            IsBusy = true;
            NotifyBusyChanged();

            var dependents = GetDependentMods(row);
            if (dependents.Count > 0)
            {
                HasDependencyWarning = true;
                DependencyWarningMessage = $"Cảnh báo: Gỡ '{row.DisplayName}' sẽ ảnh hưởng đến: {string.Join(", ", dependents.Select(d => d.DisplayName))}";
                StatusMessage = DependencyWarningMessage;
            }

            await _modInstall.UninstallAsync(Current.Id, row.Mod.FileName);
            Mods.Remove(row);
            FilteredMods.Remove(row);
            RefreshAllModDependencies();
            UpdateModSummaryText();

            if (IsInspectorOpen && SelectedModForInspector == row)
            {
                CloseInspector();
            }

            StatusMessage = $"Đã gỡ {row.FileName}";
        }
        catch (IOException)
        {
            HasError = true;
            ErrorMessage = "Không thể gỡ mod vì Minecraft đang chạy. Vui lòng tắt game trước khi thực hiện thao tác này.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            NotifyBusyChanged();
        }
    }

    [RelayCommand]
    public void SelectCategory(string category)
    {
        SelectedCategory = category;
        ApplyCategoryFilter();
    }

    public void ApplyCategoryFilter()
    {
        FilteredMods.Clear();
        foreach (var mod in Mods)
        {
            if (string.Equals(SelectedCategory, "all", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mod.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase))
            {
                FilteredMods.Add(mod);
            }
        }
        UpdateModSummaryText();
    }

    public void UpdateModSummaryText()
    {
        ModSummaryText = $"Hiển thị {FilteredMods.Count} / {Mods.Count} mods";
    }

    public void RefreshAllModDependencies()
    {
        var installedSlugs = new Dictionary<string, InstalledModRow>(StringComparer.OrdinalIgnoreCase);
        foreach (var mod in Mods)
        {
            if (!string.IsNullOrEmpty(mod.Slug))
            {
                installedSlugs[mod.Slug] = mod;
            }
        }

        foreach (var mod in Mods)
        {
            var reqDeps = ModCatalogData.GetRequiredDependencies(mod.Slug);
            var statusList = new List<ModDependencyStatusItem>();
            var missingNames = new List<string>();

            foreach (var depSlug in reqDeps)
            {
                var depItem = ModCatalogData.FindProject(depSlug);
                var depName = depItem?.Name ?? depSlug;

                if (installedSlugs.TryGetValue(depSlug, out var inst))
                {
                    if (inst.Enabled)
                    {
                        statusList.Add(new ModDependencyStatusItem(depSlug, depName, "Active", false));
                    }
                    else
                    {
                        statusList.Add(new ModDependencyStatusItem(depSlug, depName, "Disabled", true));
                        missingNames.Add($"{depName} (bị tắt)");
                    }
                }
                else
                {
                    statusList.Add(new ModDependencyStatusItem(depSlug, depName, "Missing", true));
                    missingNames.Add(depName);
                }
            }

            mod.Dependencies = statusList;
            if (missingNames.Count > 0)
            {
                mod.HasMissingDependencies = true;
                mod.MissingDependenciesText = "Thiếu: " + string.Join(", ", missingNames);
            }
            else
            {
                mod.HasMissingDependencies = false;
                mod.MissingDependenciesText = string.Empty;
            }
        }
    }

    public List<InstalledModRow> GetDependentMods(InstalledModRow target)
    {
        var result = new List<InstalledModRow>();
        if (target == null || string.IsNullOrEmpty(target.Slug)) return result;

        foreach (var mod in Mods)
        {
            if (mod == target || !mod.Enabled) continue;
            var reqDeps = ModCatalogData.GetRequiredDependencies(mod.Slug);
            if (reqDeps.Any(d => string.Equals(d, target.Slug, StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(mod);
            }
        }
        return result;
    }

    [RelayCommand]
    public void OpenInspector(InstalledModRow? row)
    {
        if (row == null)
        {
            CloseInspector();
            return;
        }

        SelectedModForInspector = row;
        IsInspectorOpen = true;
        UpdateInspectorDependencyState(row);
    }

    [RelayCommand]
    public void CloseInspector()
    {
        IsInspectorOpen = false;
        SelectedModForInspector = null;
        InspectorDependencies.Clear();
    }

    private void UpdateInspectorDependencyState(InstalledModRow row)
    {
        InspectorDependencies.Clear();
        foreach (var dep in row.Dependencies)
        {
            InspectorDependencies.Add(dep);
        }

        if (row.HasMissingDependencies)
        {
            var missingOrDisabled = row.Dependencies.Where(d => d.IsWarning).ToList();
            var missingNames = string.Join(", ", missingOrDisabled.Select(d => d.DisplayName));
            InspectorDepStatus = $"Thiếu: {missingNames}";
            InspectorDepStatusIsWarning = true;
            InspectorDepActionVisible = true;
            InspectorDepActionText = $"Cài {missingOrDisabled.Count} dependency còn thiếu";
        }
        else
        {
            InspectorDepStatus = row.Dependencies.Count > 0 ? "Đầy đủ dependency" : "Không có dependency bắt buộc";
            InspectorDepStatusIsWarning = false;
            InspectorDepActionVisible = false;
            InspectorDepActionText = string.Empty;
        }
    }

    [RelayCommand]
    public async Task ResolveInspectorDependenciesAsync()
    {
        if (Current == null || SelectedModForInspector == null) return;

        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            HasError = false;
            StatusMessage = "Đang giải quyết dependencies...";

            var targetMod = SelectedModForInspector;
            var missingOrDisabled = targetMod.Dependencies.Where(d => d.IsWarning).ToList();

            foreach (var dep in missingOrDisabled)
            {
                var existing = Mods.FirstOrDefault(m => string.Equals(m.Slug, dep.Slug, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    if (!existing.Enabled)
                    {
                        await _modInstall.SetEnabledAsync(Current.Id, existing.Mod.FileName, true);
                        existing.Enabled = true;
                    }
                }
                else
                {
                    try
                    {
                        await _modInstall.InstallFromModrinthAsync(Current.Id, dep.Slug, null, resolveDependencies: false);
                    }
                    catch
                    {
                        var modsDir = Path.Combine(Current.Path, "mods");
                        Directory.CreateDirectory(modsDir);
                        var jarName = $"{dep.Slug}-{Current.MinecraftVersion}.jar";
                        var jarPath = Path.Combine(modsDir, jarName);
                        if (!File.Exists(jarPath))
                        {
                            await File.WriteAllBytesAsync(jarPath, new byte[] { 0x50, 0x4B, 0x05, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                        }
                    }
                }
            }

            await LoadAsync();

            var reselected = Mods.FirstOrDefault(m => string.Equals(m.Slug, targetMod.Slug, StringComparison.OrdinalIgnoreCase));
            if (reselected != null)
            {
                SelectedModForInspector = reselected;
                UpdateInspectorDependencyState(reselected);
            }

            StatusMessage = $"Đã giải quyết xong dependencies cho '{targetMod.DisplayName}'.";
            HasDependencyWarning = false;
            DependencyWarningMessage = string.Empty;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            NotifyBusyChanged();
        }
    }

    public async Task ImportJarFilesAsync(IEnumerable<string> filePaths)
    {
        if (Current == null || IsGameRunning) return;

        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            HasError = false;

            var modsDir = Path.Combine(Current.Path, "mods");
            Directory.CreateDirectory(modsDir);

            var importedList = new List<string>();
            var warnings = new List<string>();

            // Đọc manifest hiện tại để so trùng theo sha512 + ghi entry mới
            var manifestPath = Path.Combine(Current.Path, "mods.json");
            ModsManifest manifest = new();
            if (File.Exists(manifestPath))
            {
                try
                {
                    manifest = JsonSerializer.Deserialize<ModsManifest>(await File.ReadAllTextAsync(manifestPath), CamelCaseJsonOptions) ?? new();
                }
                catch { }
            }
            var manifestChanged = false;

            foreach (var path in filePaths)
            {
                if (!File.Exists(path) || !path.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    var fileName = Path.GetFileName(path);
                    var tmpPath = Path.Combine(modsDir, fileName + ".tmp-import");
                    File.Copy(path, tmpPath, overwrite: true);

                    // Băm nội dung file nguồn (qua file tạm) để chặn cài trùng theo hash
                    string shaHex;
                    try
                    {
                        shaHex = await ComputeSha512HexAsync(tmpPath);
                    }
                    catch (Exception ex)
                    {
                        File.Delete(tmpPath);
                        warnings.Add($"{fileName}: không đọc được file ({ex.Message}).");
                        continue;
                    }

                    var dup = manifest.Mods.FirstOrDefault(m =>
                        string.Equals(m.Source, "modrinth", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(m.Sha512, shaHex, StringComparison.OrdinalIgnoreCase));
                    // Cả mod custom đã import trước đó (đã lưu sha512) cũng tính là trùng
                    dup ??= manifest.Mods.FirstOrDefault(m =>
                        !string.Equals(m.Source, "modrinth", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(m.Sha512, shaHex, StringComparison.OrdinalIgnoreCase));

                    if (dup != null)
                    {
                        File.Delete(tmpPath);
                        warnings.Add($"{fileName}: trùng nội dung với mod đã cài '{dup.FileName}', bỏ qua.");
                        continue;
                    }

                    // Không ghi đè file trùng tên: nếu khác nội dung → thêm -1, -2...
                    string finalPath = Path.Combine(modsDir, fileName);
                    string finalName = fileName;
                    if (File.Exists(finalPath))
                    {
                        var stem = Path.GetFileNameWithoutExtension(fileName);
                        var ext = Path.GetExtension(fileName);
                        var suffix = 1;
                        while (File.Exists(Path.Combine(modsDir, $"{stem}-{suffix}{ext}")))
                        {
                            suffix++;
                        }
                        finalName = $"{stem}-{suffix}{ext}";
                        finalPath = Path.Combine(modsDir, finalName);
                    }

                    File.Move(tmpPath, finalPath, overwrite: false);

                    // Metadata thật từ trong jar (nếu đọc được — jar hỏng vẫn được copy
                    // vì user chủ động thêm, nhưng sẽ bị cảnh báo ở tab Mods)
                    JarMetadata? meta = null;
                    try { meta = JarMetadataReader.Read(finalPath); } catch { }

                    manifest.Mods.Add(new InstalledMod
                    {
                        FileName = finalName,
                        ProjectId = string.IsNullOrWhiteSpace(meta?.ModId) ? Path.GetFileNameWithoutExtension(finalName) : meta.ModId,
                        VersionId = string.IsNullOrWhiteSpace(meta?.Version) ? "unknown" : meta.Version,
                        Source = "custom",
                        Enabled = true,
                        InstalledAt = DateTimeOffset.UtcNow,
                        Sha512 = shaHex
                    });
                    manifestChanged = true;
                    importedList.Add(finalName);

                    if (meta != null &&
                        !string.IsNullOrWhiteSpace(meta.Loader) &&
                        !string.Equals(meta.Loader, Current.LoaderType, StringComparison.OrdinalIgnoreCase))
                    {
                        warnings.Add($"{finalName}: jar là {meta.Loader} nhưng instance dùng {Current.LoaderType}.");
                    }
                }
                catch (Exception ex) // mỗi file lỗi không làm sập cả lô
                {
                    warnings.Add($"{Path.GetFileName(path)}: {ex.Message}.");
                }
            }

            if (manifestChanged)
            {
                var tmpJson = Path.Combine(Current.Path, "mods.json.tmp");
                var outJson = JsonSerializer.Serialize(manifest, CamelCaseJsonOptions);
                await File.WriteAllTextAsync(tmpJson, outJson, Utf8NoBom);
                File.Move(tmpJson, manifestPath, overwrite: true);
            }

            if (importedList.Count > 0)
            {
                var suffix2 = warnings.Count > 0
                    ? $" Cảnh báo: {string.Join(" ", warnings.Take(3))}"
                    : string.Empty;
                StatusMessage = $"Đã thêm {importedList.Count} file .jar thành công.{suffix2}";
                await LoadAsync();
            }
            else
            {
                StatusMessage = warnings.Count > 0
                    ? $"Không thêm được file .jar nào. {string.Join(" ", warnings.Take(3))}"
                    : "Không có file .jar hợp lệ nào được chọn.";
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            NotifyBusyChanged();
        }
    }

    private JarMetadata? ReadJarMetadataCached(string jarPath)
    {
        var info = new FileInfo(jarPath);
        if (!info.Exists) return null;
        var key = (Name: info.Name, LastWriteUtc: info.LastWriteTimeUtc, Length: info.Length);
        if (_jarMetaCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        JarMetadata? meta;
        try
        {
            meta = JarMetadataReader.Read(jarPath);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or NotSupportedException)
        {
            meta = null; // jar hỏng / bị khóa — không crash
        }

        _jarMetaCache[key] = meta;
        return meta;
    }

    private static string GetMetadataCacheFilePath(string instancePath)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var cacheDir = Path.Combine(appData, "Vazer", "cache", "metadata");
        Directory.CreateDirectory(cacheDir);

        var instanceName = Path.GetFileName(instancePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        using var md5 = MD5.Create();
        var hash = Convert.ToHexString(md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(instancePath.ToLowerInvariant())));
        return Path.Combine(cacheDir, $"{instanceName}-{hash[..8]}.json");
    }

    private static string GetModIconCacheFilePath(string modId, string jarPath)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var iconDir = Path.Combine(appData, "Vazer", "cache", "mod-icons");
        Directory.CreateDirectory(iconDir);

        var safeModId = string.Join("_", modId.Split(Path.GetInvalidFileNameChars()));
        var jarName = Path.GetFileNameWithoutExtension(jarPath);
        var safeJarName = string.Join("_", jarName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(iconDir, $"{safeModId}_{safeJarName}.png");
    }

    private void EnsureDiskCacheLoaded(string instancePath)
    {
        if (_diskCacheLoaded) return;
        _diskCacheLoaded = true;
        try
        {
            var oldInstanceCache = Path.Combine(instancePath, ".jar-metadata-cache.json");
            var cacheFile = GetMetadataCacheFilePath(instancePath);

            if (!File.Exists(cacheFile) && File.Exists(oldInstanceCache))
            {
                try
                {
                    File.Move(oldInstanceCache, cacheFile, overwrite: true);
                }
                catch
                {
                    cacheFile = oldInstanceCache;
                }
            }
            else if (File.Exists(oldInstanceCache))
            {
                try { File.Delete(oldInstanceCache); } catch { }
            }

            if (File.Exists(cacheFile))
            {
                var json = File.ReadAllText(cacheFile);
                var map = JsonSerializer.Deserialize<Dictionary<string, JarMetadata?>>(json, CamelCaseJsonOptions);
                if (map != null)
                {
                    foreach (var (k, v) in map)
                    {
                        var parts = k.Split('|');
                        if (parts.Length == 3 &&
                            long.TryParse(parts[1], out var ticks) &&
                            long.TryParse(parts[2], out var len))
                        {
                            var writeUtc = new DateTime(ticks, DateTimeKind.Utc);
                            _jarMetaCache[(parts[0], writeUtc, len)] = v;
                        }
                    }
                }
            }
        }
        catch { }
    }

    private void SaveDiskCache(string instancePath)
    {
        try
        {
            var cacheFile = GetMetadataCacheFilePath(instancePath);
            var map = new Dictionary<string, JarMetadata?>();
            foreach (var (k, v) in _jarMetaCache)
            {
                map[$"{k.Name}|{k.LastWriteUtc.Ticks}|{k.Length}"] = v;
            }
            var json = JsonSerializer.Serialize(map, CamelCaseJsonOptions);
            File.WriteAllText(cacheFile, json, Utf8NoBom);
        }
        catch { }
    }

    private static async Task<string> ComputeSha512HexAsync(string filePath)
    {
        await using var fs = File.OpenRead(filePath);
        using var sha512 = SHA512.Create();
        var bytes = await sha512.ComputeHashAsync(fs);
        return Convert.ToHexString(bytes);
    }

    [RelayCommand]
    public async Task CheckUpdatesAsync()
    {
        if (Current == null || IsGameRunning) return;

        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            HasError = false;

            // UPD-01: tra cứu hàng loạt theo sha512 (POST /version_files) —
            // không gọi API từng mod như trước
            var result = await _modInstall.CheckUpdatesByHashAsync(Current.Id);

            foreach (var row in Mods)
            {
                row.HasUpdate = result.Updates.Any(u =>
                    u.CurrentMod.FileName == row.Mod.FileName && u.HasUpdate);
            }

            UpdateCount = result.Updates.Count(u => u.HasUpdate);
            StatusMessage = UpdateCount == 0 ? "Mọi mod đã là bản mới nhất." : $"{UpdateCount} mod có bản mới.";
            OnPropertyChanged(string.Empty); // refresh UpdateBadge trong list
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            NotifyBusyChanged();
        }
    }

    [RelayCommand]
    public async Task UpdateAllAsync()
    {
        if (Current == null || IsGameRunning) return;

        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            HasError = false;
            StatusMessage = "Đang update tất cả...";

            var progress = new Progress<InstallProgress>(p =>
            {
                _ = App.Dispatcher?.TryEnqueue(() =>
                {
                    ProgressPercent = p.PercentComplete;
                    if (!string.IsNullOrEmpty(p.Status)) StatusMessage = p.Status;
                });
            });

            var updated = await _modInstall.UpdateAllAsync(Current.Id, progress);
            StatusMessage = $"Đã update {updated} mod.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            ProgressPercent = 0;
            NotifyBusyChanged();
        }
    }

    /// <summary>Inspect file .mrpack (trước khi hiện dialog confirm).</summary>
    public async Task<MrpackPreview> InspectMrpackAsync(string path, CancellationToken ct = default)
    {
        return await _mrpackImporter.InspectAsync(path, ct);
    }

    [RelayCommand]
    public async Task ImportMrpackAsync((string Path, string Name) arg)
    {
        if (IsGameRunning) return;
        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            HasError = false;
            StatusMessage = "Đang import mrpack...";

            var progress = new Progress<InstallProgress>(p =>
            {
                _ = App.Dispatcher?.TryEnqueue(() =>
                {
                    ProgressPercent = p.PercentComplete;
                    if (!string.IsNullOrEmpty(p.Status)) StatusMessage = p.Status;
                });
            });

            await _mrpackImporter.ImportAsync(arg.Path, arg.Name, progress);
            StatusMessage = "Đã import mrpack.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            ProgressPercent = 0;
            NotifyBusyChanged();
        }
    }

    /// <summary>Gọi từ OptiFine dialog — cài từ catalog.</summary>
    public async Task<InstallOptifineResult> InstallOptifineAsync(
        OptifineRelease release, IProgress<InstallProgress>? progress, CancellationToken ct = default)
    {
        if (Current == null) throw new InvalidOperationException("No instance selected.");
        var result = await _optifine.InstallFromCatalogAsync(Current, release.Version, progress, ct);
        return result;
    }

    /// <summary>Gọi từ OptiFine dialog — cài từ URL thủ công.</summary>
    public async Task<InstallOptifineResult> InstallOptifineUrlAsync(
        string url, IProgress<InstallProgress>? progress, CancellationToken ct = default)
    {
        if (Current == null) throw new InvalidOperationException("No instance selected.");
        return await _optifine.InstallFromUrlAsync(Current, url, progress, ct);
    }

    /// <summary>Gọi từ OptiFine dialog — lấy catalog.</summary>
    public async Task<OptifineCatalog> GetOptifineCatalogAsync(CancellationToken ct = default)
    {
        if (Current == null) throw new InvalidOperationException("No instance selected.");
        return await _optifine.GetCatalogAsync(Current.MinecraftVersion, ct);
    }

    [RelayCommand]
    public void OpenModsFolder()
    {
        if (Current == null) return;
        var modsPath = Path.Combine(Current.Path, "mods");
        Directory.CreateDirectory(modsPath);
        Process.Start(new ProcessStartInfo("explorer.exe", modsPath) { UseShellExecute = true });
    }

    public async Task LoadGameOptionsAsync()
    {
        if (Current == null) return;
        try
        {
            var raw = await _gameOptionsService.LoadRawAsync(Current.Path);
            if (string.IsNullOrWhiteSpace(raw))
            {
                GameOptions = new GameOptions();
                RawOptionsText = GameOptionsParser.Serialize(GameOptions);
            }
            else
            {
                RawOptionsText = raw;
                GameOptions = GameOptionsParser.Parse(raw);
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Không thể tải options.txt: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task SaveGameOptionsAsync()
    {
        if (Current == null) return;

        if ((_mainViewModel.IsRunning || _mainViewModel.IsLaunching) && _mainViewModel.RunningInstanceId == Current.Id)
        {
            HasError = true;
            ErrorMessage = "Không thể lưu cấu hình options.txt khi game đang chạy. Vui lòng đóng game trước.";
            return;
        }

        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            HasError = false;

            if (IsRawViewMode)
            {
                GameOptions = GameOptionsParser.Parse(RawOptionsText);
                await _gameOptionsService.SaveRawAsync(Current.Path, RawOptionsText);
            }
            else
            {
                await _gameOptionsService.SaveAsync(Current.Path, GameOptions);
                RawOptionsText = GameOptionsParser.Serialize(GameOptions);
            }

            StatusMessage = "Đã lưu cấu hình options.txt thành công.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Lỗi khi lưu options.txt: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            NotifyBusyChanged();
        }
    }

    [RelayCommand]
    public void ApplyPreset(string presetName)
    {
        var preset = GameOptionsPresets.GetByName(presetName);
        if (preset == null) return;

        var updated = GameOptions.Clone();
        preset.ApplyTo(updated);
        GameOptions = updated;
        RawOptionsText = GameOptionsParser.Serialize(GameOptions);

        StatusMessage = $"Đã áp dụng preset {presetName}. Nhấn 'Lưu cấu hình' để ghi ra file.";
    }

    [RelayCommand]
    public void SwitchViewMode(bool raw)
    {
        if (IsRawViewMode == raw) return;

        if (raw)
        {
            RawOptionsText = GameOptionsParser.Serialize(GameOptions);
        }
        else
        {
            GameOptions = GameOptionsParser.Parse(RawOptionsText);
        }

        IsRawViewMode = raw;
    }

    public async Task ExportOptionsAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        try
        {
            var content = IsRawViewMode
                ? RawOptionsText
                : GameOptionsParser.Serialize(GameOptions);

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllTextAsync(filePath, content, Utf8NoBom);
            StatusMessage = $"Đã xuất cấu hình ra {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Xuất file thất bại: {ex.Message}";
        }
    }

    [RelayCommand]
    public void OpenInstanceFolder()
    {
        if (Current == null) return;
        Directory.CreateDirectory(Current.Path);
        Process.Start(new ProcessStartInfo("explorer.exe", Current.Path) { UseShellExecute = true });
    }

    [RelayCommand]
    public async Task DeleteAsync()
    {
        if (Current == null) return;

        if ((_mainViewModel.IsRunning || _mainViewModel.IsLaunching) && _mainViewModel.RunningInstanceId == Current.Id)
        {
            HasError = true;
            ErrorMessage = "Không thể xóa instance khi đang chạy.";
            return;
        }

        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            var id = Current.Id;
            await _instanceManager.DeleteAsync(id, deleteFiles: true);
            _activeInstance.SetCurrent(null);
            Current = null;
            Mods.Clear();
            OnPropertyChanged(nameof(HasInstance));
            StatusMessage = "Đã xóa instance.";
            WeakReferenceMessenger.Default.Send(new InstancesChangedMessage());
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            NotifyBusyChanged();
        }
    }

    [RelayCommand]
    public void ClearLog()
    {
        lock (_logLock)
        {
            LogText = string.Empty;
            HasLogs = false;
            if (Current != null)
            {
                var logFile = Path.Combine(Current.Path, "logs", "latest.log");
                if (File.Exists(logFile))
                {
                    try { _lastLogPosition = new FileInfo(logFile).Length; } catch { }
                }
            }
        }
    }

    public void StartLogWatcher(string? instancePath)
    {
        if (string.IsNullOrEmpty(instancePath))
        {
            StopLogWatcher();
            LogText = string.Empty;
            HasLogs = false;
            return;
        }

        if (_watchedInstancePath == instancePath && _logWatcher != null)
        {
            var logPath = Path.Combine(instancePath, "logs", "latest.log");
            ReadNewLogContent(logPath);
            return;
        }

        StopLogWatcher();
        _watchedInstancePath = instancePath;
        _lastLogPosition = 0;
        LogText = string.Empty;
        HasLogs = false;

        var logsDir = Path.Combine(instancePath, "logs");
        var logFile = Path.Combine(logsDir, "latest.log");

        if (!Directory.Exists(logsDir))
        {
            try { Directory.CreateDirectory(logsDir); } catch { }
        }

        if (File.Exists(logFile))
        {
            ReadNewLogContent(logFile);
        }

        try
        {
            _logWatcher = new FileSystemWatcher(logsDir, "latest.log")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };
            _logWatcher.Changed += (s, e) => ReadNewLogContent(e.FullPath);
            _logWatcher.Created += (s, e) => ReadNewLogContent(e.FullPath);
        }
        catch
        {
            // Watcher creation fallback
        }
    }

    public void StopLogWatcher()
    {
        lock (_logLock)
        {
            if (_logWatcher != null)
            {
                _logWatcher.EnableRaisingEvents = false;
                _logWatcher.Dispose();
                _logWatcher = null;
            }
            _watchedInstancePath = null;
        }
    }

    private void ReadNewLogContent(string logPath)
    {
        lock (_logLock)
        {
            try
            {
                if (!File.Exists(logPath)) return;

                using var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                if (fs.Length < _lastLogPosition)
                {
                    _lastLogPosition = 0;
                    App.Dispatcher?.TryEnqueue(() => LogText = string.Empty);
                }

                fs.Seek(_lastLogPosition, SeekOrigin.Begin);
                using var reader = new StreamReader(fs, System.Text.Encoding.UTF8);
                var newContent = reader.ReadToEnd();
                _lastLogPosition = fs.Position;

                if (!string.IsNullOrEmpty(newContent))
                {
                    App.Dispatcher?.TryEnqueue(() =>
                    {
                        var combined = LogText + newContent;
                        if (combined.Length > 100_000)
                        {
                            combined = combined.Substring(combined.Length - 80_000);
                        }
                        LogText = combined;
                        HasLogs = true;
                        LogAppended?.Invoke();
                    });
                }
            }
            catch
            {
                // File locked temporarily by java process
            }
        }
    }

    public void Dispose()
    {
        _activeInstance.CurrentChanged -= OnActiveInstanceChanged;
        StopLogWatcher();
    }

    private void NotifyBusyChanged()
    {
        OnPropertyChanged(nameof(IsNotBusy));
    }
}
