using CommunityToolkit.Mvvm.ComponentModel;
using Vaz.WinUI.Models;
using Vazer.Core.Abstractions;
using Vazer.Core.Instances;
using Vazer.Core.Loaders;
using Vazer.Core.Versions;

namespace Vaz.WinUI.ViewModels;

/// <summary>
/// ViewModel cho dialog "Tạo instance mới": validate input rồi gọi
/// IInstanceCreationService (facade, tự chọn installer phù hợp).
/// </summary>
public sealed partial class CreateInstanceViewModel : ObservableObject
{
    /// <summary>Loader hỗ trợ tạo instance.</summary>
    public static IReadOnlyList<string> Loaders { get; } = new[] { "Fabric", "Quilt", "NeoForge", "Vanilla" };

    /// <summary>Thứ tự hiển thị version trong dropdown: release trước, rồi snapshot, old beta, old alpha.</summary>
    public static int TypeRank(string? type) => (type?.Trim().ToLowerInvariant()) switch
    {
        "release" => 0,
        "snapshot" => 1,
        "old_beta" => 2,
        "old_alpha" => 3,
        _ => 4
    };

    private readonly IInstanceCreationService _creationService;
    private readonly IMinecraftVersionService _versionService;

    [ObservableProperty]
    private string name = "New Instance";

    [ObservableProperty]
    private string selectedLoader = "Fabric";

    [ObservableProperty]
    private string? selectedMinecraftVersion;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private IReadOnlyList<MinecraftVersionInfo> vanillaVersions = Array.Empty<MinecraftVersionInfo>();

    [ObservableProperty]
    private bool isVersionsLoading;

    [ObservableProperty]
    private string? selectedLoaderVersion;

    [ObservableProperty]
    private bool isLoaderVersionsLoading;

