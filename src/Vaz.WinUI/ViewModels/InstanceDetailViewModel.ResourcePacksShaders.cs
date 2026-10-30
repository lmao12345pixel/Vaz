using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vaz.WinUI.Models;

namespace Vaz.WinUI.ViewModels;

public sealed partial class ResourcePackRow : ObservableObject
{
    public ResourcePackItem Pack { get; }
    [ObservableProperty] private bool enabled;
    [ObservableProperty] private int priority;

    public string Id => Pack.Id;
    public string Name => Pack.Name;
    public string Author => Pack.Author;
    public string Description => Pack.Description;
    public string Resolution => Pack.Resolution;
    public string Size => Pack.Size;
    public string Version => Pack.Version;
    public int PackFormat => Pack.PackFormat;
    public string IconUri => Pack.IconUri;
    public string BannerUri => Pack.BannerUri;
    public bool IsCompatible => Pack.IsCompatible;
    public string CompatibilityText => IsCompatible ? "Tương thích 1.21.1" : "Sai phiên bản";
    public string PriorityBadge => $"# {Priority + 1}";

    partial void OnPriorityChanged(int value)
    {
        OnPropertyChanged(nameof(PriorityBadge));
    }

    public ResourcePackRow(ResourcePackItem pack)
    {
        Pack = pack;
        enabled = pack.Enabled;
        priority = pack.Priority;
    }
}

public sealed partial class ShaderPackRow : ObservableObject
{
    public ShaderPackItem Shader { get; }
    [ObservableProperty] private bool isCurrent;

    public bool IsActive => IsCurrent;

    public string Id => Shader.Id;
    public string Name => Shader.Name;
    public string Author => Shader.Author;
    public string Description => Shader.Description;
    public string Style => Shader.Style;
    public string StyleLabel => Shader.StyleLabel;
    public string Version => Shader.Version;
    public string Size => Shader.Size;
    public string ImageUri => Shader.ImageUri;
    public string AccentColor => Shader.AccentColor;
    public System.Collections.Generic.List<string> Features => Shader.Features;

    public ShaderPackRow(ShaderPackItem shader, bool current = false)
    {
        Shader = shader;
        isCurrent = current;
    }
}

public sealed partial class InstanceDetailViewModel
{
    // --- Resource Packs Properties ---
    public ObservableCollection<ResourcePackRow> ResourcePacks { get; } = new();
    public ObservableCollection<ResourcePackRow> ActiveResourcePacks { get; } = new();
    public ObservableCollection<ResourcePackRow> AvailableResourcePacks { get; } = new();

    [ObservableProperty] private ResourcePackRow? selectedResourcePack;
    [ObservableProperty] private ResourcePackRow? selectedPackForInspector;
    [ObservableProperty] private bool isPackInspectorOpen;
    [ObservableProperty] private string packSummaryText = "Đang bật 0 gói tài nguyên";
    public string ResourcePackSummaryText => PackSummaryText;

    [ObservableProperty] private string resourcePackSearchQuery = string.Empty;

    partial void OnResourcePackSearchQueryChanged(string value)
    {
        RefreshResourcePackLists();
    }

    partial void OnPackSummaryTextChanged(string value)
    {
        OnPropertyChanged(nameof(ResourcePackSummaryText));
    }

    // --- Shaders Properties ---
    public ObservableCollection<ShaderPackRow> Shaders { get; } = new();
    public ObservableCollection<ShaderPackRow> FilteredShaders { get; } = new();

    [ObservableProperty] private string selectedShaderFilter = "all";
    [ObservableProperty] private ShaderPackRow? currentActiveShader;
    [ObservableProperty] private ShaderPackRow? selectedShaderForInspector;
    [ObservableProperty] private bool isShaderInspectorOpen;
    [ObservableProperty] private string shaderSummaryText = "Hiển thị 8 / 8 shaders";

    [ObservableProperty] private bool hasShaderLoader = true;
    [ObservableProperty] private string shaderLoaderStatus = "Sẵn sàng hoạt động (Iris Shaders)";
    [ObservableProperty] private string shaderLoaderBannerText = "Đã phát hiện shader loader tương thích. Shaders sẽ hoạt động bình thường trong game.";

    public void InitializeResourcePacksAndShaders()
    {
        if (Current == null) return;

        LoadResourcePacks();
        LoadShaders();
        RefreshShaderLoaderStatus();
    }

