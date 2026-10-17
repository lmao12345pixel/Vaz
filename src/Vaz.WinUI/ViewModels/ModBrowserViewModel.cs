using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vaz.WinUI.Services;
using Vazer.Core.Abstractions;
using Vazer.Core.Modrinth;
using Vazer.Core.Mods;

namespace Vaz.WinUI.ViewModels;

/// <summary>Row view cho một Modrinth search hit.</summary>
public sealed class SearchHitRow
{
    public required SearchHit Hit { get; init; }

    public string Title => Hit.Title;
    public string Author => string.IsNullOrEmpty(Hit.Author) ? Hit.Slug : Hit.Author;
    public string Description => Hit.Description;
    public string? IconUrl => Hit.IconUrl;
    public string Slug => Hit.Slug;
    public string ProjectId => Hit.ProjectId;
    public long Downloads => Hit.Downloads;

    public string DownloadsText => FormatCount(Hit.Downloads);

    public static string FormatCount(long n) => n switch
    {
        >= 1_000_000 => $"{n / 1_000_000.0:0.#}M",
        >= 1_000 => $"{n / 1_000.0:0.#}K",
        _ => n.ToString()
    };
}

/// <summary>
/// ViewModel cho Mod Browser page: search Modrinth (debounce 300ms),
/// filter loader/MC version/sort, install, performance pack.
/// </summary>
public sealed partial class ModBrowserViewModel : ObservableObject
{
    private readonly IModrinthClient _modrinth;
    private readonly IModInstallService _modInstall;
    private readonly ActiveInstanceService _activeInstance;
    private CancellationTokenSource? _searchCts;
    private int _offset;

    public ObservableCollection<SearchHitRow> Results { get; } = new();

    public string[] SortOptions { get; } = { "Relevance", "Downloads", "Updated", "Newest" };
    public string[] LoaderOptions { get; } = { "Auto", "All", "fabric", "neoforge" };
    public string[] VersionOptions { get; } =
        { "Auto", "All", "1.21.1", "1.21", "1.20.1", "1.20", "1.19.4", "1.19.2", "1.18.2", "1.16.5" };

    private const int PageSize = 20;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedLoader = "Auto";
    [ObservableProperty] private string selectedGameVersion = "Auto";
    [ObservableProperty] private string selectedSort = "Relevance";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isLoadingMore;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private int progressPercent;
    [ObservableProperty] private bool hasResults;
    [ObservableProperty] private bool canInstallPerformancePack = true;
    [ObservableProperty] private bool hasWarning;
    [ObservableProperty] private string warningMessage = string.Empty;

    public bool IsNotBusy => !IsBusy;
    public bool CanLoadMore => !IsBusy && !IsLoadingMore;
    public bool ShowEmptyState => !IsBusy && !HasError && !HasResults;

    public ModBrowserViewModel(
        IModrinthClient modrinth,
        IModInstallService modInstall,
        ActiveInstanceService activeInstance)
    {
        _modrinth = modrinth;
        _modInstall = modInstall;
        _activeInstance = activeInstance;
        _activeInstance.CurrentChanged += (_, _) => _ = RefreshInstanceContextAsync();
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
        await SearchCoreAsync(reset: true, CancellationToken.None);
    }

    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        await SearchCoreAsync(reset: false, CancellationToken.None);
    }

    [RelayCommand]
    public async Task RetryAsync()
    {
        HasError = false;
        await SearchCoreAsync(reset: true, CancellationToken.None);
    }

    /// <summary>Debounce 300ms — gọi từ SearchBar TextChanged.</summary>
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
            await Task.Delay(300, ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await SearchCoreAsync(reset: true, ct);
    }

    private async Task SearchCoreAsync(bool reset, CancellationToken ct)
    {
        if (IsBusy || (IsLoadingMore && !reset))
        {
            return;
        }

        if (reset)
        {
            IsBusy = true;
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

            string[]? loaders = ResolveLoaders(inst);
            string[]? gameVersions = ResolveGameVersions(inst);
            string? sort = SelectedSort switch
            {
                "Downloads" => "downloads",
                "Updated" => "updated",
                "Newest" => "newest",
                _ => null
            };

            var query = new SearchQuery
            {
                Query = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
                Loaders = loaders,
                GameVersions = gameVersions,
                SortIndex = sort,
                Offset = reset ? 0 : _offset,
                Limit = PageSize
            };

            var result = await _modrinth.SearchAsync(query, ct);

            if (reset)
            {
                Results.Clear();
            }

            foreach (var hit in result.Hits)
            {
                Results.Add(new SearchHitRow { Hit = hit });
            }

            _offset = query.Offset + result.Hits.Length;
            HasResults = Results.Count > 0;
            StatusMessage = Results.Count == 0
                ? "No results"
                : $"{Results.Count} / {result.TotalHits} results";
        }
        catch (OperationCanceledException)
        {
            // user-cancel — bỏ qua
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            IsLoadingMore = false;
            NotifyBusyChanged();
        }
    }

    private string[]? ResolveLoaders(Vazer.Core.Abstractions.MinecraftInstance? inst)
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

    private string[]? ResolveGameVersions(Vazer.Core.Abstractions.MinecraftInstance? inst)
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
    public async Task InstallAsync(SearchHitRow? row)
    {
        if (row == null) return;

        var inst = await _activeInstance.GetCurrentAsync();
        if (inst == null)
        {
            HasError = true;
            ErrorMessage = "Chưa chọn instance — chọn instance ở thanh bên trái trước.";
            return;
        }

        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            HasError = false;
            StatusMessage = $"Installing {row.Title}...";

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

            var result = await _modInstall.InstallFromModrinthAsync(
                inst.Id, row.Slug, versionId: null, resolveDependencies: true, progress: progress);

            StatusMessage = result.Success
                ? $"Đã cài {result.ModName ?? row.Title}"
                : $"Cài thất bại: {result.ModName ?? row.Title}";

            if (!string.IsNullOrEmpty(result.Warning))
            {
                HasError = true;
                ErrorMessage = result.Warning!;
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            ProgressPercent = 0;
            NotifyBusyChanged();
        }
    }

    [RelayCommand]
    public async Task InstallPerformancePackAsync()
    {
        var inst = await _activeInstance.GetCurrentAsync();
        if (inst == null)
        {
            HasError = true;
            ErrorMessage = "Chưa chọn instance.";
            return;
        }

        if (string.Equals(inst.LoaderType, "vanilla", StringComparison.OrdinalIgnoreCase))
        {
            HasError = true;
            ErrorMessage = "Performance Pack yêu cầu Fabric hoặc NeoForge. Instance hiện tại là Vanilla.";
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

            var result = await _modInstall.InstallPerformancePackAsync(inst.Id, progress);

            StatusMessage = result.Success
                ? "Đã cài Performance Pack"
                : "Cài Performance Pack thất bại";

            if (!string.IsNullOrEmpty(result.Warning))
            {
                HasWarning = true;
                WarningMessage = result.Warning!;
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            ProgressPercent = 0;
            NotifyBusyChanged();
        }
    }
}
