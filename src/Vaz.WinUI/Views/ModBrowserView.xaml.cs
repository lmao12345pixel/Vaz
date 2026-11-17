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

    public bool HasChanges => ViewModel?.HasChanges ?? false;

    public event EventHandler? CloseRequested;

    private ScrollViewer? _listScrollViewer;
    private SearchHitRow? _pendingRowForInstall;

    public ModBrowserView()
    {
        ViewModel = ((App)Application.Current).Services
            .GetRequiredService<ModBrowserViewModel>();
        InitializeComponent();

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        ViewModel.ItemsTrimmedFromHead += ViewModel_ItemsTrimmedFromHead;
        ViewModel.ItemsPrependedAtHead += ViewModel_ItemsPrependedAtHead;
        ((INotifyCollectionChanged)ViewModel.FilterChips).CollectionChanged += FilterChips_CollectionChanged;

        Unloaded += (_, _) =>
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            ViewModel.ItemsTrimmedFromHead -= ViewModel_ItemsTrimmedFromHead;
            ViewModel.ItemsPrependedAtHead -= ViewModel_ItemsPrependedAtHead;
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

    public async Task InitializeForInstanceAsync(string? instanceId, string projectType = "mod")
    {
        if (ViewModel != null)
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

    public bool HandleEscapeKey()
    {
        if (DependencyPromptOverlay.Visibility == Visibility.Visible)
        {
            DependencyPromptOverlay.Visibility = Visibility.Collapsed;
            _pendingRowForInstall = null;
            return true;
        }
        return false;
    }

    private void UserControl_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            if (HandleEscapeKey())
            {
                e.Handled = true;
                return;
            }

            CloseRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private void SearchBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            if (HandleEscapeKey())
            {
                e.Handled = true;
                return;
            }

            CloseRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private void UserControl_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            if (HandleEscapeKey())
            {
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
        EnsureScrollViewer();

        if (args.InRecycleQueue)
        {
            if (args.ItemContainer?.ContentTemplateRoot is ModCard card)
            {
                card.Recycle();
            }
            return;
        }

        if (args.ItemContainer?.ContentTemplateRoot is ModCard modCard)
        {
            if (args.Phase == 0)
            {
                // Phase 0: Animate entrance cho item mới hiển thị và đăng ký phase tiếp theo
                modCard.AnimateEntrance(args.ItemIndex);
                args.RegisterUpdateCallback(ResultsListView_ContainerContentChanging);
            }
            else if (args.Phase == 1)
            {
                // Phase 1: Chuẩn bị layout & metadata, đăng ký phase tải ảnh
                args.RegisterUpdateCallback(ResultsListView_ContainerContentChanging);
            }
            else if (args.Phase == 2)
            {
                // Phase 2: Nạp ảnh bất đồng bộ với DecodePixelWidth=100
                modCard.LoadIconImage();
            }
        }
    }

    private void ResultsListView_Loaded(object sender, RoutedEventArgs e)
    {
        EnsureScrollViewer();
    }

    private void EnsureScrollViewer()
    {
        if (_listScrollViewer == null)
        {
            _listScrollViewer = FindChild<ScrollViewer>(ResultsListView);
            if (_listScrollViewer != null)
            {
                _listScrollViewer.VerticalAnchorRatio = 0.5;
                _listScrollViewer.ViewChanged += ListScrollViewer_ViewChanged;
            }
        }
    }

    private void ListScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_listScrollViewer == null) return;

        // Cuộn xuống gần cuối -> nạp tiếp
        var distanceToEnd = _listScrollViewer.ScrollableHeight - _listScrollViewer.VerticalOffset;
        if (distanceToEnd < 250 && ViewModel.CanLoadMore)
        {
            _ = ViewModel.LoadMoreAsync();
        }

        // Cuộn ngược lên gần đầu -> nạp lại các item đã bị trim ở đầu (Sliding Window)
        if (_listScrollViewer.VerticalOffset < 250 && ViewModel.CanLoadPrevious)
        {
            _ = ViewModel.LoadPreviousAsync();
        }
    }

    private void ViewModel_ItemsTrimmedFromHead(object? sender, int count)
    {
        EnsureScrollViewer();
        if (_listScrollViewer == null) return;
        const double estimatedItemHeight = 82.0;
        var delta = count * estimatedItemHeight;
        var newOffset = Math.Max(0, _listScrollViewer.VerticalOffset - delta);
        _listScrollViewer.ChangeView(null, newOffset, null, disableAnimation: true);
    }

    private void ViewModel_ItemsPrependedAtHead(object? sender, int count)
    {
        EnsureScrollViewer();
        if (_listScrollViewer == null) return;
        const double estimatedItemHeight = 82.0;
        var delta = count * estimatedItemHeight;
        var newOffset = _listScrollViewer.VerticalOffset + delta;
        _listScrollViewer.ChangeView(null, newOffset, null, disableAnimation: true);
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
