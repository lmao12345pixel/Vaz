using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vaz.WinUI.Services;
using Vazer.Core.Abstractions;
using Vazer.Core.Mods;

namespace Vaz.WinUI.ViewModels;

/// <summary>
/// ViewModel cho ModrinthCatalogView: Hỗ trợ giao diện Modrinth Catalog bản web.
/// Quản lý thanh tìm kiếm, bộ lọc Sort by, View (số lượng), phân trang, và tích hợp API.
/// </summary>
public sealed partial class ModrinthCatalogViewModel : ModrinthStyleBrowserViewModel
{
    public string[] ViewOptions { get; } = { "20", "50", "100" };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(PaginationText))]
    private string selectedView = "20";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PaginationText))]
    [NotifyPropertyChangedFor(nameof(CanGoPrev))]
    private int currentPage = 1;

    public int PageSizeOption => int.TryParse(SelectedView, out var s) ? s : 20;

    public int TotalPages
    {
        get
        {
            var total = TotalHits > 0 ? TotalHits : Results.Count;
            return Math.Max(1, (total + PageSizeOption - 1) / PageSizeOption);
        }
    }

    public string PaginationText => $"1, 2, ..., {TotalPages}";

    public bool CanGoPrev => CurrentPage > 1;
    public bool CanGoNext => HasMoreResults;

    public ModrinthCatalogViewModel(
        IModrinthSearchService searchService,
        IModDownloadManager downloadManager,
        ActiveInstanceService activeInstance,
        IModInstallService? legacyModInstall = null)
        : base(searchService, downloadManager, activeInstance, legacyModInstall)
    {
    }

    partial void OnSelectedViewChanged(string value)
    {
        CurrentPage = 1;
        _ = SearchAsync();
    }

    [RelayCommand]
    public async Task NextPageAsync()
    {
        if (!CanLoadMore) return;
        await LoadMoreAsync();
        CurrentPage++;
        OnPropertyChanged(nameof(PaginationText));
        OnPropertyChanged(nameof(CanGoPrev));
        OnPropertyChanged(nameof(CanGoNext));
    }

    [RelayCommand]
    public async Task PrevPageAsync()
    {
        if (!CanLoadPrevious) return;
        await LoadPreviousAsync();
        if (CurrentPage > 1) CurrentPage--;
        OnPropertyChanged(nameof(PaginationText));
        OnPropertyChanged(nameof(CanGoPrev));
        OnPropertyChanged(nameof(CanGoNext));
    }
}
