using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vaz.WinUI.Services;
using Vazer.Core.Abstractions;
using Vazer.Core.Mods;

namespace Vaz.WinUI.ViewModels;

public sealed partial class FilterCheckboxItem : ObservableObject
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Name => DisplayName;

    [ObservableProperty] private bool? isChecked = false;

    public Action? OnChanged { get; set; }

    partial void OnIsCheckedChanged(bool? value)
    {
        OnChanged?.Invoke();
    }
}

/// <summary>
/// ViewModel cho Modrinth-Style Browser (Resource Packs & Shaders):
/// Giao diện Dark Theme giống web modrinth.com với Sidebar Filter và 16:9 Grid Cards.
/// Tái sử dụng toàn bộ logic Infinite Scroll, Sliding Window 100 items và Lazy Load từ ModBrowserViewModel.
/// </summary>
public partial class ModrinthStyleBrowserViewModel : ModBrowserViewModel
{
    public ObservableCollection<FilterCheckboxItem> CategoryFilters { get; } = new();
    public ObservableCollection<FilterCheckboxItem> LoaderFilters { get; } = new();
    public ObservableCollection<FilterCheckboxItem> EnvironmentFilters { get; } = new();

    [ObservableProperty] private bool isResourcePacks = true;
    [ObservableProperty] private bool isShaders;

    public ModrinthStyleBrowserViewModel(
        IModrinthSearchService searchService,
        IModDownloadManager downloadManager,
        ActiveInstanceService activeInstance,
        IModInstallService? legacyModInstall = null)
        : base(searchService, downloadManager, activeInstance, legacyModInstall)
    {
        InitializeFilterOptions();
    }

    private void InitializeFilterOptions()
    {
        // Loaders
        var loaders = new[]
        {
            ("fabric", "Fabric"),
            ("neoforge", "NeoForge"),
            ("forge", "Forge"),
            ("quilt", "Quilt"),
            ("iris", "Iris"),
            ("optifine", "OptiFine")
        };
        foreach (var (id, name) in loaders)
        {
            LoaderFilters.Add(new FilterCheckboxItem
            {
                Id = id,
                DisplayName = name,
                OnChanged = OnFilterChanged
            });
        }

        // Environments
        EnvironmentFilters.Add(new FilterCheckboxItem { Id = "client", DisplayName = "Client", OnChanged = OnFilterChanged });
        EnvironmentFilters.Add(new FilterCheckboxItem { Id = "server", DisplayName = "Server", OnChanged = OnFilterChanged });

        UpdateCategoryList();
    }

    public void SetProjectTypeDirect(string projectType)
    {
        if (string.Equals(projectType, "shader", StringComparison.OrdinalIgnoreCase))
        {
            IsShaders = true;
            IsResourcePacks = false;
        }
        else
        {
            IsResourcePacks = true;
            IsShaders = false;
        }

        SetProjectType(IsShaders ? "shader" : "resourcepack");
        UpdateCategoryList();
    }

    partial void OnIsResourcePacksChanged(bool value)
    {
        if (value)
        {
            IsShaders = false;
            SetProjectType("resourcepack");
            UpdateCategoryList();
            _ = SearchAsync();
        }
    }

    partial void OnIsShadersChanged(bool value)
    {
        if (value)
        {
            IsResourcePacks = false;
            SetProjectType("shader");
            UpdateCategoryList();
            _ = SearchAsync();
        }
    }

    private void UpdateCategoryList()
    {
        CategoryFilters.Clear();

        if (IsShaders)
        {
            var shaderCats = new[]
            {
                ("fantasy", "Fantasy"),
                ("realistic", "Realistic"),
                ("performance", "Performance"),
                ("vibrant", "Vibrant"),
                ("vanilla-like", "Vanilla-like"),
                ("atmosphere", "Atmosphere"),
                ("cinematic", "Cinematic"),
                ("low-end", "Low-End"),
                ("high-end", "High-End")
            };
            foreach (var (id, name) in shaderCats)
            {
                CategoryFilters.Add(new FilterCheckboxItem { Id = id, DisplayName = name, OnChanged = OnFilterChanged });
            }
        }
        else
        {
            var rpCats = new[]
            {
                ("16x", "16x"),
                ("32x", "32x"),
                ("64x", "64x"),
                ("128x+", "128x+"),
                ("faithful", "Faithful"),
                ("realistic", "Realistic"),
                ("medieval", "Medieval"),
                ("modern", "Modern"),
                ("steampunk", "Steampunk"),
                ("cartoon", "Cartoon"),
                ("fantasy", "Fantasy"),
                ("simplistic", "Simplistic")
            };
            foreach (var (id, name) in rpCats)
            {
                CategoryFilters.Add(new FilterCheckboxItem { Id = id, DisplayName = name, OnChanged = OnFilterChanged });
            }
        }
    }

    private void OnFilterChanged()
    {
        _ = SearchAsync();
    }

    [RelayCommand]
    public void ClearFilters()
    {
        foreach (var c in CategoryFilters) c.IsChecked = false;
        foreach (var l in LoaderFilters) l.IsChecked = false;
        foreach (var e in EnvironmentFilters) e.IsChecked = false;
        SelectedGameVersion = "All";
        SelectedSort = "Relevance";
        SearchText = string.Empty;
        _ = SearchAsync();
    }

    protected override string[]? ResolveCategories()
    {
        var checkedCats = CategoryFilters
            .Where(c => c.IsChecked == true)
            .Select(c => c.Id)
            .ToArray();

        return checkedCats.Length > 0 ? checkedCats : null;
    }

    protected override string[]? ResolveLoaders(MinecraftInstance? inst)
    {
        var checkedLoaders = LoaderFilters
            .Where(l => l.IsChecked == true)
            .Select(l => l.Id)
            .ToArray();

        return checkedLoaders.Length > 0 ? checkedLoaders : null;
    }

    protected override string[]? ResolveGameVersions(MinecraftInstance? inst)
    {
        if (SelectedGameVersion == "All") return null;
        if (SelectedGameVersion != "Auto") return new[] { SelectedGameVersion };
        return inst == null ? null : new[] { inst.MinecraftVersion };
    }
}
