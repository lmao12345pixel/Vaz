using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IconImageSource))]
    private Microsoft.UI.Xaml.Media.ImageSource? customIconSource;

    public string Id => Pack.Id;
    public string Name => Pack.Name;
    public string FileName => !string.IsNullOrEmpty(Pack.FileName) ? Pack.FileName : (Name + ".zip");
    public string Author => Pack.Author;
    public string Description => Pack.Description;
    public string Resolution => Pack.Resolution;
    public string Size => Pack.Size;
    public string Version => Pack.Version;
    public int PackFormat => Pack.PackFormat;
    public string IconUri => Pack.IconUri;
    public string BannerUri => Pack.BannerUri;
    public bool HasBanner => !string.IsNullOrWhiteSpace(BannerUri);
    public Microsoft.UI.Xaml.Media.ImageSource BannerImageSource
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(BannerUri))
            {
                if (Uri.TryCreate(BannerUri, UriKind.RelativeOrAbsolute, out var uri))
                {
                    return new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(uri);
                }
            }
            // Không bao giờ trả về null cho binding ImageSource (crash.txt).
            return Vaz.WinUI.Helpers.UiPlaceholders.Image;
        }
    }
    public Microsoft.UI.Xaml.Media.ImageSource IconImageSource
    {
        get
        {
            if (CustomIconSource != null)
            {
                return CustomIconSource;
            }

            if (!string.IsNullOrWhiteSpace(IconUri))
            {
                if (Uri.TryCreate(IconUri, UriKind.RelativeOrAbsolute, out var uri))
                {
                    return new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(uri);
                }
            }
            return Vaz.WinUI.Helpers.UiPlaceholders.Image;
        }
    }
    public bool IsCompatible => Pack.IsCompatible && (PackFormat == 34 || PackFormat <= 0);
    public bool IsIncompatible => !IsCompatible;
    public string CompatibilityText => IsCompatible ? "Tương thích 1.21.1" : $"Sai phiên bản (Format {PackFormat})";
    public string PriorityBadge => $"# {Priority + 1}";

    public string ViewDetailsAutomationName => $"Xem chi tiết {Name}";
    public string EnablePackAutomationName => $"Bật gói {Name}";
    public string DisablePackAutomationName => $"Tắt gói {Name}";
    public string MoveUpAutomationName => $"Tăng mức ưu tiên {Name}";
    public string MoveDownAutomationName => $"Giảm mức ưu tiên {Name}";
    public string MoreOptionsAutomationName => $"Tùy chọn khác cho {Name}";

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

    /// <summary>
    /// Binding an toàn cho Image.Source trong shader inspector — fallback placeholder
    /// thay vì chuỗi rỗng (lỗi crash.txt: "The value cannot be converted to ImageSource").
    /// </summary>
    public Microsoft.UI.Xaml.Media.ImageSource ShaderImageSource
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Shader.ImageUri) &&
                Uri.TryCreate(Shader.ImageUri, UriKind.RelativeOrAbsolute, out var uri))
            {
                return new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(uri);
            }

            return Vaz.WinUI.Helpers.UiPlaceholders.Image;
        }
    }
    public string AccentColor => Shader.AccentColor;
    public System.Collections.Generic.List<string> Features => Shader.Features;

    public string ViewDetailsAutomationName => $"Xem chi tiết {Name}";
    public string ActivateShaderAutomationName => $"Kích hoạt shader {Name}";

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
        StartFileSystemWatchers(Current.Path);
    }

    public void RefreshShaderLoaderStatus()
    {
        var modNames = Mods.Where(m => !m.IsMissingFile && m.Enabled).Select(m => m.FileName ?? m.DisplayName ?? m.Slug ?? string.Empty);
        HasShaderLoader = Vazer.Core.Graphics.ShaderLoaderResolver.DetectShaderLoader(modNames);

        if (HasShaderLoader)
        {
            ShaderLoaderStatus = "Sẵn sàng hoạt động (Iris Shaders)";
            ShaderLoaderBannerText = "Đã phát hiện shader loader tương thích. Shaders sẽ hoạt động bình thường trong game.";
        }
        else
        {
            ShaderLoaderStatus = "Chưa cài đặt Shader Loader (Iris / OptiFine)";
            ShaderLoaderBannerText = "Để sử dụng Shaders, instance cần có Iris hoặc OptiFine. Nhấn để mở trang tải Iris trên Modrinth.";
        }
    }

    [RelayCommand]
    public async Task OpenIrisDownloadPageAsync()
    {
        await Task.CompletedTask;
        try
        {
            var uri = new Uri("https://modrinth.com/mod/iris");
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }
        catch
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://modrinth.com/mod/iris") { UseShellExecute = true });
            }
            catch { }
        }
        StatusMessage = "Đang mở trang tải Iris Shaders trên Modrinth...";
    }

    public void LoadResourcePacks()
    {
        ResourcePacks.Clear();
        var defaults = ResourcePackShaderData.DefaultResourcePacks;

        var instancePath = Current?.Path;
        System.Collections.Generic.List<string>? activeOptionsPacks = null;
        if (!string.IsNullOrEmpty(instancePath))
        {
            var rpDir = Path.Combine(instancePath, "resourcepacks");
            Directory.CreateDirectory(rpDir);

            var optionsPath = Path.Combine(instancePath, "options.txt");
            if (File.Exists(optionsPath))
            {
                activeOptionsPacks = Vazer.Core.GameOptions.ResourcePackOptionsHelper.ReadActivePacksFromOptionsFile(optionsPath);
            }
        }

        foreach (var p in defaults)
        {
            var row = new ResourcePackRow(p);
            ResourcePacks.Add(row);

            if (!string.IsNullOrEmpty(instancePath) && !string.IsNullOrEmpty(p.FileName))
            {
                var potentialZip = Path.Combine(instancePath, "resourcepacks", p.FileName);
                if (File.Exists(potentialZip))
                {
                    LoadThumbnailInBackground(row, potentialZip);
                }
            }
        }

        // Đọc thêm file zip từ thư mục resourcepacks của instance nếu có
        if (!string.IsNullOrEmpty(instancePath))
        {
            var rpDir = Path.Combine(instancePath, "resourcepacks");
            if (Directory.Exists(rpDir))
            {
                var zipFiles = Directory.GetFiles(rpDir, "*.zip");
                foreach (var zip in zipFiles)
                {
                    var fName = Path.GetFileName(zip);
                    if (!ResourcePacks.Any(rp => string.Equals(rp.FileName, fName, StringComparison.OrdinalIgnoreCase)))
                    {
                        var customItem = new ResourcePackItem
                        {
                            Id = Path.GetFileNameWithoutExtension(fName).ToLowerInvariant(),
                            Name = Path.GetFileNameWithoutExtension(fName),
                            FileName = fName,
                            Author = "Tùy biến",
                            Description = "Gói tài nguyên từ thư mục resourcepacks.",
                            Resolution = "32x",
                            Size = $"{new FileInfo(zip).Length / 1024 / 1024:N1} MB",
                            Version = Current?.MinecraftVersion ?? "1.21.1",
                            PackFormat = 34,
                            IconUri = "ms-appx:///Assets/resourcepacks/faithful-icon.png",
                            BannerUri = "ms-appx:///Assets/resourcepacks/faithful-banner.png",
                            Enabled = false,
                            Priority = 99,
                            IsCompatible = true
                        };
                        var row = new ResourcePackRow(customItem);
                        ResourcePacks.Add(row);
                        LoadThumbnailInBackground(row, zip);
                    }
                }
            }
        }

        // Khôi phục thứ tự và trạng thái từ options.txt nếu có
        if (activeOptionsPacks != null && activeOptionsPacks.Count > 0)
        {
            foreach (var r in ResourcePacks)
            {
                r.Enabled = false;
                r.Priority = 99;
            }

            for (int i = 0; i < activeOptionsPacks.Count; i++)
            {
                var optPack = activeOptionsPacks[i];
                var found = ResourcePacks.FirstOrDefault(rp =>
                    string.Equals(rp.FileName, optPack, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(Path.GetFileName(rp.FileName), Path.GetFileName(optPack), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(rp.Name, optPack, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(rp.Name, Path.GetFileNameWithoutExtension(optPack), StringComparison.OrdinalIgnoreCase));

                if (found != null)
                {
                    found.Enabled = true;
                    found.Priority = i;
                }
            }
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
        OnPropertyChanged(nameof(AvailableEmptyTitle));
        OnPropertyChanged(nameof(AvailableEmptySubtitle));
        PackSummaryText = $"Đang bật {ActiveResourcePacks.Count} gói tài nguyên (ưu tiên từ trên xuống)";

        SyncOptionsTxt();
    }

    private void SyncOptionsTxt()
    {
        if (Current == null || string.IsNullOrEmpty(Current.Path)) return;
        var optionsPath = Path.Combine(Current.Path, "options.txt");
        var activePacks = ActiveResourcePacks.Select(p => !string.IsNullOrEmpty(p.FileName) ? p.FileName : p.Name).ToList();
        try
        {
            Vazer.Core.GameOptions.ResourcePackOptionsHelper.SyncToOptionsFile(optionsPath, activePacks);
        }
        catch { }
    }

    public string AvailableEmptyTitle => string.IsNullOrWhiteSpace(ResourcePackSearchQuery)
        ? (ResourcePacks.Count == 0 ? "Thư mục gói tài nguyên trống" : "Không có gói khả dụng")
        : "Không tìm thấy gói tài nguyên nào";

    public string AvailableEmptySubtitle => string.IsNullOrWhiteSpace(ResourcePackSearchQuery)
        ? (ResourcePacks.Count == 0 ? "Kéo thả file .zip hoặc nhấn '+ Thêm .zip' để cài đặt" : "Tất cả các gói đã được kích hoạt ở cột bên phải")
        : "Thử tìm kiếm với từ khóa khác";

    [RelayCommand]
    public void EnablePack(ResourcePackRow pack)
    {
        // Khi bật gói mới, đặt lên ưu tiên cao nhất (# 1, Priority 0)
        foreach (var p in ResourcePacks.Where(x => x.Enabled && x != pack))
        {
            p.Priority += 1;
        }
        pack.Enabled = true;
        pack.Priority = 0;
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
                    FileName = fn,
                    Author = "Tùy biến",
                    Description = packDesc,
                    Resolution = "16x",
                    Size = $"{new FileInfo(dest).Length / 1024 / 1024:N1} MB",
                    Version = Current.MinecraftVersion ?? "1.21.1",
                    PackFormat = packFormat > 0 ? packFormat : currentExpected,
                    IconUri = "ms-appx:///Assets/resourcepacks/faithful-icon.png",
                    BannerUri = "ms-appx:///Assets/resourcepacks/faithful-banner.png",
                    Enabled = false,
                    IsCompatible = isCompat
                };
                var row = new ResourcePackRow(newItem);
                ResourcePacks.Add(row);
                LoadThumbnailInBackground(row, dest);
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
    public void LoadShaders()
    {
        Shaders.Clear();
        var defaults = ResourcePackShaderData.DefaultShaders;

        var instancePath = Current?.Path;
        if (!string.IsNullOrEmpty(instancePath))
        {
            var shDir = Path.Combine(instancePath, "shaderpacks");
            Directory.CreateDirectory(shDir);

            if (Directory.Exists(shDir))
            {
                var zipFiles = Directory.GetFiles(shDir, "*.zip");
                foreach (var zip in zipFiles)
                {
                    var fName = Path.GetFileName(zip);
                    if (!Shaders.Any(s => string.Equals(s.Name + ".zip", fName, StringComparison.OrdinalIgnoreCase) ||
                                          string.Equals(s.Id, Path.GetFileNameWithoutExtension(fName), StringComparison.OrdinalIgnoreCase)))
                    {
                        var customShader = new ShaderPackItem
                        {
                            Id = Path.GetFileNameWithoutExtension(fName).ToLowerInvariant(),
                            Name = Path.GetFileNameWithoutExtension(fName),
                            Author = "Tùy biến",
                            Description = "Shader pack từ thư mục shaderpacks.",
                            Style = "fantasy",
                            StyleLabel = "Tùy biến",
                            Version = Current?.MinecraftVersion ?? "1.21.1",
                            Size = $"{new FileInfo(zip).Length / 1024 / 1024:N1} MB",
                            ImageUri = "ms-appx:///Assets/shaders/bsl-preview.png",
                            AccentColor = "#6366F1",
                            Features = new System.Collections.Generic.List<string> { "Custom Shader", "Tùy biến" }
                        };
                        Shaders.Add(new ShaderPackRow(customShader, false));
                    }
                }
            }
        }

        foreach (var s in defaults)
        {
            if (!Shaders.Any(sh => sh.Id == s.Id))
            {
                bool isCur = s.Id == "complementary-reimagined"; // Default active
                var row = new ShaderPackRow(s, isCur);
                Shaders.Add(row);
                if (isCur) CurrentActiveShader = row;
            }
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

    private void LoadThumbnailInBackground(ResourcePackRow row, string zipPath)
    {
        if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath)) return;

        _ = Task.Run(async () =>
        {
            byte[]? pngBytes = null;
            try
            {
                using var archive = ZipFile.OpenRead(zipPath);
                var entry = archive.GetEntry("pack.png") ?? archive.Entries.FirstOrDefault(e =>
                    string.Equals(e.FullName, "pack.png", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(Path.GetFileName(e.FullName), "pack.png", StringComparison.OrdinalIgnoreCase));

                if (entry != null)
                {
                    using var stream = entry.Open();
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    pngBytes = ms.ToArray();
                }
            }
            catch
            {
                // File unreadable or invalid zip
            }

            if (pngBytes != null && pngBytes.Length > 0)
            {
                App.Dispatcher?.TryEnqueue(async () =>
                {
                    try
                    {
                        var ras = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                        using (var writer = ras.AsStreamForWrite())
                        {
                            await writer.WriteAsync(pngBytes, 0, pngBytes.Length);
                            await writer.FlushAsync();
                        }
                        ras.Seek(0);
                        var bmp = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                        await bmp.SetSourceAsync(ras);
                        row.CustomIconSource = bmp;
                    }
                    catch
                    {
                        row.CustomIconSource = Vaz.WinUI.Helpers.UiPlaceholders.Image;
                    }
                });
            }
        });
    }

    private FileSystemWatcher? _resourcePacksWatcher;
    private FileSystemWatcher? _shaderPacksWatcher;
    private System.Threading.Timer? _resourcePacksDebounceTimer;
    private System.Threading.Timer? _shadersDebounceTimer;
    private readonly object _watcherLock = new();

    public void StartFileSystemWatchers(string? instancePath)
    {
        DisposeFileSystemWatchers();

        if (string.IsNullOrEmpty(instancePath) || !Directory.Exists(instancePath)) return;

        try
        {
            var rpDir = Path.Combine(instancePath, "resourcepacks");
            Directory.CreateDirectory(rpDir);
            _resourcePacksWatcher = new FileSystemWatcher(rpDir, "*.zip")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            _resourcePacksWatcher.Created += OnResourcePacksDirectoryChanged;
            _resourcePacksWatcher.Deleted += OnResourcePacksDirectoryChanged;
            _resourcePacksWatcher.Renamed += OnResourcePacksDirectoryChanged;
        }
        catch { }

        try
        {
            var shDir = Path.Combine(instancePath, "shaderpacks");
            Directory.CreateDirectory(shDir);
            _shaderPacksWatcher = new FileSystemWatcher(shDir, "*.zip")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            _shaderPacksWatcher.Created += OnShadersDirectoryChanged;
            _shaderPacksWatcher.Deleted += OnShadersDirectoryChanged;
            _shaderPacksWatcher.Renamed += OnShadersDirectoryChanged;
        }
        catch { }
    }

    private void OnResourcePacksDirectoryChanged(object sender, FileSystemEventArgs e)
    {
        lock (_watcherLock)
        {
            _resourcePacksDebounceTimer?.Dispose();
            _resourcePacksDebounceTimer = new System.Threading.Timer(_ =>
            {
                App.Dispatcher?.TryEnqueue(() =>
                {
                    LoadResourcePacks();
                });
            }, null, 400, System.Threading.Timeout.Infinite);
        }
    }

    private void OnShadersDirectoryChanged(object sender, FileSystemEventArgs e)
    {
        lock (_watcherLock)
        {
            _shadersDebounceTimer?.Dispose();
            _shadersDebounceTimer = new System.Threading.Timer(_ =>
            {
                App.Dispatcher?.TryEnqueue(() =>
                {
                    LoadShaders();
                });
            }, null, 400, System.Threading.Timeout.Infinite);
        }
    }

    public void DisposeFileSystemWatchers()
    {
        lock (_watcherLock)
        {
            _resourcePacksDebounceTimer?.Dispose();
            _resourcePacksDebounceTimer = null;

            _shadersDebounceTimer?.Dispose();
            _shadersDebounceTimer = null;

            if (_resourcePacksWatcher != null)
            {
                _resourcePacksWatcher.EnableRaisingEvents = false;
                _resourcePacksWatcher.Created -= OnResourcePacksDirectoryChanged;
                _resourcePacksWatcher.Deleted -= OnResourcePacksDirectoryChanged;
                _resourcePacksWatcher.Renamed -= OnResourcePacksDirectoryChanged;
                _resourcePacksWatcher.Dispose();
                _resourcePacksWatcher = null;
            }

            if (_shaderPacksWatcher != null)
            {
                _shaderPacksWatcher.EnableRaisingEvents = false;
                _shaderPacksWatcher.Created -= OnShadersDirectoryChanged;
                _shaderPacksWatcher.Deleted -= OnShadersDirectoryChanged;
                _shaderPacksWatcher.Renamed -= OnShadersDirectoryChanged;
                _shaderPacksWatcher.Dispose();
                _shaderPacksWatcher = null;
            }
        }
    }
}
