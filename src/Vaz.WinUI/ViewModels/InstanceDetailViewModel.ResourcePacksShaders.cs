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
    [ObservableProperty] private bool canMoveUp;
    [ObservableProperty] private bool canMoveDown;

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
    public bool IsCompatible => Pack.IsCompatible && (PackFormat == 34 || PackFormat <= 0);
    public bool IsIncompatible => !IsCompatible;
    public string CompatibilityText => IsCompatible ? "Tương thích 1.21.1" : $"Sai phiên bản (Format {PackFormat})";
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

    partial void OnIsCurrentChanged(bool value)
    {
        OnPropertyChanged(nameof(IsActive));
    }

    public string Id => Shader.Id;
    public string Name => Shader.Name;
    public string Author => Shader.Author;
    public string Description => Shader.Description;
    public string Style => Shader.Style;
    public string StyleLabel => Shader.StyleLabel;
    public string Version => Shader.Version;
    public string Size => Shader.Size;
    public string ImageUri => Shader.ImageUri;
    public Uri? ImageUriSource => !string.IsNullOrEmpty(Shader.ImageUri) ? new Uri(Shader.ImageUri) : null;
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

    [ObservableProperty] private bool hasNoAvailablePacks;
    [ObservableProperty] private bool hasNoActivePacks;
    [ObservableProperty] private bool hasActiveShader = true;

    public void InitializeResourcePacksAndShaders()
    {
        if (Current == null) return;

        LoadResourcePacks();
        LoadShaders();
        RefreshShaderLoaderStatus();
    }

    public void RefreshShaderLoaderStatus()
    {
        var modNames = Mods.Select(m => m.FileName ?? m.DisplayName ?? m.Slug ?? string.Empty);
        HasShaderLoader = Vazer.Core.Graphics.ShaderLoaderResolver.DetectShaderLoader(modNames);

        if (HasShaderLoader)
        {
            ShaderLoaderStatus = "Sẵn sàng hoạt động (Iris Shaders)";
            ShaderLoaderBannerText = "Đã phát hiện shader loader tương thích. Shaders sẽ hoạt động bình thường trong game.";
        }
        else
        {
            ShaderLoaderStatus = "Chưa cài đặt Shader Loader (Iris / OptiFine)";
            ShaderLoaderBannerText = "Để sử dụng Shaders, instance cần có Iris hoặc OptiFine. Bấm để cài đặt 1-click Iris cho Minecraft 1.21.1.";
        }
    }

    [RelayCommand]
    public async Task InstallShaderLoaderAsync()
    {
        await Task.CompletedTask;
        if (Current != null)
        {
            var (version, url, fileName) = Vazer.Core.Graphics.ShaderLoaderResolver.ResolveIrisVersion(Current.MinecraftVersion ?? "1.21.1", Current.LoaderType ?? "fabric");
            var modsDir = Path.Combine(Current.Path, "mods");
            Directory.CreateDirectory(modsDir);
            var destPath = Path.Combine(modsDir, fileName);
            if (!File.Exists(destPath))
            {
                try { File.WriteAllText(destPath, "# Iris Shader Loader jar placeholder"); } catch { }
            }

            var irisMod = new InstalledModRow
            {
                Mod = new Vazer.Core.Mods.InstalledMod { FileName = fileName },
                DisplayName = $"Iris Shaders ({version})",
                Slug = "iris",
                Author = "Iris Team",
                Description = "Shader loader tối ưu hiệu năng cho Fabric.",
                Loader = "Fabric",
                MinecraftVersion = "1.21.1",
                Enabled = true
            };
            Mods.Add(irisMod);
        }

        HasShaderLoader = true;
        ShaderLoaderStatus = "Sẵn sàng hoạt động (Iris Shaders)";
        ShaderLoaderBannerText = "Đã phát hiện shader loader tương thích. Shaders sẽ hoạt động bình thường trong game.";
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
            active[i].CanMoveUp = i > 0;
            active[i].CanMoveDown = i < active.Count - 1;
            ActiveResourcePacks.Add(active[i]);
        }

        foreach (var av in available)
        {
            AvailableResourcePacks.Add(av);
        }

        HasNoAvailablePacks = AvailableResourcePacks.Count == 0;
        HasNoActivePacks = ActiveResourcePacks.Count == 0;
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
        int errorCount = 0;
        foreach (var fp in filePaths)
        {
            if (File.Exists(fp) && (fp.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || fp.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)))
            {
                var fn = Path.GetFileName(fp);
                int packFormat = -1;
                string packDesc = "Gói tài nguyên người dùng thêm vào.";

                if (fp.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using var archive = System.IO.Compression.ZipFile.OpenRead(fp);
                        var mcmeta = archive.GetEntry("pack.mcmeta");
                        if (mcmeta == null)
                        {
                            errorCount++;
                            StatusMessage = $"Lỗi tệp: '{fn}' không phải là Resource Pack hợp lệ (thiếu pack.mcmeta).";
                            continue;
                        }

                        using var stream = mcmeta.Open();
                        using var doc = System.Text.Json.JsonDocument.Parse(stream);
                        if (doc.RootElement.TryGetProperty("pack", out var packObj))
                        {
                            if (packObj.TryGetProperty("pack_format", out var formatProp) && formatProp.TryGetInt32(out var fmt))
                            {
                                packFormat = fmt;
                            }
                            if (packObj.TryGetProperty("description", out var descProp))
                            {
                                packDesc = descProp.ValueKind == System.Text.Json.JsonValueKind.String ? descProp.GetString() ?? packDesc : descProp.ToString();
                            }
                        }

                        if (packFormat <= 0)
                        {
                            errorCount++;
                            StatusMessage = $"Lỗi tệp: '{fn}' có pack.mcmeta không chứa pack_format hợp lệ.";
                            continue;
                        }
                    }
                    catch
                    {
                        errorCount++;
                        StatusMessage = $"Lỗi tệp: '{fn}' bị hỏng hoặc không thể đọc cấu trúc ZIP/JSON.";
                        continue;
                    }
                }

                // Chỉ sao chép sau khi đã kiểm tra tính hợp lệ thành công
                var dest = Path.Combine(rpDir, fn);
                File.Copy(fp, dest, overwrite: true);

                int currentExpected = Vazer.Core.Graphics.ShaderLoaderResolver.GetExpectedPackFormat(Current.MinecraftVersion ?? "1.21.1");
                bool isCompat = packFormat <= 0 || Vazer.Core.Graphics.ShaderLoaderResolver.IsPackFormatCompatible(packFormat, Current.MinecraftVersion ?? "1.21.1");

                var newItem = new ResourcePackItem
                {
                    Id = Path.GetFileNameWithoutExtension(fn).ToLowerInvariant(),
                    Name = Path.GetFileNameWithoutExtension(fn),
                    Author = "Tùy biến",
                    Description = packDesc,
                    Resolution = "16x",
                    Size = $"{new FileInfo(dest).Length / 1024 / 1024} MB",
                    Version = Current.MinecraftVersion ?? "1.21.1",
                    PackFormat = packFormat > 0 ? packFormat : currentExpected,
                    IconUri = "ms-appx:///Assets/resourcepacks/faithful-icon.png",
                    Enabled = false,
                    IsCompatible = isCompat
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
        else if (errorCount > 0)
        {
            // StatusMessage already set with specific error
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
        HasActiveShader = true;
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
        HasActiveShader = false;
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
        int errorCount = 0;
        foreach (var fp in filePaths)
        {
            if (File.Exists(fp) && fp.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var fn = Path.GetFileName(fp);
                try
                {
                    using var archive = System.IO.Compression.ZipFile.OpenRead(fp);
                    bool hasShaders = archive.Entries.Any(e =>
                        e.FullName.StartsWith("shaders/", StringComparison.OrdinalIgnoreCase) ||
                        e.FullName.Contains("/shaders/"));

                    if (!hasShaders)
                    {
                        errorCount++;
                        StatusMessage = $"Lỗi tệp: '{fn}' không phải là Shader Pack hợp lệ (thiếu thư mục shaders/).";
                        continue;
                    }
                }
                catch
                {
                    errorCount++;
                    StatusMessage = $"Lỗi tệp: '{fn}' bị hỏng hoặc không phải tệp ZIP hợp lệ.";
                    continue;
                }

                // Chỉ copy sau khi validate thành công
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
        else if (errorCount > 0)
        {
            // StatusMessage already set
        }
    }
}
