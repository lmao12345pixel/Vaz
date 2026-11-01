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

using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

namespace Vaz.WinUI.ViewModels;

/// <summary>
/// ObservableCollection hỗ trợ ISupportIncrementalLoading cho Mod Browser.
/// Tự động gọi delegate nạp thêm dữ liệu bất đồng bộ khi WinUI cuộn tới cuối danh sách.
/// </summary>
public sealed class ModSearchCollection : ObservableCollection<SearchHitRow>, ISupportIncrementalLoading
{
    private readonly Func<uint, Task<uint>> _loadMoreFunc;
    private readonly Func<bool> _hasMoreFunc;

    public ModSearchCollection(Func<uint, Task<uint>> loadMoreFunc, Func<bool> hasMoreFunc)
    {
        _loadMoreFunc = loadMoreFunc ?? throw new ArgumentNullException(nameof(loadMoreFunc));
        _hasMoreFunc = hasMoreFunc ?? throw new ArgumentNullException(nameof(hasMoreFunc));
    }

    public bool HasMoreItems => _hasMoreFunc();

    public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
    {
        return AsyncInfo.Run(async ct =>
        {
            // Throttle rapid scroll requests to prevent stutter
            await Task.Delay(80, ct);
            var loaded = await _loadMoreFunc(count);
            return new LoadMoreItemsResult { Count = loaded };
        });
    }

    public void AddRange(IEnumerable<SearchHitRow> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        foreach (var item in items)
        {
            Add(item);
        }
    }

    public void InsertRange(int index, IEnumerable<SearchHitRow> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        int current = index;
        foreach (var item in items)
        {
            Insert(current++, item);
        }
    }

    public void RemoveRange(int index, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (index < Count)
            {
                RemoveAt(index);
            }
        }
    }
}

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

    public string? FeaturedGallery => Hit.FeaturedGallery;
    public string[]? Gallery => Hit.Gallery;

    public string? CoverUrl
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Hit.FeaturedGallery))
                return Hit.FeaturedGallery;
            if (Hit.Gallery is { Length: > 0 } && !string.IsNullOrWhiteSpace(Hit.Gallery[0]))
                return Hit.Gallery[0];
            return Hit.IconUrl;
        }
    }

    private ImageSource? _coverImageSource;
    public ImageSource CoverImageSource
    {
        get
        {
            if (_coverImageSource != null) return _coverImageSource;

            var url = CoverUrl;
            if (!string.IsNullOrWhiteSpace(url) &&
                Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
            {
                var bitmap = new BitmapImage(uri)
                {
                    DecodePixelWidth = 400
                };
                _coverImageSource = bitmap;
                return _coverImageSource;
            }

            _coverImageSource = Vaz.WinUI.Helpers.UiPlaceholders.Image;
            return _coverImageSource;
        }
    }

    public string OverlayTag
    {
        get
        {
            if (Hit.Follows > 500 || Hit.Downloads > 500_000) return "Featured";
            if (Hit.Downloads > 50_000) return "Popular";
            return string.Empty;
        }
    }

    public bool HasOverlayTag => !string.IsNullOrEmpty(OverlayTag);
    public Visibility OverlayTagVisibility => HasOverlayTag ? Visibility.Visible : Visibility.Collapsed;

    public IReadOnlyList<string> BadgeTags
    {
        get
        {
            var list = new List<string>();
            if (Hit.Categories != null)
            {
                foreach (var c in Hit.Categories.Take(2))
                {
                    list.Add(c);
                }
            }
            if (Hit.Versions != null && Hit.Versions.Length > 0)
            {
                list.Add(Hit.Versions[0]);
            }
            return list;
        }
    }

    public string BadgeText => BadgeTags.Count > 0 ? string.Join("  •  ", BadgeTags) : string.Empty;

    public string AuthorText => string.IsNullOrWhiteSpace(Author) ? string.Empty : $"by {Author}";

    public string FollowsText => FormatCount(Hit.Follows);

    public string UpdatedAtText
    {
        get
        {
            if (Hit.DateModified == null) return string.Empty;
            var diff = DateTimeOffset.UtcNow - Hit.DateModified.Value;
            if (diff.TotalMinutes < 1) return "just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 30) return $"{(int)diff.TotalDays}d ago";
            if (diff.TotalDays < 365) return $"{(int)(diff.TotalDays / 30)}mo ago";
            return $"{(int)(diff.TotalDays / 365)}y ago";
        }
    }

    public IReadOnlyList<string> DisplayTags
    {
        get
        {
            var raw = new List<string>();
            if (Hit.Categories != null)
            {
                foreach (var c in Hit.Categories)
                {
                    if (!string.IsNullOrWhiteSpace(c) && !raw.Contains(c, StringComparer.OrdinalIgnoreCase))
                        raw.Add(c);
                }
            }
            if (Hit.Versions != null && Hit.Versions.Length > 0)
            {
                var v = Hit.Versions[0];
                if (!string.IsNullOrWhiteSpace(v) && !raw.Contains(v, StringComparer.OrdinalIgnoreCase))
                    raw.Add(v);
            }

            const int maxTags = 3;
            if (raw.Count <= maxTags) return raw;

            var result = raw.Take(maxTags).ToList();
            result.Add($"+{raw.Count - maxTags}");
            return result;
        }
    }

    public static string FormatCount(long n) => n switch
    {
        >= 1_000_000 => $"{n / 1_000_000.0:0.##}M",
        >= 1_000 => $"{n / 1_000.0:0.#}K",
        _ => n.ToString("N0")
    };
}

