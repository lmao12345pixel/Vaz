using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
            if (HasBanner && Uri.TryCreate(BannerUri, UriKind.RelativeOrAbsolute, out var uri))
            {
                return new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(uri);
            }
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

            if (!string.IsNullOrWhiteSpace(IconUri) && Uri.TryCreate(IconUri, UriKind.RelativeOrAbsolute, out var uri))
            {
                return new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(uri);
            }

            return Vaz.WinUI.Helpers.UiPlaceholders.Image;
        }
    }

    public bool IsCompatible => Pack.IsCompatible && (PackFormat == 34 || PackFormat <= 0);
    public bool IsIncompatible => !IsCompatible;
    public string CompatibilityText => IsCompatible ? "Tương thích 1.21.1" : $"Sai phiên bản (Format {PackFormat})";

    public string ViewDetailsAutomationName => $"Xem chi tiết {Name}";
    public string DeletePackAutomationName => $"Xóa gói {Name}";
    public string OpenFileAutomationName => $"Mở vị trí tệp {Name}";

    public ResourcePackRow(ResourcePackItem pack)
    {
        Pack = pack;
    }
}

public sealed partial class ShaderPackRow : ObservableObject
{
    public ShaderPackItem Shader { get; }

    public string Id => Shader.Id;
    public string Name => Shader.Name;
    public string FileName => Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? Name : (Name + ".zip");
    public string Author => Shader.Author;
    public string Description => Shader.Description;
    public string Style => Shader.Style;
    public string StyleLabel => Shader.StyleLabel;
    public string Version => Shader.Version;
    public string Size => Shader.Size;
    public string ImageUri => Shader.ImageUri;

    public Uri? ImageUriSource
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Shader.ImageUri) &&
                Uri.TryCreate(Shader.ImageUri, UriKind.RelativeOrAbsolute, out var uri))
            {
                return uri;
            }
            return null;
        }
    }

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
    public string DeleteShaderAutomationName => $"Xóa shader {Name}";
    public string OpenFileAutomationName => $"Mở vị trí tệp {Name}";

    public ShaderPackRow(ShaderPackItem shader)
    {
        Shader = shader;
    }
}

public partial class InstanceDetailViewModel
{
    // --- Resource Packs Library ---
    public ObservableCollection<ResourcePackRow> ResourcePacks { get; } = new();
    public ObservableCollection<ResourcePackRow> FilteredResourcePacks { get; } = new();

    [ObservableProperty] private ResourcePackRow? selectedResourcePack;
    [ObservableProperty] private ResourcePackRow? selectedPackForInspector;
    [ObservableProperty] private bool isPackInspectorOpen;
    [ObservableProperty] private string resourcePackSummaryText = "Đã cài 0 gói tài nguyên";
    [ObservableProperty] private string resourcePacksCountText = "0";
    [ObservableProperty] private string resourcePackSearchQuery = string.Empty;
    [ObservableProperty] private bool hasNoPacks;

    partial void OnResourcePackSearchQueryChanged(string value) => RefreshResourcePackLists();

    // --- Shaders Library ---
    public ObservableCollection<ShaderPackRow> Shaders { get; } = new();
    public ObservableCollection<ShaderPackRow> FilteredShaders { get; } = new();

    [ObservableProperty] private string selectedShaderFilter = "all";
    [ObservableProperty] private ShaderPackRow? selectedShaderForInspector;
    [ObservableProperty] private bool isShaderInspectorOpen;
    [ObservableProperty] private string shaderSummaryText = "Đã cài 0 shaders";
    [ObservableProperty] private bool hasShaderLoader = true;
    [ObservableProperty] private string shaderLoaderStatus = "Đã phát hiện Iris Shader Loader";
    [ObservableProperty] private string shaderLoaderBannerText = "Instance của bạn đang sử dụng Fabric + Iris Shaders tương thích hoàn hảo.";
    [ObservableProperty] private bool hasNoShaders;
    [ObservableProperty] private string shaderSearchQuery = string.Empty;

