using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Vaz.WinUI.Models;
using Vaz.WinUI.Services;
using Vazer.Core.Abstractions;
using Vazer.Core.Modrinth;
using Vazer.Core.Mods;

namespace Vaz.WinUI.ViewModels;

/// <summary>
/// Row view cho một Modrinth search hit trong ModCard.
/// </summary>
public sealed partial class SearchHitRow : ObservableObject
{
    public SearchHit Hit { get; init; } = null!;

    public string Title => Hit.Title;
    public string Author => string.IsNullOrEmpty(Hit.Author) ? Hit.Slug : Hit.Author;
    public string Description => Hit.Description;
    public string? IconUrl => Hit.IconUrl;
    public string Slug => Hit.Slug;
    public string ProjectId => Hit.ProjectId;
    public long Downloads => Hit.Downloads;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInstall))]
    [NotifyPropertyChangedFor(nameof(IsInstalling))]
    [NotifyPropertyChangedFor(nameof(IsInstalled))]
    [NotifyPropertyChangedFor(nameof(CanInstallVisibility))]
    [NotifyPropertyChangedFor(nameof(IsInstallingVisibility))]
    [NotifyPropertyChangedFor(nameof(IsInstalledVisibility))]
    private ModInstallState installState = ModInstallState.Idle;

    public bool CanInstall => InstallState == ModInstallState.Idle;
    public bool IsInstalling => InstallState == ModInstallState.Installing;
    public bool IsInstalled => InstallState == ModInstallState.Installed;

    public Visibility CanInstallVisibility =>
        CanInstall ? Visibility.Visible : Visibility.Collapsed;

    public Visibility IsInstallingVisibility =>
        IsInstalling ? Visibility.Visible : Visibility.Collapsed;

    public Visibility IsInstalledVisibility =>
        IsInstalled ? Visibility.Visible : Visibility.Collapsed;

    private ImageSource? _iconImageSource;
    public ImageSource IconImageSource
    {
        get
        {
            if (_iconImageSource != null) return _iconImageSource;

            if (!string.IsNullOrWhiteSpace(Hit.IconUrl) &&
                Uri.TryCreate(Hit.IconUrl, UriKind.RelativeOrAbsolute, out var uri))
            {
                var bitmap = new BitmapImage(uri)
                {
                    DecodePixelWidth = 100
                };
                _iconImageSource = bitmap;
                return _iconImageSource;
            }

            _iconImageSource = Vaz.WinUI.Helpers.UiPlaceholders.Image;
            return _iconImageSource;
        }
    }

    public string DownloadsText => FormatCount(Hit.Downloads);

    public static string FormatCount(long n) => n switch
    {
        >= 1_000_000 => $"{n / 1_000_000.0:0.#}M",
        >= 1_000 => $"{n / 1_000.0:0.#}K",
        _ => n.ToString()
    };
}

/// <summary>
/// ViewModel cho Mod Browser: chịu trách nhiệm UI binding, gọi Service tách biệt
/// (IModrinthSearchService và IModDownloadManager), hỗ trợ Shimmer, Dynamic Chips và Infinite Scroll.
/// </summary>
public sealed partial class ModBrowserViewModel : ObservableObject
{
    private readonly IModrinthSearchService _searchService;
    private readonly IModDownloadManager _downloadManager;
    private readonly ActiveInstanceService _activeInstance;
    private readonly IModInstallService? _legacyModInstall;

    private CancellationTokenSource? _searchCts;
    private int _offset;
    private int _currentRequestId;
    private int _debounceMs = 200; // 200ms debounce, fallback 250ms nếu gặp 429

    private const int PageSize = 20;

    public ObservableCollection<SearchHitRow> Results { get; } = new();
    public ObservableCollection<string> FilterChips { get; } = new();

    public string[] SortOptions { get; } = { "Relevance", "Downloads", "Updated", "Newest" };
    public string[] LoaderOptions { get; } = { "Auto", "All", "fabric", "neoforge" };
    public string[] VersionOptions { get; } =
        { "Auto", "All", "1.21.1", "1.21", "1.20.1", "1.20", "1.19.4", "1.19.2", "1.18.2", "1.16.5" };

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedCategory = "Tất cả";
    [ObservableProperty] private string selectedLoader = "Auto";
    [ObservableProperty] private string selectedGameVersion = "Auto";
    [ObservableProperty] private string selectedSort = "Relevance";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isShimmerLoading;
    [ObservableProperty] private bool isLoadingMore;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private int progressPercent;
    [ObservableProperty] private bool hasResults;
    [ObservableProperty] private bool hasMoreResults;
    [ObservableProperty] private bool canInstallPerformancePack = true;
    [ObservableProperty] private bool hasWarning;
    [ObservableProperty] private string warningMessage = string.Empty;
    [ObservableProperty] private bool includeDependencies = true;