/// <summary>
/// ViewModel cho Mod Browser: chịu trách nhiệm UI binding, gọi Service tách biệt
/// (IModrinthSearchService và IModDownloadManager), hỗ trợ Shimmer, Dynamic Chips và Infinite Scroll.
/// </summary>
public partial class ModBrowserViewModel : ObservableObject
{
    protected readonly IModrinthSearchService _searchService;
    protected readonly IModDownloadManager _downloadManager;
    protected readonly ActiveInstanceService _activeInstance;
    protected readonly IModInstallService? _legacyModInstall;

    private CancellationTokenSource? _searchCts;
    private int _headOffset;
    private int _tailOffset;
    private int _totalHits;
    private int _currentRequestId;
    private int _debounceMs = 200; // 200ms debounce, fallback 250ms nếu gặp 429

    public int TotalHits => _totalHits;

    public const int MaxItems = 100;
    private const int PageSize = 20;

    public ModSearchCollection Results { get; }
    public ObservableCollection<string> FilterChips { get; } = new();
    public ObservableCollection<string> SelectedCategories { get; } = new();
    public ObservableCollection<string> SelectedLoaders { get; } = new();

    public event EventHandler<int>? ItemsTrimmedFromHead;
    public event EventHandler<int>? ItemsPrependedAtHead;

    public string[] SortOptions { get; } = { "Relevance", "Downloads", "Follows", "Updated", "Newest" };
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
    [ObservableProperty] private bool isLoadingPrevious;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private string statusMessage = string.Empty;

    public string StatusText => StatusMessage;
    [ObservableProperty] private int progressPercent;
    [ObservableProperty] private bool hasResults;
    [ObservableProperty] private bool hasMoreResults;
    [ObservableProperty] private bool canInstallPerformancePack = true;
    [ObservableProperty] private bool hasWarning;
    [ObservableProperty] private string warningMessage = string.Empty;
    [ObservableProperty] private bool includeDependencies = true;
    [ObservableProperty] private bool hasChanges;
    [ObservableProperty] private string projectType = "mod";
    [ObservableProperty] private string browserTitle = "Khám phá & Cài đặt Mods";
    [ObservableProperty] private string searchPlaceholder = "Tìm kiếm mods (ví dụ: sodium, iris, fabric)...";
    [ObservableProperty] private bool isModProjectType = true;

