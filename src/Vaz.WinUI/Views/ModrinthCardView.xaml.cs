using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Vaz.WinUI.Helpers;
using Vaz.WinUI.Models;
using Vaz.WinUI.ViewModels;

namespace Vaz.WinUI.Views;

public sealed partial class ModrinthCardView : UserControl
{
    public static readonly DependencyProperty ItemProperty =
        DependencyProperty.Register(
            nameof(Item),
            typeof(SearchHitRow),
            typeof(ModrinthCardView),
            new PropertyMetadata(null, (d, e) => ((ModrinthCardView)d).OnItemChanged(e.OldValue as SearchHitRow, e.NewValue as SearchHitRow)));

    public SearchHitRow? Item
    {
        get => (SearchHitRow?)GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    public event EventHandler<SearchHitRow>? InstallClick;

    private SearchHitRow? _observedItem;
    private ModInstallState _lastKnownState = ModInstallState.Idle;

    public ModrinthCardView()
    {
        InitializeComponent();
        Unloaded += ModrinthCardView_Unloaded;
    }

    private void ModrinthCardView_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_observedItem != null)
        {
            _observedItem.PropertyChanged -= Item_PropertyChanged;
            _observedItem = null;
        }

        InstallingRing.IsActive = false;
        CoverImage.Source = null;
        HoverExitStoryboard.Stop();
        HoverEnterStoryboard.Stop();
    }

    private void OnItemChanged(SearchHitRow? oldItem, SearchHitRow? newItem)
    {
        if (oldItem != null)
        {
            oldItem.PropertyChanged -= Item_PropertyChanged;
        }

        _observedItem = newItem;
        CoverImage.Source = null;

        if (newItem != null)
        {
            newItem.PropertyChanged += Item_PropertyChanged;
            _lastKnownState = newItem.InstallState;
            UpdateState(newItem.InstallState);
        }
        else
        {
            _lastKnownState = ModInstallState.Idle;
            UpdateState(ModInstallState.Idle);
        }

        Bindings.Update();
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is SearchHitRow row && e.PropertyName == nameof(SearchHitRow.InstallState))
        {
            if (row.InstallState != _lastKnownState)
            {
                _lastKnownState = row.InstallState;
                UpdateState(row.InstallState);
            }
        }
    }

    private void UpdateState(ModInstallState state)
    {
        switch (state)
        {
            case ModInstallState.Installing:
                IdleButton.Visibility = Visibility.Collapsed;
                InstallingPanel.Visibility = Visibility.Visible;
                InstallingRing.IsActive = true;
                InstalledBadge.Visibility = Visibility.Collapsed;
                break;
            case ModInstallState.Installed:
                IdleButton.Visibility = Visibility.Collapsed;
                InstallingPanel.Visibility = Visibility.Collapsed;
                InstallingRing.IsActive = false;
                InstalledBadge.Visibility = Visibility.Visible;
                break;
            default:
                IdleButton.Visibility = Visibility.Visible;
                InstallingPanel.Visibility = Visibility.Collapsed;
                InstallingRing.IsActive = false;
                InstalledBadge.Visibility = Visibility.Collapsed;
                break;
        }
    }

    /// <summary>
    /// Nạp ảnh Cover bất đồng bộ trong Phase 2 với DecodePixelWidth=400.
    /// </summary>
    public void LoadCoverImage()
    {
        if (Item == null) return;
        if (CoverImage.Source != null) return;

        string? url = !string.IsNullOrWhiteSpace(Item.CoverUrl) ? Item.CoverUrl : Item.IconUrl;

        if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
        {
            var bitmap = new BitmapImage(uri)
            {
                DecodePixelWidth = 400
            };
            CoverImage.Source = bitmap;
        }
        else
        {
            CoverImage.Source = UiPlaceholders.Image;
        }
    }

    /// <summary>
    /// Giải phóng hình ảnh khi card bị tái chế trong GridView virtualized panel.
    /// </summary>
    public void Recycle()
    {
        CoverImage.Source = null;
        InstallingRing.IsActive = false;
        HoverEnterStoryboard.Stop();
        HoverExitStoryboard.Stop();
        CardScale.ScaleX = 1.0;
        CardScale.ScaleY = 1.0;
        HoverBorder.Opacity = 0.0;
    }

    private void Card_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        HoverExitStoryboard.Stop();
        HoverEnterStoryboard.Begin();
    }

    private void Card_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        HoverEnterStoryboard.Stop();
        HoverExitStoryboard.Begin();
    }

    private void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (Item != null)
        {
            InstallClick?.Invoke(this, Item);
        }
    }

    private void CoverImage_Failed(object sender, ExceptionRoutedEventArgs e)
    {
        CoverImage.Source = UiPlaceholders.Image;
    }
}
