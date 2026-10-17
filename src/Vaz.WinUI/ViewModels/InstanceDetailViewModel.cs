using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vaz.WinUI.Services;
using Vazer.Core.Abstractions;
using Vazer.Core.Loaders.Optifine;
using Vazer.Core.Modpacks;
using Vazer.Core.Mods;

namespace Vaz.WinUI.ViewModels;

/// <summary>Row view cho mod đã cài.</summary>
public sealed class InstalledModRow
{
    public required InstalledMod Mod { get; init; }
    public bool HasUpdate { get; set; }

    public string FileName => Mod.FileName;
    public string? VersionId => string.IsNullOrEmpty(Mod.VersionId) ? null : Mod.VersionId;
    public bool Enabled { get; set; }
    public string UpdateBadge => HasUpdate ? "↑ update" : string.Empty;
}

/// <summary>
/// ViewModel cho Instance Detail page: launch, danh sách mod, update,
/// mrpack, OptiFine, mở mods folder, settings (ReadOnly) + delete.
/// </summary>
public sealed partial class InstanceDetailViewModel : ObservableObject
{
    private readonly IInstanceManager _instanceManager;
    private readonly IModInstallService _modInstall;
    private readonly IMrpackImporter _mrpackImporter;
    private readonly IOptifineInstallerService _optifine;
    private readonly IGameLauncher _launcher;
    private readonly IJavaResolver _javaResolver;
    private readonly IAccountStore _accountStore;
    private readonly ActiveInstanceService _activeInstance;

    public ObservableCollection<InstalledModRow> Mods { get; } = new();

    [ObservableProperty] private MinecraftInstance? current;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private int progressPercent;
    [ObservableProperty] private int updateCount;
    [ObservableProperty] private string launchStatus = string.Empty;

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
        IGameLauncher launcher,
        IJavaResolver javaResolver,
        IAccountStore accountStore,
        ActiveInstanceService activeInstance)
    {
        _instanceManager = instanceManager;
        _modInstall = modInstall;
        _mrpackImporter = mrpackImporter;
        _optifine = optifine;
        _launcher = launcher;
        _javaResolver = javaResolver;
        _accountStore = accountStore;
        _activeInstance = activeInstance;
        _activeInstance.CurrentChanged += (_, _) => _ = LoadAsync();
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
                var mods = await _modInstall.ListAsync(Current.Id);
                foreach (var mod in mods)
                {
                    Mods.Add(new InstalledModRow { Mod = mod, Enabled = mod.Enabled });
                }
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

        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            HasError = false;
            LaunchStatus = "Đang chuẩn bị launch...";

            var account = await _accountStore.GetActiveAsync();
            if (account == null)
            {
                HasError = true;
                ErrorMessage = "Chưa có tài khoản active — thêm tài khoản ở trang Accounts.";
                LaunchStatus = string.Empty;
                return;
            }

            var java = await _javaResolver.EnsureAsync(
                _javaResolver.RequiredJava(Current.MinecraftVersion),
                progress: null);

            var result = await _launcher.LaunchAsync(new LaunchParameters
            {
                InstanceId = Current.Id,
                InstancePath = Current.Path,
                Account = account,
                Java = java,
                MinecraftVersion = Current.MinecraftVersion,
                MaxRamMb = 4096
            });

            LaunchStatus = $"Đã launch (PID {result.ProcessId})";
            await _instanceManager.UpdateLastPlayedAsync(Current.Id);
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
            await _modInstall.SetEnabledAsync(Current.Id, row.Mod.FileName, enabled);
            row.Enabled = enabled;
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

            var progress = new Progress<InstallProgress>(p => App.Dispatcher.TryEnqueue(() =>
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
            await _modInstall.UninstallAsync(Current.Id, row.Mod.FileName);
            Mods.Remove(row);
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
                _ = App.Dispatcher.TryEnqueue(() =>
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
                _ = App.Dispatcher.TryEnqueue(() =>
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

    [RelayCommand]
    public async Task DeleteAsync()
    {
        if (Current == null) return;

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

    private void NotifyBusyChanged()
    {
        OnPropertyChanged(nameof(IsNotBusy));
    }
}
