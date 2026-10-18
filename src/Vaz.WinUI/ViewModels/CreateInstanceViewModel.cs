using CommunityToolkit.Mvvm.ComponentModel;
using Vazer.Core.Abstractions;

namespace Vaz.WinUI.ViewModels;

/// <summary>
/// ViewModel cho dialog "Tạo instance mới": validate input rồi gọi
/// IInstanceManager (Fabric / NeoForge / Vanilla).
/// </summary>
public sealed partial class CreateInstanceViewModel : ObservableObject
{
    /// <summary>Loader hỗ trợ tạo instance.</summary>
    public static IReadOnlyList<string> Loaders { get; } = new[] { "Fabric", "NeoForge", "Vanilla" };

    /// <summary>Các Minecraft version phổ biến cho picker.</summary>
    public static IReadOnlyList<string> MinecraftVersions { get; } =
        new[] { "1.21.1", "1.21", "1.20.6", "1.20.4", "1.20.1", "1.19.4" };

    private readonly IInstanceManager _instanceManager;

    [ObservableProperty]
    private string name = "New Instance";

    [ObservableProperty]
    private string selectedLoader = "Fabric";

    [ObservableProperty]
    private string selectedMinecraftVersion = "1.21.1";

    [ObservableProperty]
    private string? loaderVersion;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    public CreateInstanceViewModel(IInstanceManager instanceManager)
    {
        _instanceManager = instanceManager;
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
        if (name.Length == 0)
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

        var loaderVersion = string.IsNullOrWhiteSpace(LoaderVersion) ? null : LoaderVersion.Trim();

        try
        {
            IsBusy = true;
            return SelectedLoader switch
            {
                "Fabric" => await _instanceManager.CreateFabricAsync(name, SelectedMinecraftVersion, loaderVersion),
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