    public bool IsNotBusy => !IsBusy;
    public bool CanLoadMore => !IsBusy && !IsLoadingMore && HasMoreResults;
    public bool ShowEmptyState => !IsBusy && !IsShimmerLoading && !HasError && !HasResults;

    partial void OnSearchTextChanged(string value)
    {
        UpdateFilterChips(value);
        OnSearchTextChangedDebounced();
    }

    partial void OnSelectedCategoryChanged(string value) => _ = SearchAsync();
    partial void OnSelectedLoaderChanged(string value) => _ = SearchAsync();
    partial void OnSelectedGameVersionChanged(string value) => _ = SearchAsync();
    partial void OnSelectedSortChanged(string value) => _ = SearchAsync();

    public ModBrowserViewModel(
        IModrinthSearchService searchService,
        IModDownloadManager downloadManager,
        ActiveInstanceService activeInstance,
        IModInstallService? legacyModInstall = null)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _downloadManager = downloadManager ?? throw new ArgumentNullException(nameof(downloadManager));
        _activeInstance = activeInstance ?? throw new ArgumentNullException(nameof(activeInstance));
        _legacyModInstall = legacyModInstall;

        _activeInstance.CurrentChanged += (_, _) => _ = RefreshInstanceContextAsync();
        UpdateFilterChips(string.Empty);
    }

    public void UpdateFilterChips(string query)
    {
        var suggestions = _searchService.GetSuggestedCategories(query);
        FilterChips.Clear();
        foreach (var c in suggestions)
        {
            FilterChips.Add(c);
        }

        if (!FilterChips.Contains(SelectedCategory))
        {
            SelectedCategory = "Tất cả";
        }
    }

    [RelayCommand]
    public void SelectCategory(string category)
    {
        if (SelectedCategory != category)
        {
            SelectedCategory = category;
        }
    }

    /// <summary>Refresh context theo instance active (loader filter + performance pack capability).</summary>
    public async Task RefreshInstanceContextAsync()
    {
        var inst = await _activeInstance.GetCurrentAsync();
        CanInstallPerformancePack = inst != null &&
                                    !string.Equals(inst.LoaderType, "vanilla", StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        await SearchCoreAsync(reset: true, _searchCts.Token);
    }

    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        if (!CanLoadMore) return;
        var ct = _searchCts?.Token ?? CancellationToken.None;
        await SearchCoreAsync(reset: false, ct);
    }

    [RelayCommand]
    public async Task RetryAsync()
    {
        HasError = false;
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        await SearchCoreAsync(reset: true, _searchCts.Token);
    }

    /// <summary>Debounce 200ms (hoặc 250ms nếu gặp 429) — gọi từ SearchBar TextChanged.</summary>
    public void OnSearchTextChangedDebounced()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;
        _ = DebouncedSearchAsync(ct);
    }

    private async Task DebouncedSearchAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(_debounceMs, ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await SearchCoreAsync(reset: true, ct);
    }

    private async Task SearchCoreAsync(bool reset, CancellationToken ct)
    {
        if (!reset && (IsBusy || IsLoadingMore))
        {
            return;
        }

        var requestId = Interlocked.Increment(ref _currentRequestId);

        if (reset)
        {
            IsBusy = true;
            IsShimmerLoading = true;
            _offset = 0;
        }
        else
        {
            IsLoadingMore = true;
        }

        HasError = false;
        NotifyBusyChanged();

        try
        {
            var inst = await _activeInstance.GetCurrentAsync(ct);
            ct.ThrowIfCancellationRequested();

            string[]? loaders = ResolveLoaders(inst);
            string[]? gameVersions = ResolveGameVersions(inst);
            string? sort = SelectedSort switch
            {
                "Downloads" => "downloads",
                "Updated" => "updated",
                "Newest" => "newest",
                _ => null
            };

            bool isBrowseMode = string.IsNullOrWhiteSpace(SearchText);

            var filterOptions = new SearchFilterOptions
            {
                Query = SearchText,
                Category = SelectedCategory,
                Loaders = loaders,
                GameVersions = gameVersions,
                SortIndex = isBrowseMode ? (sort ?? "downloads") : sort,
                Offset = reset ? 0 : _offset,
                Limit = PageSize
            };

            SearchResult result;
            if (isBrowseMode && (string.IsNullOrEmpty(SelectedCategory) || SelectedCategory == "Tất cả"))
            {
                result = await _searchService.GetFeaturedModsAsync(filterOptions, ct);
            }
            else
            {
                result = await _searchService.SearchAsync(filterOptions, ct);
            }

            ct.ThrowIfCancellationRequested();

            if (requestId != _currentRequestId)
            {
                return;
            }

            var rows = new List<SearchHitRow>();
            foreach (var hit in result.Hits)
            {
                var state = ModInstallState.Idle;
                if (inst != null)
                {
                    state = await _downloadManager.CheckInstallStateAsync(inst.Id, hit.ProjectId, hit.Slug, ct);
                }

                rows.Add(new SearchHitRow
                {
                    Hit = hit,
                    InstallState = state
                });
            }

            ct.ThrowIfCancellationRequested();
            if (requestId != _currentRequestId)
            {
                return;
            }

            if (reset)
            {
                Results.Clear();
                HasMoreResults = false;
            }

            foreach (var r in rows)
            {
                Results.Add(r);
            }

            _offset = filterOptions.Offset + result.Hits.Length;
            HasResults = Results.Count > 0;
            HasMoreResults = Results.Count > 0 && Results.Count < result.TotalHits;
            StatusMessage = Results.Count == 0
                ? (isBrowseMode ? "Không có mod nào phù hợp bộ lọc." : "Không tìm thấy kết quả. Thử từ khóa tiếng Anh (ví dụ: sodium, iris, fabric)...")
                : $"{Results.Count} / {result.TotalHits} kết quả";
        }
        catch (OperationCanceledException)
        {
            // user-cancel — bỏ qua
        }
        catch (Exception ex)
        {
            if (requestId == _currentRequestId)
            {
                HasError = true;
                var mapped = ModrinthErrorMapper.Map(ex);
                ErrorMessage = mapped.Message;

                // Nếu gặp lỗi rate limit (429), tăng debounce lên 250ms an toàn
                if (ex.Message.Contains("429") || mapped.Message.Contains("quá nhanh"))
                {
                    _debounceMs = 250;
                }
            }
        }
        finally
        {
            if (requestId == _currentRequestId)
            {
                IsBusy = false;
                IsShimmerLoading = false;
                IsLoadingMore = false;
                NotifyBusyChanged();
            }
        }
    }

    private string[]? ResolveLoaders(MinecraftInstance? inst)
    {
        if (SelectedLoader == "All") return null;
        if (SelectedLoader != "Auto")
        {
            return new[] { SelectedLoader };
        }

        if (inst == null ||
            string.Equals(inst.LoaderType, "vanilla", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new[] { inst.LoaderType.ToLowerInvariant() };
    }

    private string[]? ResolveGameVersions(MinecraftInstance? inst)
    {
        if (SelectedGameVersion == "All") return null;
        if (SelectedGameVersion != "Auto") return new[] { SelectedGameVersion };
        return inst == null ? null : new[] { inst.MinecraftVersion };
    }

    private void NotifyBusyChanged()
    {
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(CanLoadMore));
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    [RelayCommand]
    public async Task InstallAsync(SearchHitRow? row) => await InstallModAsync(row);

    public async Task InstallModAsync(SearchHitRow? row, IProgress<InstallProgress>? externalProgress = null)
    {
        if (row == null) return;

        var inst = await _activeInstance.GetCurrentAsync();
        if (inst == null)
        {
            HasError = true;
            ErrorMessage = "Chưa chọn instance để cài mod.";
            return;
        }

        // Kiểm tra xem mod đã có trên đĩa chưa; nếu đã có, giữ trạng thái Installed và không tải lại
        var alreadyInstalled = await _downloadManager.IsModInstalledOnDiskAsync(inst.Id, row.ProjectId, row.Slug);
        if (alreadyInstalled)
        {
            row.InstallState = ModInstallState.Installed;
            StatusMessage = $"Mod {row.Title} đã được cài đặt.";
            return;
        }

        try
        {
            row.InstallState = ModInstallState.Installing;
            IsBusy = true;
            ProgressPercent = 0;
            StatusMessage = $"Đang tải {row.Title}...";
            HasError = false;
            NotifyBusyChanged();

            var progress = new Progress<InstallProgress>(p =>
            {
                _ = App.Dispatcher.TryEnqueue(() =>
                {
                    ProgressPercent = p.PercentComplete;
                    if (!string.IsNullOrEmpty(p.Status))
                    {
                        StatusMessage = p.TotalBytes > 0
                            ? $"{p.Status}... {p.PercentComplete}%"
                            : $"{p.Status}...";
                    }
                    else
                    {
                        StatusMessage = $"Downloading {row.Title}... {p.PercentComplete}%";
                    }
                    externalProgress?.Report(p);
                });
            });

            var result = await _downloadManager.InstallModAsync(
                inst.Id, row.Slug, includeDependencies: IncludeDependencies, progress: progress);

            if (result.Success)
            {
                row.InstallState = ModInstallState.Installed;
                ProgressPercent = 100;
                StatusMessage = $"Đã cài {result.ModName ?? row.Title}";
            }
            else
            {
                // Kiểm tra lại trên đĩa phòng khi đã cài trước đó
                var onDisk = await _downloadManager.IsModInstalledOnDiskAsync(inst.Id, row.ProjectId, row.Slug);
                row.InstallState = onDisk ? ModInstallState.Installed : ModInstallState.Idle;
                StatusMessage = $"Cài thất bại: {result.ModName ?? row.Title}";
            }

            if (!string.IsNullOrEmpty(result.Warning))
            {
                HasError = true;
                ErrorMessage = result.Warning!;
            }
        }
        catch (OperationCanceledException)
        {
            var onDisk = await _downloadManager.IsModInstalledOnDiskAsync(inst.Id, row.ProjectId, row.Slug);
            row.InstallState = onDisk ? ModInstallState.Installed : ModInstallState.Idle;
            ProgressPercent = 0;
            StatusMessage = "Đã hủy tải.";
        }
        catch (Exception ex)
        {
            var onDisk = await _downloadManager.IsModInstalledOnDiskAsync(inst.Id, row.ProjectId, row.Slug);
            row.InstallState = onDisk ? ModInstallState.Installed : ModInstallState.Idle;
            HasError = true;
            ErrorMessage = ModrinthErrorMapper.Map(ex).Message;
            StatusMessage = "Cài thất bại.";
        }
        finally
        {
            IsBusy = false;
            NotifyBusyChanged();
        }
    }

    [RelayCommand]
    public void CancelInstall()
    {
        _downloadManager.CancelInstall();
    }

    [RelayCommand]
    public async Task InstallPerformancePackAsync()
    {
        HasError = false;
        HasWarning = false;

        var inst = await _activeInstance.GetCurrentAsync();
        if (inst == null)
        {
            SetWarning("Chọn instance trước.");
            return;
        }

        if (string.Equals(inst.LoaderType, "vanilla", StringComparison.OrdinalIgnoreCase))
        {
            SetWarning("Performance Pack yêu cầu Fabric hoặc NeoForge.");
            return;
        }

        if (!await ConfirmPerformancePackAsync(inst.Name))
        {
            return;
        }

        if (_legacyModInstall == null)
        {
            SetWarning("Dịch vụ cài đặt gói tối ưu chưa sẵn sàng.");
            return;
        }

        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            HasError = false;
            StatusMessage = "Đang cài Performance Pack (Sodium + Iris)...";

            var progress = new Progress<InstallProgress>(p =>
            {
                _ = App.Dispatcher.TryEnqueue(() =>
                {
                    ProgressPercent = p.PercentComplete;
                    if (!string.IsNullOrEmpty(p.Status))
                    {
                        StatusMessage = p.Status;
                    }
                });
            });

            var result = await _legacyModInstall.InstallPerformancePackAsync(inst.Id, progress);

            StatusMessage = result.Success
                ? "Đã cài Performance Pack"
                : "Cài Performance Pack thất bại";

            if (!string.IsNullOrEmpty(result.Warning))
            {
                SetWarning(result.Warning!);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = string.Empty;
            HasError = true;
            ErrorMessage = ModrinthErrorMapper.Map(ex).Message;
        }
        finally
        {
            IsBusy = false;
            ProgressPercent = 0;
            NotifyBusyChanged();
        }
    }

    private void SetWarning(string message)
    {
        HasWarning = true;
        WarningMessage = message;
    }

    private static async Task<bool> ConfirmPerformancePackAsync(string instanceName)
    {
        var root = (App.MainWindow?.Content as FrameworkElement)?.XamlRoot;
        if (root == null)
        {
            return true;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = root,
            Title = "Cài Performance Pack?",
            Content = $"Cài Sodium + Iris cho instance \"{instanceName}\"?\n" +
                      "Alternative performance pack thay OptiFine.\n" +
                      "Yêu cầu: Fabric hoặc NeoForge.",
            PrimaryButtonText = "Cài",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Primary
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
}
