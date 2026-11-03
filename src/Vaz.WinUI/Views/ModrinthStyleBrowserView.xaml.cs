using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Vaz.WinUI.Models;
using Vaz.WinUI.Services;
using Vaz.WinUI.ViewModels;
using Windows.System;

namespace Vaz.WinUI.Views;

public sealed partial class ModrinthStyleBrowserView : UserControl
{
    public ModrinthStyleBrowserViewModel ViewModel { get; }

    public event EventHandler? CloseRequested;

    private ScrollViewer? _gridScrollViewer;
    private string? _currentInstanceId;

    public bool HasChanges => ViewModel?.HasChanges ?? false;

    public ModrinthStyleBrowserView()
    {
        ViewModel = ((App)Application.Current).Services.GetRequiredService<ModrinthStyleBrowserViewModel>();
        InitializeComponent();
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        ViewModel.ItemsTrimmedFromHead += ViewModel_ItemsTrimmedFromHead;
        ViewModel.ItemsPrependedAtHead += ViewModel_ItemsPrependedAtHead;
        Loaded += ModrinthStyleBrowserView_Loaded;
        Unloaded += ModrinthStyleBrowserView_Unloaded;
    }

    private void ModrinthStyleBrowserView_Loaded(object sender, RoutedEventArgs e)
    {
        EnsureScrollViewer();
    }

    private void ModrinthStyleBrowserView_Unloaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            ViewModel.ItemsTrimmedFromHead -= ViewModel_ItemsTrimmedFromHead;
            ViewModel.ItemsPrependedAtHead -= ViewModel_ItemsPrependedAtHead;
        }

        if (_gridScrollViewer != null)
        {
            _gridScrollViewer.ViewChanged -= GridScrollViewer_ViewChanged;
            _gridScrollViewer = null;
        }
    }

    public async Task InitializeForInstanceAsync(string? instanceId, string projectType = "resourcepack")
    {
        _currentInstanceId = instanceId;
        if (ViewModel != null)
        {
            ViewModel.HasChanges = false;
            ViewModel.SetProjectTypeDirect(projectType);
            await ViewModel.RefreshInstanceContextAsync();
            await ViewModel.SearchAsync();
        }
    }

    public void FocusSearchBox()
    {
        SearchBox?.Focus(FocusState.Programmatic);
    }

    public bool HandleEscapeKey()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ModrinthStyleBrowserViewModel.IsShimmerLoading))
        {
            if (ViewModel?.IsShimmerLoading == true)
            {
                ShimmerPanel.Visibility = Visibility.Visible;
                ShimmerStoryboard.Begin();
            }
            else
            {
                ShimmerStoryboard.Stop();
                ShimmerPanel.Visibility = Visibility.Collapsed;
                FadeInResultsStoryboard.Begin();
            }
        }
    }

    private void EnsureScrollViewer()
    {
        if (_gridScrollViewer != null) return;

        _gridScrollViewer = FindVisualChild<ScrollViewer>(ResultsGridView);
        if (_gridScrollViewer != null)
        {
            _gridScrollViewer.VerticalAnchorRatio = 0.5;
            _gridScrollViewer.ViewChanged -= GridScrollViewer_ViewChanged;
            _gridScrollViewer.ViewChanged += GridScrollViewer_ViewChanged;
        }
    }

    private void GridScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_gridScrollViewer == null || ViewModel == null) return;

        if (_gridScrollViewer.VerticalOffset < 250 && ViewModel.CanLoadPrevious && !ViewModel.IsLoadingPrevious)
        {
            _ = ViewModel.LoadPreviousAsync();
        }
    }

    private void ViewModel_ItemsTrimmedFromHead(object? sender, int count)
    {
        EnsureScrollViewer();
        if (_gridScrollViewer == null || count <= 0) return;

        const double estimatedRowHeight = 310.0;
        int cols = Math.Max(1, (int)(_gridScrollViewer.ViewportWidth / 290.0));
        int rows = (count + cols - 1) / cols;
        double heightDelta = rows * estimatedRowHeight;
        double newOffset = Math.Max(0, _gridScrollViewer.VerticalOffset - heightDelta);

        _gridScrollViewer.ChangeView(null, newOffset, null, disableAnimation: true);
    }

    private void ViewModel_ItemsPrependedAtHead(object? sender, int count)
    {
        EnsureScrollViewer();
        if (_gridScrollViewer == null || count <= 0) return;

        const double estimatedRowHeight = 310.0;
        int cols = Math.Max(1, (int)(_gridScrollViewer.ViewportWidth / 290.0));
        int rows = (count + cols - 1) / cols;
        double heightDelta = rows * estimatedRowHeight;
        double newOffset = _gridScrollViewer.VerticalOffset + heightDelta;

        _gridScrollViewer.ChangeView(null, newOffset, null, disableAnimation: true);
    }

    private void ResultsGridView_Loaded(object sender, RoutedEventArgs e)
    {
        EnsureScrollViewer();
    }

    private void ResultsGridView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        EnsureScrollViewer();

        if (args.InRecycleQueue)
        {
            if (args.ItemContainer?.ContentTemplateRoot is ModrinthCardView cardRecycle)
            {
                cardRecycle.Recycle();
            }
            return;
        }

        if (args.ItemContainer?.ContentTemplateRoot is not ModrinthCardView card)
        {
            return;
        }

        if (args.Phase == 0)
        {
            args.RegisterUpdateCallback(ResultsGridView_ContainerContentChanging);
        }
        else if (args.Phase == 1)
        {
            args.RegisterUpdateCallback(ResultsGridView_ContainerContentChanging);
        }
        else if (args.Phase == 2)
        {
            card.LoadCoverImage();
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

    private void LoadPrevious_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.CanLoadPrevious == true)
        {
            _ = ViewModel.LoadPreviousAsync();
        }
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput && ViewModel != null)
        {
            ViewModel.SearchText = sender.Text;
        }
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        _ = ViewModel?.SearchAsync();
    }

    private void SearchBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UserControl_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UserControl_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private async void ModrinthCard_InstallClick(object? sender, SearchHitRow item)
    {
        if (ViewModel != null)
        {
            await ViewModel.InstallAsync(item);
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed) return typed;
            var sub = FindVisualChild<T>(child);
            if (sub != null) return sub;
        }
        return null;
    }
}
