using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Vaz.WinUI.Models;
using Vaz.WinUI.Services;
using Vaz.WinUI.ViewModels;

namespace Vaz.WinUI.Views;

/// <summary>
/// Mod Browser ContentDialog — hỗ trợ Infinite Scroll, Shimmer Skeleton,
/// Blinking Cursor, Staggered Wave Animation và Dynamic Filter Chips Animation.
/// </summary>
public sealed partial class ModBrowserDialog : ContentDialog
{
    public ModBrowserViewModel ViewModel { get; }

    private ScrollViewer? _listScrollViewer;
    private SearchHitRow? _pendingRowForInstall;

    public ModBrowserDialog(string? instanceId = null)
    {
        ViewModel = ((App)Application.Current).Services
            .GetRequiredService<ModBrowserViewModel>();
        InitializeComponent();

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        ((INotifyCollectionChanged)ViewModel.FilterChips).CollectionChanged += FilterChips_CollectionChanged;

        Loaded += async (_, _) =>
        {
            DialogContentEntranceStoryboard.Begin();

            if (!string.IsNullOrEmpty(instanceId))
            {
                var active = ((App)Application.Current).Services
                    .GetRequiredService<ActiveInstanceService>();
                active.SetCurrent(instanceId);
            }
            await ViewModel.RefreshInstanceContextAsync();
            if (ViewModel.Results.Count == 0)
            {
                await ViewModel.SearchAsync();
            }
        };

        Unloaded += (_, _) =>
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            ((INotifyCollectionChanged)ViewModel.FilterChips).CollectionChanged -= FilterChips_CollectionChanged;
            if (_listScrollViewer != null)
            {
                _listScrollViewer.ViewChanged -= ListScrollViewer_ViewChanged;
            }
            ShimmerStoryboard.Stop();
            BlinkCursorStoryboard.Stop();
            DialogContentEntranceStoryboard.Stop();
            FilterChipsEntranceStoryboard.Stop();
        };
    }

    private void FilterChips_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        FilterChipsEntranceStoryboard.Stop();
        FilterChipsEntranceStoryboard.Begin();
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
            ViewModel.OnSearchTextChangedDebounced();
        }
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        ViewModel.SearchText = sender.Text;
        _ = ViewModel.SearchCommand.ExecuteAsync(null);
    }

    private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
    {
        BlinkCursorStoryboard.Begin();
    }

    private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
    {
        BlinkCursorStoryboard.Stop();
        SearchBlinkIndicator.Opacity = 0;
    }

    private void FilterChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            var category = btn.DataContext as string ?? btn.Content as string;
            if (!string.IsNullOrEmpty(category))
            {
                ViewModel.SelectCategory(category);
            }
        }
    }

    private void ResultsListView_Loaded(object sender, RoutedEventArgs e)
    {
        _listScrollViewer = FindScrollViewer(ResultsListView);
        if (_listScrollViewer != null)
        {
            _listScrollViewer.ViewChanged += ListScrollViewer_ViewChanged;
        }
    }

    private void ResultsListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue) return;
        if (args.ItemContainer.ContentTemplateRoot is ModCard card)
        {
            card.AnimateEntrance(args.ItemIndex);
        }
    }

    private void ListScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_listScrollViewer == null) return;

        // Infinite Scroll: Khi cuộn gần chạm đáy (còn dưới 180px), tự động tải thêm trang tiếp theo
        var distanceToEnd = _listScrollViewer.ScrollableHeight - _listScrollViewer.VerticalOffset;
        if (distanceToEnd < 180 && ViewModel.CanLoadMore)
        {
            _ = ViewModel.LoadMoreAsync();
        }
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ScrollViewer sv) return sv;
            var result = FindScrollViewer(child);
            if (result != null) return result;
        }
        return null;
    }

    private async void ModCard_InstallClick(object? sender, SearchHitRow row)
    {
        var reqDeps = ModCatalogData.GetRequiredDependencies(row.Slug);
        if (reqDeps.Count > 0)
        {
            _pendingRowForInstall = row;
            var depNames = string.Join(", ", reqDeps.Select(d => char.ToUpper(d[0]) + d.Substring(1)));
            DependencyPromptText.Text = $"Mod '{row.Title}' yêu cầu các thư viện phụ thuộc: {depNames}.\n\nBạn có muốn tự động tải và cài đặt kèm toàn bộ dependencies này để game hoạt động ổn định?";
            DependencyPromptOverlay.Visibility = Visibility.Visible;
            return;
        }

        await ViewModel.InstallAsync(row);
    }

    private void CancelDependencyPrompt_Click(object sender, RoutedEventArgs e)
    {
        DependencyPromptOverlay.Visibility = Visibility.Collapsed;
        _pendingRowForInstall = null;
    }

    private async void ConfirmDependencyPrompt_Click(object sender, RoutedEventArgs e)
    {
        DependencyPromptOverlay.Visibility = Visibility.Collapsed;
        if (_pendingRowForInstall != null)
        {
            var target = _pendingRowForInstall;
            _pendingRowForInstall = null;
            await ViewModel.InstallAsync(target);
        }
    }
}
