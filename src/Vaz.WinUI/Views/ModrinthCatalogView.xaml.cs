using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Vaz.WinUI.ViewModels;

namespace Vaz.WinUI.Views;

/// <summary>
/// UserControl hiển thị giao diện duyệt Modrinth Catalog cho Resource Packs và Shaders
/// theo phong cách chuẩn Modrinth Web (Dark theme #1a1a1a, Top Bar tìm kiếm, phân trang và lưới ItemsRepeater).
/// </summary>
public sealed partial class ModrinthCatalogView : UserControl
{
    public ModrinthCatalogViewModel ViewModel { get; }

    public bool HasChanges => ViewModel?.HasChanges ?? false;

    public event EventHandler? CloseRequested;

    private ScrollViewer? _scrollViewer;

    public ModrinthCatalogView()
    {
        ViewModel = ((App)Application.Current).Services.GetRequiredService<ModrinthCatalogViewModel>();
        InitializeComponent();

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        ViewModel.ItemsTrimmedFromHead += ViewModel_ItemsTrimmedFromHead;
        ViewModel.ItemsPrependedAtHead += ViewModel_ItemsPrependedAtHead;

        Unloaded += (_, _) =>
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            ViewModel.ItemsTrimmedFromHead -= ViewModel_ItemsTrimmedFromHead;
            ViewModel.ItemsPrependedAtHead -= ViewModel_ItemsPrependedAtHead;
            if (_scrollViewer != null)
            {
                _scrollViewer.ViewChanged -= CatalogScrollViewer_ViewChanged;
            }
            ShimmerStoryboard.Stop();
        };
    }

    public async Task InitializeForInstanceAsync(string? instanceId, string projectType = "resourcepack")
    {
        ViewModel.HasChanges = false;
        bool typeChanged = !string.Equals(ViewModel.ProjectType, projectType, StringComparison.OrdinalIgnoreCase);
        ViewModel.SetProjectType(projectType);
        await ViewModel.RefreshInstanceContextAsync();
        if (typeChanged || ViewModel.Results.Count == 0)
        {
            await ViewModel.SearchAsync();
        }
    }

    public void FocusSearchBox()
    {
        SearchBox.Focus(FocusState.Programmatic);
    }

    public bool HandleEscapeKey()
    {
        return false;
    }

    private void UserControl_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private void UserControl_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private void SearchBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsShimmerLoading))
        {
            if (ViewModel.IsShimmerLoading)
            {
                ShimmerStoryboard.Begin();
            }
            else
            {
                ShimmerStoryboard.Stop();
                FadeInResultsStoryboard.Begin();
            }
        }
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ViewModel.SearchText = sender.Text;
        }
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        _ = ViewModel.SearchAsync();
    }

    private void CatalogScrollViewer_Loaded(object sender, RoutedEventArgs e)
    {
        _scrollViewer = CatalogScrollViewer;
        if (_scrollViewer != null)
        {
            _scrollViewer.VerticalAnchorRatio = 0.5;
            _scrollViewer.ViewChanged += CatalogScrollViewer_ViewChanged;
        }
    }

    private async void CatalogScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_scrollViewer == null) return;

        // Cuộn xuống gần đáy -> Infinite scroll tải thêm
        if (_scrollViewer.VerticalOffset >= _scrollViewer.ScrollableHeight - 300)
        {
            if (ViewModel.CanLoadMore && !ViewModel.IsLoadingMore)
            {
                await ViewModel.LoadMoreAsync();
            }
        }
        // Cuộn lên gần đầu -> Sliding window tải lại phần trên
        else if (_scrollViewer.VerticalOffset < 200)
        {
            if (ViewModel.CanLoadPrevious && !ViewModel.IsLoadingPrevious)
            {
                await ViewModel.LoadPreviousAsync();
            }
        }
    }

    private void ViewModel_ItemsTrimmedFromHead(object? sender, int count)
    {
        if (_scrollViewer == null || count <= 0) return;
        double itemHeightEstimate = 320.0;
        double delta = count * itemHeightEstimate;
        double newOffset = Math.Max(0, _scrollViewer.VerticalOffset - delta);
        _scrollViewer.ChangeView(null, newOffset, null, disableAnimation: true);
    }

    private void ViewModel_ItemsPrependedAtHead(object? sender, int count)
    {
        if (_scrollViewer == null || count <= 0) return;
        double itemHeightEstimate = 320.0;
        double delta = count * itemHeightEstimate;
        double newOffset = _scrollViewer.VerticalOffset + delta;
        _scrollViewer.ChangeView(null, newOffset, null, disableAnimation: true);
    }

    private void CatalogItemsRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        if (args.Element is ModrinthCardView card)
        {
            card.LoadCoverImage();
        }
    }

    private void CatalogItemsRepeater_ElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
    {
        if (args.Element is ModrinthCardView card)
        {
            card.Recycle();
        }
    }

    private async void ModrinthCard_InstallClick(object? sender, SearchHitRow row)
    {
        await ViewModel.InstallAsync(row);
    }
}