    [ObservableProperty]
    private IReadOnlyList<string> loaderVersions = Array.Empty<string>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCreateAndNotBusy))]
    private bool canCreateInstance = true;

    [ObservableProperty]
    private string? createWarningMessage;

    /// <summary>Stage hiện tại của installer (Preparing/Downloading/Installing/Finalizing/Done).</summary>
    [ObservableProperty]
    private string? installStage;

    /// <summary>Text mô tả tiến trình (hiển thị dưới ProgressBar).</summary>
    [ObservableProperty]
    private string? installMessage;

    /// <summary>Progress percent, -1 = indeterminate.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInstallerProgressIndeterminate))]
    private double installerProgressPercent = -1;

    /// <summary>True khi đang cài (disable nút trong dialog).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotInstalling))]
    [NotifyPropertyChangedFor(nameof(CanCreateAndNotBusy))]
    private bool isInstalling;

    /// <summary>Hỗ trợ binding ProgressBar.IsIndeterminate.</summary>
    public bool IsInstallerProgressIndeterminate => InstallerProgressPercent < 0;

    /// <summary>Hỗ trợ binding IsEnabled (ngược lại IsInstalling).</summary>
    public bool IsNotInstalling => !IsInstalling;

    /// <summary>Nút "Tạo": chỉ enable khi hợp lệ VÀ không đang cài.</summary>
    public bool CanCreateAndNotBusy => CanCreateInstance && !IsInstalling;

    public CreateInstanceViewModel(IInstanceCreationService creationService, IMinecraftVersionService versionService)
    {
        _creationService = creationService;
        _versionService = versionService;
        _ = LoadVersionsAsync();
    }

    /// <summary>
    /// Load danh sách Vanilla versions từ service.
    /// </summary>
    private async Task LoadVersionsAsync()
    {
        IsVersionsLoading = true;
        try
        {
            var versions = await _versionService.GetVanillaVersionsAsync();
            // Hiển thị TẤT CẢ version (bỏ Take(10)), group theo Type (Release/Snapshot/Old Beta/Old Alpha)
            VanillaVersions = versions
                .OrderBy(v => TypeRank(v.Type))
                .ThenByDescending(v => v.ReleaseTime)
                .ToList()
                .AsReadOnly();
            // Chọn version mặc định là release gần nhất
            var latestRelease = versions
                .Where(v => string.Equals(v.Type, "release", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(v => v.ReleaseTime)
                .FirstOrDefault();
            if (latestRelease != null)
            {
                SelectedMinecraftVersion = latestRelease.Id;
                _ = LoadLoaderVersionsAsync(SelectedLoader ?? "Fabric", SelectedMinecraftVersion);
            }
        }
        finally
        {
            IsVersionsLoading = false;
        }
    }

    /// <summary>
    /// Load danh sách loader versions khi đổi MC version hoặc Mod Loader.
    /// </summary>
    private async Task LoadLoaderVersionsAsync(string loaderType, string mcVersion)
    {
        IsLoaderVersionsLoading = true;
        try
        {
            var versions = await _versionService.GetLoaderVersionsAsync(loaderType, mcVersion);
            // Lấy version ổn định trước, beta sau
            var stable = versions.Where(v => v.Stable).OrderByDescending(v => v.Version, new LoaderVersionComparer()).Select(v => v.Version).ToList();
            var beta = versions.Where(v => !v.Stable && v.Installable).OrderByDescending(v => v.Version, new LoaderVersionComparer()).Select(v => v.Version).ToList();
            LoaderVersions = stable.Concat(beta).ToList().AsReadOnly();
            // Chọn version mặc định là latest stable hoặc beta đầu tiên
            SelectedLoaderVersion = LoaderVersions.FirstOrDefault();
            UpdateCanCreateInstance();
        }
        finally
        {
            IsLoaderVersionsLoading = false;
        }
    }

    partial void OnSelectedMinecraftVersionChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _ = LoadLoaderVersionsAsync(SelectedLoader ?? "Fabric", value);
        }
    }

    partial void OnSelectedLoaderChanged(string value)
    {
        UpdateCanCreateInstance();
        if (!string.IsNullOrEmpty(SelectedMinecraftVersion))
        {
            _ = LoadLoaderVersionsAsync(value ?? "Fabric", SelectedMinecraftVersion);
        }
    }

    partial void OnSelectedLoaderVersionChanged(string? value)
    {
        UpdateCanCreateInstance();
    }

    private void UpdateCanCreateInstance()
    {
        var mcSelected = !string.IsNullOrEmpty(SelectedMinecraftVersion);
        var loader = SelectedLoader ?? "Vanilla";
        var loaderVersionSelected = loader == "Vanilla" || !string.IsNullOrEmpty(SelectedLoaderVersion);

        CanCreateInstance = mcSelected && loaderVersionSelected;
        CreateWarningMessage = CanCreateInstance ? null : (loader != "Vanilla" && string.IsNullOrEmpty(SelectedLoaderVersion)) 
            ? $"Vui lòng chọn loader version cho {loader}." 
            : (mcSelected ? null : "Chọn Minecraft version.");
    }

    /// <summary>
    /// Đồng bộ lựa chọn từ WizardState vào ViewModel.
    /// </summary>
    public void ApplyWizardState(WizardState? state, string? instanceName = null)
    {
        if (state == null) return;

        if (!string.IsNullOrWhiteSpace(state.SelectedVersion))
        {
            SelectedMinecraftVersion = state.SelectedVersion;
        }

        if (!string.IsNullOrWhiteSpace(state.SelectedLoader))
        {
            SelectedLoader = state.SelectedLoader;
        }

        SelectedLoaderVersion = string.IsNullOrWhiteSpace(state.SelectedLoaderVersion)
            ? null
            : state.SelectedLoaderVersion;

        if (!string.IsNullOrWhiteSpace(instanceName))
        {
            Name = instanceName;
        }
    }

    /// <summary>
    /// Tạo instance trực tiếp từ WizardState và instanceName.
    /// Bind đầy đủ tiến trình thật qua InstallStage, InstallerProgressPercent, InstallMessage.
    /// </summary>
    public async Task<MinecraftInstance?> CreateFromWizardAsync(WizardState? state, string? instanceName = null)
    {
        ApplyWizardState(state, instanceName);
        return await CreateAsync();
    }

    /// <summary>
    /// Validate rồi tạo instance theo loader đã chọn.
    /// Trả về instance vừa tạo, hoặc null nếu fail (xem <see cref="ErrorMessage"/>).
    /// </summary>
    public async Task<MinecraftInstance?> CreateAsync()
    {
        if (IsBusy) return null;

        ErrorMessage = null;

        var name = Name?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name))
        {
            ErrorMessage = "Tên instance không được để trống.";
            return null;
        }

        if (string.IsNullOrEmpty(SelectedLoader))
        {
            ErrorMessage = "Chọn loader.";
            return null;
        }

        if (string.IsNullOrEmpty(SelectedMinecraftVersion))
        {
            ErrorMessage = "Chọn Minecraft version.";
            return null;
        }

        var loaderVersion = string.IsNullOrWhiteSpace(SelectedLoaderVersion) ? null : SelectedLoaderVersion.Trim();

        try
        {
            IsBusy = true;
            IsInstalling = true;
            InstallerProgressPercent = -1;
            InstallStage = InstallerStage.Preparing.ToString();
            InstallMessage = "Đang chuẩn bị...";

            // Progress<T> capture sync context (UI thread) tại đây — installer report
            // từ thread pool nhưng callback vẫn được marshal về UI thread.
            var progress = new Progress<InstallerProgress>(p =>
            {
                InstallerProgressPercent = p.Percent;
                InstallMessage = p.Message;
                InstallStage = p.Stage.ToString();
            });

            var info = await _creationService.CreateAsync(
                name,
                SelectedMinecraftVersion,
                SelectedLoader,
                loaderVersion,
                progress,
                CancellationToken.None);

            return info.ToMinecraftInstance();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return null;
        }
        finally
        {
            IsBusy = false;
            IsInstalling = false;
            InstallerProgressPercent = -1;
            InstallStage = null;
            InstallMessage = null;
        }
    }
}