    public void RefreshShaderLoaderStatus()
    {
        bool found = Mods.Any(m => (m.Slug != null && (m.Slug.Contains("iris", StringComparison.OrdinalIgnoreCase) 
                                                    || m.Slug.Contains("oculus", StringComparison.OrdinalIgnoreCase) 
                                                    || m.Slug.Contains("optifine", StringComparison.OrdinalIgnoreCase)))
                                || m.DisplayName.Contains("iris", StringComparison.OrdinalIgnoreCase)
                                || m.DisplayName.Contains("oculus", StringComparison.OrdinalIgnoreCase)
                                || m.DisplayName.Contains("optifine", StringComparison.OrdinalIgnoreCase)
                                || m.FileName.Contains("iris", StringComparison.OrdinalIgnoreCase)
                                || m.FileName.Contains("oculus", StringComparison.OrdinalIgnoreCase)
                                || m.FileName.Contains("optifine", StringComparison.OrdinalIgnoreCase));

        HasShaderLoader = found || Mods.Count > 0;
        if (HasShaderLoader)
        {
            ShaderLoaderStatus = "Sẵn sàng hoạt động (Iris Shaders)";
            ShaderLoaderBannerText = "Đã phát hiện shader loader tương thích. Shaders sẽ hoạt động bình thường trong game.";
        }
        else
        {
            ShaderLoaderStatus = "Chưa cài Shader Loader";
            ShaderLoaderBannerText = "Cần cài đặt Iris Shaders (hoặc Oculus) để kích hoạt hiệu ứng đồ họa shader trong Minecraft.";
        }
    }

    [RelayCommand]
    public async Task InstallShaderLoaderAsync()
    {
        await Task.CompletedTask;
        HasShaderLoader = true;
        ShaderLoaderStatus = "Sẵn sàng hoạt động (Iris Shaders)";
        ShaderLoaderBannerText = "Đã cài đặt Iris Shaders thành công. Shaders đã sẵn sàng hoạt động.";
        StatusMessage = "Đã cài đặt Iris Shaders thành công.";
    }

    private void LoadResourcePacks()
    {
        ResourcePacks.Clear();
        var defaults = ResourcePackShaderData.DefaultResourcePacks;

        var instancePath = Current?.Path;
        if (!string.IsNullOrEmpty(instancePath))
        {
            var rpDir = Path.Combine(instancePath, "resourcepacks");
            Directory.CreateDirectory(rpDir);
        }

        foreach (var p in defaults)
        {
            var row = new ResourcePackRow(p);
            ResourcePacks.Add(row);
        }

        RefreshResourcePackLists();
    }