    public bool IsNotBusy => !IsBusy;
    public bool CanLoadMore => !IsBusy && !IsLoadingMore && HasMoreResults;
    public bool CanLoadPrevious => !IsBusy && !IsLoadingPrevious && _headOffset > 0;
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

        Results = new ModSearchCollection(LoadMoreInternalAsync, () => HasMoreResults);

        _activeInstance.CurrentChanged += (_, _) => _ = RefreshInstanceContextAsync();
        UpdateFilterChips(string.Empty);
    }

    public void SetProjectType(string? type)
    {
        ProjectType = string.IsNullOrWhiteSpace(type) ? "mod" : type.ToLowerInvariant();
        IsModProjectType = ProjectType == "mod";
        if (ProjectType == "resourcepack")
        {
            BrowserTitle = "Khám phá & Tải Resource Packs";
            SearchPlaceholder = "Tìm kiếm resource packs (ví dụ: faithful, bare bones)...";
        }
        else if (ProjectType == "shader")
        {
            BrowserTitle = "Khám phá & Tải Shaders";
            SearchPlaceholder = "Tìm kiếm shaders (ví dụ: bsl, complementary, seus)...";
        }
        else
        {
            BrowserTitle = "Khám phá & Cài đặt Mods";
            SearchPlaceholder = "Tìm kiếm mods (ví dụ: sodium, iris, fabric)...";
        }

        UpdateFilterChips(string.Empty);
    }

    public void UpdateFilterChips(string query)
    {
        FilterChips.Clear();
        if (ProjectType == "resourcepack")
        {
            FilterChips.Add("Tất cả");
            FilterChips.Add("16x");
            FilterChips.Add("32x");
            FilterChips.Add("64x");
            FilterChips.Add("Modern");
            FilterChips.Add("Medieval");
            FilterChips.Add("Realistic");
            FilterChips.Add("Faithful");
        }
        else if (ProjectType == "shader")
        {
            FilterChips.Add("Tất cả");
            FilterChips.Add("Fantasy");
            FilterChips.Add("Realistic");
            FilterChips.Add("Performance");
            FilterChips.Add("Vibrant");
            FilterChips.Add("Atmospheric");
        }
        else
        {
            var suggestions = _searchService.GetSuggestedCategories(query);
            foreach (var c in suggestions)
            {
                FilterChips.Add(c);
            }
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

    /// <summary>
    /// Nạp thêm trang items phía dưới khi cuộn (gọi từ ISupportIncrementalLoading hoặc ScrollViewer).
    /// </summary>
    public async Task<uint> LoadMoreInternalAsync(uint count)
    {
        if (IsBusy || IsLoadingMore || !HasMoreResults) return 0;
        var ct = _searchCts?.Token ?? CancellationToken.None;
        return await SearchCoreAsync(reset: false, ct);
    }

    /// <summary>
    /// Nạp lại trang items phía trên khi người dùng cuộn ngược lên đầu danh sách (Sliding Window).
    /// </summary>
    [RelayCommand]
    public async Task LoadPreviousAsync()
    {
        if (!CanLoadPrevious) return;

        IsLoadingPrevious = true;
        NotifyBusyChanged();

        var requestId = Interlocked.Increment(ref _currentRequestId);
        var ct = _searchCts?.Token ?? CancellationToken.None;

        try
        {
            // Throttle rapid upward scrolling
            await Task.Delay(100, ct);

            int countToFetch = Math.Min(PageSize, _headOffset);
            int fetchOffset = _headOffset - countToFetch;

            var inst = await _activeInstance.GetCurrentAsync(ct);
            ct.ThrowIfCancellationRequested();

            string[]? loaders = ResolveLoaders(inst);
            string[]? gameVersions = ResolveGameVersions(inst);
            string? sort = SelectedSort switch
            {
                "Downloads" => "downloads",
                "Follows" => "follows",
                "Updated" => "updated",
                "Newest" => "newest",
                _ => null
            };
            bool isBrowseMode = string.IsNullOrWhiteSpace(SearchText);

            var filterOptions = new SearchFilterOptions
            {
                Query = SearchText,
                ProjectType = ProjectType,
                Category = SelectedCategory,
                Categories = ResolveCategories(),
                Loaders = loaders,
                GameVersions = gameVersions,
                SortIndex = isBrowseMode ? (sort ?? "downloads") : sort,
                Offset = fetchOffset,
                Limit = countToFetch
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
            if (requestId != _currentRequestId) return;

            var rows = result.Hits.Select(hit => new SearchHitRow
            {
                Hit = hit,
                InstallState = ModInstallState.Idle
            }).ToList();

            ct.ThrowIfCancellationRequested();
            if (requestId != _currentRequestId) return;

            if (rows.Count > 0)
            {
                Results.InsertRange(0, rows);
                _headOffset -= rows.Count;

                // Cắt bớt ở đuôi nếu vượt quá MaxItems
                if (Results.Count > MaxItems)
                {
                    int excess = Results.Count - MaxItems;
                    Results.RemoveRange(Results.Count - excess, excess);
                    _tailOffset -= excess;
                }

                ItemsPrependedAtHead?.Invoke(this, rows.Count);

                HasMoreResults = _tailOffset < _totalHits;
                StatusMessage = $"{Results.Count} / {_totalHits} kết quả (đang xem #{_headOffset + 1} - #{_tailOffset})";

                if (inst != null)
                {
                    _ = CheckInstallStatesInBackgroundAsync(inst.Id, rows, requestId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // user-cancel
        }
        catch (Exception ex)
        {
            if (requestId == _currentRequestId)
            {
                HasError = true;
                ErrorMessage = ModrinthErrorMapper.Map(ex).Message;
            }
        }
        finally
        {
            if (requestId == _currentRequestId)
            {
                IsLoadingPrevious = false;
                NotifyBusyChanged();
            }
        }
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

    private async Task<uint> SearchCoreAsync(bool reset, CancellationToken ct)
    {
        if (!reset && (IsBusy || IsLoadingMore))
        {
            return 0;
        }

        var requestId = Interlocked.Increment(ref _currentRequestId);

        if (reset)
        {
            IsBusy = true;
            IsShimmerLoading = true;
            _headOffset = 0;
            _tailOffset = 0;
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
                "Follows" => "follows",
                "Updated" => "updated",
                "Newest" => "newest",
                _ => null
            };

            bool isBrowseMode = string.IsNullOrWhiteSpace(SearchText);

            var filterOptions = new SearchFilterOptions
            {
                Query = SearchText,
                ProjectType = ProjectType,
                Category = SelectedCategory,
                Categories = ResolveCategories(),
                Loaders = loaders,
                GameVersions = gameVersions,
                SortIndex = isBrowseMode ? (sort ?? "downloads") : sort,
                Offset = reset ? 0 : _tailOffset,
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
                return 0;
            }

            var rows = result.Hits.Select(hit => new SearchHitRow
            {
                Hit = hit,
                InstallState = ModInstallState.Idle
            }).ToList();

            ct.ThrowIfCancellationRequested();
            if (requestId != _currentRequestId)
            {
                return 0;
            }

            if (reset)
            {
                Results.Clear();
                _headOffset = 0;
                _tailOffset = 0;
            }

            Results.AddRange(rows);

            _tailOffset += rows.Count;
            _totalHits = result.TotalHits;

            // Sliding Window: Nếu số lượng vượt quá MaxItems (100), cắt bớt ở đầu
            if (Results.Count > MaxItems)
            {
                int excess = Results.Count - MaxItems;
                Results.RemoveRange(0, excess);
                _headOffset += excess;
                ItemsTrimmedFromHead?.Invoke(this, excess);
            }

            HasResults = Results.Count > 0;
            HasMoreResults = _tailOffset < _totalHits;
            StatusMessage = Results.Count == 0
                ? (isBrowseMode
                    ? (ProjectType == "resourcepack" ? "Không có resource pack nào phù hợp bộ lọc." : ProjectType == "shader" ? "Không có shader nào phù hợp bộ lọc." : "Không có mod nào phù hợp bộ lọc.")
                    : "Không tìm thấy kết quả. Thử từ khóa khác...")
                : $"{Results.Count} / {_totalHits} kết quả (đang xem #{_headOffset + 1} - #{_tailOffset})";

            if (inst != null && rows.Count > 0)
            {
                _ = CheckInstallStatesInBackgroundAsync(inst.Id, rows, requestId);
            }

            return (uint)rows.Count;
        }
        catch (OperationCanceledException)
        {
            return 0;
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
            return 0;
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

    protected virtual string[]? ResolveCategories()
    {
        if (SelectedCategories.Count > 0)
        {
            return SelectedCategories.ToArray();
        }

        return string.IsNullOrEmpty(SelectedCategory) || SelectedCategory == "Tất cả"
            ? null
            : new[] { SelectedCategory };
    }

    protected virtual string[]? ResolveLoaders(MinecraftInstance? inst)
    {
        if (SelectedLoaders.Count > 0)
        {
            return SelectedLoaders.ToArray();
        }

        if (ProjectType != "mod") return null;
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

    protected virtual string[]? ResolveGameVersions(MinecraftInstance? inst)
    {
        if (SelectedGameVersion == "All") return null;
        if (SelectedGameVersion != "Auto") return new[] { SelectedGameVersion };
        return inst == null ? null : new[] { inst.MinecraftVersion };
    }

    private async Task CheckInstallStatesInBackgroundAsync(string instanceId, List<SearchHitRow> rows, int requestId)
    {
        try
        {
            await Task.Yield();
            var checkTasks = rows.Select(async row =>
            {
                if (requestId != _currentRequestId) return;
                var state = await _downloadManager.CheckInstallStateAsync(instanceId, row.ProjectId, row.Slug, ProjectType, CancellationToken.None);
                if (requestId != _currentRequestId) return;
                if (state != ModInstallState.Idle && row.InstallState != state)
                {
                    row.InstallState = state;
                }
            });
            await Task.WhenAll(checkTasks);
        }
        catch
        {
            // Non-critical background verification
        }
    }

    private void NotifyBusyChanged()
    {
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(CanLoadMore));
        OnPropertyChanged(nameof(CanLoadPrevious));
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
            ErrorMessage = "Chưa chọn instance để cài đặt.";
            return;
        }

        // Kiểm tra xem item đã có trên đĩa chưa; nếu đã có, giữ trạng thái Installed và không tải lại
        var alreadyInstalled = await _downloadManager.IsModInstalledOnDiskAsync(inst.Id, row.ProjectId, row.Slug, ProjectType);
        if (alreadyInstalled)
        {
            row.InstallState = ModInstallState.Installed;
            StatusMessage = $"{row.Title} đã được cài đặt.";
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
                inst.Id, row.Slug, includeDependencies: IncludeDependencies, progress: progress, projectType: ProjectType);

            if (result.Success)
            {
                row.InstallState = ModInstallState.Installed;
                HasChanges = true;
                ProgressPercent = 100;
                StatusMessage = $"Đã cài {result.ModName ?? row.Title}";
            }
            else
            {
                // Kiểm tra lại trên đĩa phòng khi đã cài trước đó
                var onDisk = await _downloadManager.IsModInstalledOnDiskAsync(inst.Id, row.ProjectId, row.Slug, ProjectType);
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

            if (result.Success)
            {
                HasChanges = true;
            }

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