    partial void OnShaderSearchQueryChanged(string value) => FilterShaders();

    public void InitializeResourcePacksAndShaders()
    {
        LoadResourcePacks();
        LoadShaders();
        StartFileSystemWatchers(Current?.Path);
    }

    public void LoadResourcePacks()
    {
        ResourcePacks.Clear();
        var defaults = ResourcePackShaderData.DefaultResourcePacks;

        var instancePath = Current?.Path;

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
                        var fi = new FileInfo(zip);
                        long sizeKb = fi.Length / 1024;
                        string sizeStr = sizeKb > 1024 ? $"{(sizeKb / 1024.0):F1} MB" : $"{sizeKb} KB";

                        string desc = "Gói tài nguyên đã cài đặt.";
                        string res = "32x";
                        int packFormat = 34;

                        try
                        {
                            using var archive = ZipFile.OpenRead(zip);
                            var mcmetaEntry = archive.GetEntry("pack.mcmeta");
                            if (mcmetaEntry != null)
                            {
                                using var s = mcmetaEntry.Open();
                                using var doc = JsonDocument.Parse(s);
                                if (doc.RootElement.TryGetProperty("pack", out var packEl))
                                {
                                    if (packEl.TryGetProperty("description", out var dEl))
                                    {
                                        desc = dEl.ValueKind == JsonValueKind.String ? (dEl.GetString() ?? desc) : dEl.ToString();
                                    }
                                    if (packEl.TryGetProperty("pack_format", out var fEl) && fEl.TryGetInt32(out var pf))
                                    {
                                        packFormat = pf;
                                    }
                                }
                            }
                        }
                        catch { }

                        var item = new ResourcePackItem
                        {
                            Id = Path.GetFileNameWithoutExtension(fName).ToLowerInvariant(),
                            Slug = Path.GetFileNameWithoutExtension(fName).ToLowerInvariant(),
                            Name = Path.GetFileNameWithoutExtension(fName),
                            FileName = fName,
                            Author = "Tùy biến",
                            Description = desc,
                            Resolution = res,
                            Size = sizeStr,
                            Version = "1.21.1",
                            PackFormat = packFormat,
                            IsCompatible = packFormat == 34 || packFormat <= 0
                        };

                        var newRow = new ResourcePackRow(item);
                        ResourcePacks.Add(newRow);

                        LoadThumbnailInBackground(newRow, zip);
                    }
                }
            }
        }

        RefreshResourcePackLists();
    }

    public void RefreshResourcePackLists()
    {
        FilteredResourcePacks.Clear();

        var query = ResourcePackSearchQuery?.Trim() ?? string.Empty;
        var filtered = ResourcePacks.AsEnumerable();

        if (!string.IsNullOrEmpty(query))
        {
            filtered = filtered.Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Author.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var p in filtered)
        {
            FilteredResourcePacks.Add(p);
        }

        HasNoPacks = FilteredResourcePacks.Count == 0;
        ResourcePacksCountText = ResourcePacks.Count.ToString();
        ResourcePackSummaryText = $"Đã cài {ResourcePacks.Count} gói tài nguyên";
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
        SelectedPackForInspector = null;
    }

    [RelayCommand]
    public void OpenResourcePackFile(ResourcePackRow pack)
    {
        if (pack == null) return;
        var instancePath = Current?.Path;
        if (string.IsNullOrEmpty(instancePath)) return;

        var filePath = Path.Combine(instancePath, "resourcepacks", pack.FileName);
        if (File.Exists(filePath))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{filePath}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Không thể mở vị trí tệp: {ex.Message}";
            }
        }
        else
        {
            var rpDir = Path.Combine(instancePath, "resourcepacks");
            Directory.CreateDirectory(rpDir);
            try
            {
                Process.Start(new ProcessStartInfo { FileName = rpDir, UseShellExecute = true });
            }
            catch { }
        }
    }

    [RelayCommand]
    public void DeleteResourcePack(ResourcePackRow pack)
    {
        if (pack == null) return;
        var instancePath = Current?.Path;
        if (!string.IsNullOrEmpty(instancePath))
        {
            var filePath = Path.Combine(instancePath, "resourcepacks", pack.FileName);
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Không thể xóa tệp: {ex.Message}";
                    _notificationService?.ShowError("Không thể xóa gói tài nguyên", ex.Message);
                    return;
                }
            }
        }

        ResourcePacks.Remove(pack);
        if (SelectedPackForInspector == pack)
        {
            ClosePackInspector();
        }
        RefreshResourcePackLists();
        StatusMessage = $"Đã xóa gói {pack.Name}.";
        _notificationService?.ShowSuccess($"Đã xóa gói {pack.Name}", "Gói tài nguyên đã bị xóa khỏi thư mục.");
    }

    public async Task ImportResourcePackFilesAsync(System.Collections.Generic.IEnumerable<string> filePaths)
    {
        await Task.Yield();
        var instancePath = Current?.Path;
        if (string.IsNullOrEmpty(instancePath)) return;

        var rpDir = Path.Combine(instancePath, "resourcepacks");
        Directory.CreateDirectory(rpDir);

        int count = 0;
        int errorCount = 0;

        foreach (var fp in filePaths)
        {
            if (File.Exists(fp) && fp.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var fn = Path.GetFileName(fp);

                try
                {
                    using var archive = ZipFile.OpenRead(fp);
                    var mcmetaEntry = archive.GetEntry("pack.mcmeta");
                    if (mcmetaEntry == null)
                    {
                        errorCount++;
                        StatusMessage = $"Lỗi tệp: '{fn}' không phải là Resource Pack hợp lệ (thiếu pack.mcmeta).";
                        continue;
                    }
                }
                catch
                {
                    errorCount++;
                    StatusMessage = $"Lỗi tệp: '{fn}' bị hỏng hoặc không phải tệp ZIP hợp lệ.";
                    continue;
                }

                var dest = Path.Combine(rpDir, fn);
                File.Copy(fp, dest, overwrite: true);

                var newItem = new ResourcePackItem
                {
                    Id = Path.GetFileNameWithoutExtension(fn).ToLowerInvariant(),
                    Slug = Path.GetFileNameWithoutExtension(fn).ToLowerInvariant(),
                    Name = Path.GetFileNameWithoutExtension(fn),
                    FileName = fn,
                    Author = "Tùy biến",
                    Description = "Gói tài nguyên người dùng thêm vào.",
                    Resolution = "32x",
                    Size = "15 MB",
                    Version = "1.21.1",
                    PackFormat = 34,
                    IsCompatible = true
                };

                var newRow = new ResourcePackRow(newItem);
                ResourcePacks.Add(newRow);

                LoadThumbnailInBackground(newRow, dest);
                count++;
            }
        }

        if (count > 0)
        {
            RefreshResourcePackLists();
            StatusMessage = $"Đã thêm {count} gói tài nguyên vào thư viện.";
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
                    if (!Shaders.Any(s => string.Equals(s.FileName, fName, StringComparison.OrdinalIgnoreCase)))
                    {
                        var fi = new FileInfo(zip);
                        long sizeKb = fi.Length / 1024;
                        string sizeStr = sizeKb > 1024 ? $"{(sizeKb / 1024.0):F1} MB" : $"{sizeKb} KB";

                        var item = new ShaderPackItem
                        {
                            Id = Path.GetFileNameWithoutExtension(fName).ToLowerInvariant(),
                            Name = Path.GetFileNameWithoutExtension(fName),
                            Author = "Tùy biến",
                            Description = "Shader pack đã cài đặt.",
                            Style = "realistic",
                            StyleLabel = "Thực tế",
                            Version = "1.0.0",
                            Size = sizeStr,
                            AccentColor = "#3b82f6"
                        };
                        Shaders.Add(new ShaderPackRow(item));
                    }
                }
            }
        }

        foreach (var s in defaults)
        {
            if (!Shaders.Any(existing => existing.Id == s.Id))
            {
                Shaders.Add(new ShaderPackRow(s));
            }
        }

        FilterShaders();
    }

    public void SelectShaderFilter(string filter)
    {
        SelectedShaderFilter = filter;
        FilterShaders();
    }

    private void FilterShaders()
    {
        FilteredShaders.Clear();
        var query = ShaderSearchQuery?.Trim() ?? string.Empty;

        var items = Shaders.AsEnumerable();
        if (SelectedShaderFilter != "all")
        {
            items = items.Where(s => s.Style.Equals(SelectedShaderFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(query))
        {
            items = items.Where(s =>
                s.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                s.Author.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var s in items)
        {
            FilteredShaders.Add(s);
        }

        ShaderSummaryText = $"Đã cài {Shaders.Count} shaders (Hiển thị {FilteredShaders.Count})";
        HasNoShaders = FilteredShaders.Count == 0;
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

    [RelayCommand]
    public void OpenShaderFile(ShaderPackRow shader)
    {
        if (shader == null) return;
        var instancePath = Current?.Path;
        if (string.IsNullOrEmpty(instancePath)) return;

        var filePath = Path.Combine(instancePath, "shaderpacks", shader.FileName);
        if (File.Exists(filePath))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{filePath}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Không thể mở vị trí tệp: {ex.Message}";
            }
        }
        else
        {
            var shDir = Path.Combine(instancePath, "shaderpacks");
            Directory.CreateDirectory(shDir);
            try
            {
                Process.Start(new ProcessStartInfo { FileName = shDir, UseShellExecute = true });
            }
            catch { }
        }
    }

    [RelayCommand]
    public void DeleteShader(ShaderPackRow shader)
    {
        if (shader == null) return;
        var instancePath = Current?.Path;
        if (!string.IsNullOrEmpty(instancePath))
        {
            var filePath = Path.Combine(instancePath, "shaderpacks", shader.FileName);
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Không thể xóa shader: {ex.Message}";
                    _notificationService?.ShowError("Không thể xóa shader", ex.Message);
                    return;
                }
            }
        }

        Shaders.Remove(shader);
        if (SelectedShaderForInspector == shader)
        {
            CloseShaderInspector();
        }
        FilterShaders();
        StatusMessage = $"Đã xóa shader {shader.Name}.";
        _notificationService?.ShowSuccess($"Đã xóa shader {shader.Name}", "Shader đã bị xóa khỏi thư mục.");
    }

    public async Task ImportShaderFilesAsync(System.Collections.Generic.IEnumerable<string> filePaths)
    {
        await Task.Yield();
        var instancePath = Current?.Path;
        if (string.IsNullOrEmpty(instancePath)) return;

        var shDir = Path.Combine(instancePath, "shaderpacks");
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
                    using var archive = ZipFile.OpenRead(fp);
                    bool hasShaders = archive.Entries.Any(e => e.FullName.StartsWith("shaders/", StringComparison.OrdinalIgnoreCase) ||
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
                    Version = "1.0.0",
                    Size = "8 MB",
                    AccentColor = "#3b82f6"
                };
                Shaders.Add(new ShaderPackRow(newItem));
                count++;
            }
        }

        if (count > 0)
        {
            FilterShaders();
            StatusMessage = $"Đã thêm {count} shader pack vào thư viện.";
        }
    }

    [RelayCommand]
    public async Task OpenIrisDownloadPageAsync()
    {
        try
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://modrinth.com/mod/iris"));
        }
        catch { }
    }

    // --- Thumbnail Background Loading ---
    private void LoadThumbnailInBackground(ResourcePackRow row, string zipPath)
    {
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

    // --- FileSystemWatchers for Hot-Reloading Folders ---
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
                _resourcePacksWatcher.Dispose();
                _resourcePacksWatcher = null;
            }

            if (_shaderPacksWatcher != null)
            {
                _shaderPacksWatcher.EnableRaisingEvents = false;
                _shaderPacksWatcher.Dispose();
                _shaderPacksWatcher = null;
            }
        }
    }
}
