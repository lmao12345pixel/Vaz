using CommunityToolkit.Mvvm.ComponentModel;
using Vazer.Core.Abstractions;
using Vazer.Core.Versions;

namespace Vaz.WinUI.ViewModels;

/// <summary>
/// ViewModel cho dialog "Tạo instance mới": validate input rồi gọi
/// IInstanceManager (Fabric / NeoForge / Vanilla).
/// </summary>
public sealed partial class CreateInstanceViewModel : ObservableObject
{
    /// <summary>Loader hỗ trợ tạo instance.</summary>
    public static IReadOnlyList<string> Loaders { get; } = new[] { "Fabric", "Quilt", "NeoForge", "Vanilla" };

    private readonly IInstanceManager _instanceManager;
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
    private bool canCreateInstance = true;

    [ObservableProperty]
    private string? createWarningMessage;

    public CreateInstanceViewModel(IInstanceManager instanceManager, IMinecraftVersionService versionService)
    {
        _instanceManager = instanceManager;
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
            // Lấy top 10 release gần nhất
            VanillaVersions = versions
                .Where(v => v.Type == "release")
                .OrderByDescending(v => v.ReleaseTime)
                .Take(10)
                .ToList()
                .AsReadOnly();
            // Chọn version mặc định là release gần nhất
            if (VanillaVersions.Count > 0)
            {
                SelectedMinecraftVersion = VanillaVersions[0].Id;
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
            return SelectedLoader switch
            {
                "Fabric" => await _instanceManager.CreateFabricAsync(name, SelectedMinecraftVersion, loaderVersion),
                "Quilt" => await _instanceManager.CreateQuiltAsync(name, SelectedMinecraftVersion, loaderVersion),
                "NeoForge" => await _instanceManager.CreateNeoForgeAsync(name, SelectedMinecraftVersion, loaderVersion),
                "Vanilla" => await _instanceManager.CreateVanillaAsync(name, SelectedMinecraftVersion),
                _ => throw new InvalidOperationException($"Loader không hỗ trợ: {SelectedLoader}")
            };
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
