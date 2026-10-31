using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Vaz.WinUI.Models;
using Vaz.WinUI.Services;
using Vaz.WinUI.ViewModels;

namespace Vaz.WinUI.Views;

/// <summary>
/// Mod Browser View (UserControl) tích hợp vào InstanceDetailPage.
/// Hỗ trợ Infinite Scroll, Shimmer Skeleton, Blinking Cursor,
/// Staggered Wave Animation và Dynamic Filter Chips Animation.
/// </summary>
public sealed partial class ModBrowserView : UserControl
{
    public ModBrowserViewModel ViewModel { get; }

    public event EventHandler? CloseRequested;

    private ScrollViewer? _listScrollViewer;
    private SearchHitRow? _pendingRowForInstall;

    public ModBrowserView()
    {
        ViewModel = ((App)Application.Current).Services
            .GetRequiredService<ModBrowserViewModel>();
        InitializeComponent();

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        ((INotifyCollectionChanged)ViewModel.FilterChips).CollectionChanged += FilterChips_CollectionChanged;

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
            FilterChipsEntranceStoryboard.Stop();
        };
    }

    public async Task InitializeForInstanceAsync(string? instanceId)
    {
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
    }

    public void FocusSearchBox()
    {
        SearchBox.Focus(FocusState.Programmatic);
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void UserControl_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            if (DependencyPromptOverlay.Visibility == Visibility.Visible)
            {
                DependencyPromptOverlay.Visibility = Visibility.Collapsed;
                _pendingRowForInstall = null;
                e.Handled = true;
                return;
            }

            CloseRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
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

    private void ResultsListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (!args.InRecycleQueue && args.ItemContainer?.ContentTemplateRoot is ModCard card)
        {
            card.AnimateEntrance(args.ItemIndex);
        }
    }

    private void ResultsListView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_listScrollViewer == null)
        {
            _listScrollViewer = FindChild<ScrollViewer>(ResultsListView);
            if (_listScrollViewer != null)
            {
                _listScrollViewer.ViewChanged += ListScrollViewer_ViewChanged;
            }
        }
    }

    private void ListScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_listScrollViewer == null) return;
        var distanceToEnd = _listScrollViewer.ScrollableHeight - _listScrollViewer.VerticalOffset;
        if (distanceToEnd < 180 && ViewModel.CanLoadMore)
        {
            _ = ViewModel.LoadMoreAsync();
        }
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

    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild) return typedChild;
            var subChild = FindChild<T>(child);
            if (subChild != null) return subChild;
        }
        return null;
    }
}
