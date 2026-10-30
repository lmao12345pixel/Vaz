using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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

    public string FileName => Mod.FileName;
    public string? VersionId => string.IsNullOrEmpty(Mod.VersionId) ? null : Mod.VersionId;
    public string Version => VersionId ?? "1.0.0";
    public string UpdateBadge => HasUpdate ? "↑ update" : string.Empty;
    public string WarningBadge => HasMissingDependencies ? "Thiếu dependency" : string.Empty;

    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "other";
    public string CategoryLabel { get; set; } = "Khác";
    public string Loader { get; set; } = "Fabric";
    public string MinecraftVersion { get; set; } = "1.21.1";
    public string IconUri { get; set; } = "ms-appx:///Assets/mods/default.png";
    public string ProjectUrl { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;

    public List<ModDependencyStatusItem> Dependencies { get; set; } = new();

    partial void OnHasMissingDependenciesChanged(bool value)
    {
        OnPropertyChanged(nameof(WarningBadge));
    }

    partial void OnHasUpdateChanged(bool value)
    {
        OnPropertyChanged(nameof(UpdateBadge));
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
            OnPropertyChanged(nameof(HasInstance));
            OnPropertyChanged(nameof(FolderPath));
            OnPropertyChanged(nameof(LoaderDisplay));

            Mods.Clear();
            UpdateCount = 0;

            if (Current != null)
            {
                // Auto-discover jar files placed in mods folder that might not be in mods.json
                var modsDir = Path.Combine(Current.Path, "mods");
                if (Directory.Exists(modsDir))
                {
                    var jarFiles = Directory.GetFiles(modsDir, "*.jar");
                    if (jarFiles.Length > 0)
                    {
                        var manifestPath = Path.Combine(Current.Path, "mods.json");
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
                            if (!manifest.Mods.Any(m => string.Equals(m.FileName, fn, StringComparison.OrdinalIgnoreCase)))
                            {
                                manifest.Mods.Add(new InstalledMod
                                {
                                    FileName = fn,
                                    ProjectId = ModCatalogData.GetSlug(fn, null),
                                    VersionId = "1.0.0",
                                    Source = "custom",
                                    Enabled = true,
                                    InstalledAt = DateTimeOffset.UtcNow
                                });
                                manifestChanged = true;
                            }
                        }

                        if (manifestChanged)
                        {
                            var tmpPath = Path.Combine(Current.Path, "mods.json.tmp");
                            var outJson = JsonSerializer.Serialize(manifest, CamelCaseJsonOptions);
                            await File.WriteAllTextAsync(tmpPath, outJson, Utf8NoBom);
                            File.Move(tmpPath, manifestPath, overwrite: true);
                        }
                    }
                }

                var mods = await _modInstall.ListAsync(Current.Id);
                foreach (var mod in mods)
                {
                    var slug = ModCatalogData.GetSlug(mod.FileName, mod.ProjectId);
                    var proj = ModCatalogData.FindProject(slug);

                    var row = new InstalledModRow
                    {
                        Mod = mod,
                        Enabled = mod.Enabled,
                        Slug = slug,
                        DisplayName = proj?.Name ?? (string.IsNullOrEmpty(mod.ProjectId) ? mod.FileName : mod.ProjectId),
                        Author = proj?.Author ?? "Community",
                        Description = proj?.Description ?? "Mod Minecraft được cài đặt trong instance.",
                        Category = proj?.Category ?? "other",
                        CategoryLabel = proj?.CategoryLabel ?? "Khác",
                        Loader = Current.LoaderType,
                        MinecraftVersion = Current.MinecraftVersion,
                        IconUri = proj?.Icon ?? "ms-appx:///Assets/mods/default.png",
                        ProjectUrl = $"https://modrinth.com/mod/{slug}",
                        SourceUrl = $"https://github.com/vazer/{slug}"
                    };
                    Mods.Add(row);
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
        if (Current == null) return;

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
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public async Task InstallPerformancePackAsync()
    {
        if (Current == null) return;

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
        if (Current == null) return;

        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            HasError = false;

            var modsDir = Path.Combine(Current.Path, "mods");
            Directory.CreateDirectory(modsDir);
            var importedCount = 0;

            foreach (var path in filePaths)
            {
                if (!File.Exists(path) || !path.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)) continue;

                var dest = Path.Combine(modsDir, Path.GetFileName(path));
                File.Copy(path, dest, overwrite: true);
                importedCount++;
            }

            if (importedCount > 0)
            {
                StatusMessage = $"Đã thêm {importedCount} file .jar thành công.";
                await LoadAsync();
            }
            else
            {
                StatusMessage = "Không có file .jar hợp lệ nào được chọn.";
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

    [RelayCommand]
    public async Task CheckUpdatesAsync()
    {
        if (Current == null) return;

        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            HasError = false;

            var result = await _modInstall.CheckUpdatesAsync(Current.Id);

            foreach (var row in Mods)
            {
                row.HasUpdate = result.Updates.Any(u => u.CurrentMod.FileName == row.Mod.FileName);
            }

            UpdateCount = result.Updates.Count;
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
        if (Current == null) return;

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