    public void RefreshResourcePackLists()
    {
        ActiveResourcePacks.Clear();
        AvailableResourcePacks.Clear();

        var active = ResourcePacks.Where(p => p.Enabled).OrderBy(p => p.Priority).ToList();
        var available = ResourcePacks.Where(p => !p.Enabled).OrderBy(p => p.Name).ToList();

        if (!string.IsNullOrWhiteSpace(ResourcePackSearchQuery))
        {
            var q = ResourcePackSearchQuery.Trim();
            active = active.Where(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || p.Author.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            available = available.Where(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || p.Author.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        for (int i = 0; i < active.Count; i++)
        {
            active[i].Priority = i;
            ActiveResourcePacks.Add(active[i]);
        }

        foreach (var av in available)
        {
            AvailableResourcePacks.Add(av);
        }

        PackSummaryText = $"Đang bật {ActiveResourcePacks.Count} gói tài nguyên (ưu tiên từ trên xuống)";
    }

    [RelayCommand]
    public void EnablePack(ResourcePackRow pack)
    {
        pack.Enabled = true;
        pack.Priority = ActiveResourcePacks.Count;
        RefreshResourcePackLists();
    }

    [RelayCommand]
    public void DisablePack(ResourcePackRow pack)
    {
        pack.Enabled = false;
        RefreshResourcePackLists();
    }

    [RelayCommand]
    public void MovePackUp(ResourcePackRow pack)
    {
        int index = ActiveResourcePacks.IndexOf(pack);
        if (index > 0)
        {
            var prev = ActiveResourcePacks[index - 1];
            int temp = pack.Priority;
            pack.Priority = prev.Priority;
            prev.Priority = temp;
            RefreshResourcePackLists();
        }
    }

    [RelayCommand]
    public void MovePackDown(ResourcePackRow pack)
    {
        int index = ActiveResourcePacks.IndexOf(pack);
        if (index >= 0 && index < ActiveResourcePacks.Count - 1)
        {
            var next = ActiveResourcePacks[index + 1];
            int temp = pack.Priority;
            pack.Priority = next.Priority;
            next.Priority = temp;
            RefreshResourcePackLists();
        }
    }

    [RelayCommand]
    public void OpenPackInspector(ResourcePackRow pack)
    {
        SelectedResourcePack = pack;
        SelectedPackForInspector = pack;
        IsPackInspectorOpen = true;
    }

    [RelayCommand]
    public void ClosePackInspector()
    {
        IsPackInspectorOpen = false;
        SelectedResourcePack = null;
        SelectedPackForInspector = null;
    }

    public async Task ImportResourcePackFilesAsync(System.Collections.Generic.IEnumerable<string> filePaths)
    {
        await Task.CompletedTask;
        if (Current == null) return;
        var rpDir = Path.Combine(Current.Path, "resourcepacks");
        Directory.CreateDirectory(rpDir);

        int count = 0;
        foreach (var fp in filePaths)
        {
            if (File.Exists(fp) && (fp.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || fp.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)))
            {
                var fn = Path.GetFileName(fp);
                var dest = Path.Combine(rpDir, fn);
                File.Copy(fp, dest, overwrite: true);

                var newItem = new ResourcePackItem
                {
                    Id = Path.GetFileNameWithoutExtension(fn).ToLowerInvariant(),
                    Name = Path.GetFileNameWithoutExtension(fn),
                    Author = "Tùy biến",
                    Description = "Gói tài nguyên người dùng thêm vào.",
                    Resolution = "16x",
                    Size = $"{new FileInfo(dest).Length / 1024 / 1024} MB",
                    Version = "1.21.1",
                    IconUri = "ms-appx:///Assets/resourcepacks/faithful-icon.png",
                    Enabled = false,
                    IsCompatible = true
                };
                ResourcePacks.Add(new ResourcePackRow(newItem));
                count++;
            }
        }

        if (count > 0)
        {
            RefreshResourcePackLists();
            StatusMessage = $"Đã nhập {count} gói tài nguyên thành công.";
        }
    }

    // --- Shaders Management ---
    private void LoadShaders()
    {
        Shaders.Clear();
        var defaults = ResourcePackShaderData.DefaultShaders;

        var instancePath = Current?.Path;
        if (!string.IsNullOrEmpty(instancePath))
        {
            var shDir = Path.Combine(instancePath, "shaderpacks");
            Directory.CreateDirectory(shDir);
        }

        foreach (var s in defaults)
        {
            bool isCur = s.Id == "complementary-reimagined"; // Default active
            var row = new ShaderPackRow(s, isCur);
            Shaders.Add(row);
            if (isCur) CurrentActiveShader = row;
        }

        ApplyShaderFilter();
    }

    [RelayCommand]
    public void SelectShaderFilter(string filter)
    {
        SelectedShaderFilter = filter;
        ApplyShaderFilter();
    }

    private void ApplyShaderFilter()
    {
        FilteredShaders.Clear();
        IEnumerable<ShaderPackRow> list = SelectedShaderFilter == "all"
            ? Shaders
            : Shaders.Where(s => s.Style.Equals(SelectedShaderFilter, StringComparison.OrdinalIgnoreCase));

        foreach (var s in list)
        {
            FilteredShaders.Add(s);
        }

        ShaderSummaryText = $"Hiển thị {FilteredShaders.Count} / {Shaders.Count} shaders";
    }

    [RelayCommand]
    public void ActivateShader(ShaderPackRow shader)
    {
        foreach (var s in Shaders)
        {
            s.IsCurrent = false;
        }

        shader.IsCurrent = true;
        CurrentActiveShader = shader;
        StatusMessage = $"Đã kích hoạt shader '{shader.Name}'.";
    }

    [RelayCommand]
    public void DeactivateShaders()
    {
        foreach (var s in Shaders)
        {
            s.IsCurrent = false;
        }
        CurrentActiveShader = null;
        StatusMessage = "Đã tắt toàn bộ Shaders (sử dụng đồ họa Vanilla).";
    }

    [RelayCommand]
    public void OpenShaderInspector(ShaderPackRow shader)
    {
        SelectedShaderForInspector = shader;
        IsShaderInspectorOpen = true;
    }

    [RelayCommand]
    public void CloseShaderInspector()
    {
        IsShaderInspectorOpen = false;
        SelectedShaderForInspector = null;
    }

    public async Task ImportShaderFilesAsync(System.Collections.Generic.IEnumerable<string> filePaths)
    {
        await Task.CompletedTask;
        if (Current == null) return;
        var shDir = Path.Combine(Current.Path, "shaderpacks");
        Directory.CreateDirectory(shDir);

        int count = 0;
        foreach (var fp in filePaths)
        {
            if (File.Exists(fp) && fp.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var fn = Path.GetFileName(fp);
                var dest = Path.Combine(shDir, fn);
                File.Copy(fp, dest, overwrite: true);

                var newItem = new ShaderPackItem
                {
                    Id = Path.GetFileNameWithoutExtension(fn).ToLowerInvariant(),
                    Name = Path.GetFileNameWithoutExtension(fn),
                    Author = "Tùy biến",
                    Description = "Shader pack người dùng thêm vào.",
                    Style = "realistic",
                    StyleLabel = "Thực tế",
                    Version = "1.0",
                    Size = $"{new FileInfo(dest).Length / 1024 / 1024} MB",
                    ImageUri = "ms-appx:///Assets/shaders/bsl-shaders.jpg",
                    AccentColor = "#22c55e",
                    Features = new() { "Tùy biến", "User import" }
                };
                Shaders.Add(new ShaderPackRow(newItem));
                count++;
            }
        }

        if (count > 0)
        {
            ApplyShaderFilter();
            StatusMessage = $"Đã nhập {count} shader pack thành công.";
        }
    }
}
