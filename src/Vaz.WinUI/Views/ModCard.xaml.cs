using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Vaz.WinUI.Models;
using Vaz.WinUI.ViewModels;

namespace Vaz.WinUI.Views;

public sealed partial class ModCard : UserControl
{
    public static readonly DependencyProperty ItemProperty =
        DependencyProperty.Register(
            nameof(Item),
            typeof(SearchHitRow),
            typeof(ModCard),
            new PropertyMetadata(null, (d, e) => ((ModCard)d).OnItemChanged(e.OldValue as SearchHitRow, e.NewValue as SearchHitRow)));

    public SearchHitRow? Item
    {
        get => (SearchHitRow?)GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    public event EventHandler<SearchHitRow>? InstallClick;

    private SearchHitRow? _observedItem;
    private ModInstallState _lastKnownState = ModInstallState.Idle;

    public ModCard()
    {
        InitializeComponent();
        Unloaded += ModCard_Unloaded;
    }

    private void ModCard_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_observedItem != null)
        {
            _observedItem.PropertyChanged -= Item_PropertyChanged;
            _observedItem = null;
        }
        ModIconImage.Source = null;
        HoverExitStoryboard.Stop();
        HoverEnterStoryboard.Stop();
        EntranceStoryboard.Stop();
        FadeToInstallingStoryboard.Stop();
        FadeToInstalledStoryboard.Stop();
        FadeToIdleStoryboard.Stop();
    }

    private void OnItemChanged(SearchHitRow? oldItem, SearchHitRow? newItem)
    {
        if (oldItem != null)
        {
            oldItem.PropertyChanged -= Item_PropertyChanged;
        }

        _observedItem = newItem;
        ModIconImage.Source = null;
        if (newItem != null)
        {
            newItem.PropertyChanged += Item_PropertyChanged;
            _lastKnownState = newItem.InstallState;
            UpdateState(newItem.InstallState, animate: false);
        }
        else
        {
            _lastKnownState = ModInstallState.Idle;
            UpdateState(ModInstallState.Idle, animate: false);
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
                UpdateState(row.InstallState, animate: true);
            }
        }
    }

    private void UpdateState(ModInstallState state, bool animate)
    {
        if (animate)
        {
            switch (state)
            {
                case ModInstallState.Installing:
                    IdleContainer.IsHitTestVisible = false;
                    InstallingContainer.IsHitTestVisible = false;
                    InstalledContainer.IsHitTestVisible = false;
                    FadeToInstallingStoryboard.Begin();
                    break;
                case ModInstallState.Installed:
                    IdleContainer.IsHitTestVisible = false;
                    InstallingContainer.IsHitTestVisible = false;
                    InstalledContainer.IsHitTestVisible = false;
                    FadeToInstalledStoryboard.Begin();
                    break;
                default:
                    IdleContainer.IsHitTestVisible = true;
                    InstallingContainer.IsHitTestVisible = false;
                    InstalledContainer.IsHitTestVisible = false;
                    FadeToIdleStoryboard.Begin();
                    break;
            }
        }
        else
        {
            FadeToInstallingStoryboard.Stop();
            FadeToInstalledStoryboard.Stop();
            FadeToIdleStoryboard.Stop();

            switch (state)
            {
                case ModInstallState.Installing:
                    IdleContainer.Opacity = 0;
                    IdleContainer.IsHitTestVisible = false;
                    InstallingContainer.Opacity = 1;
                    InstallingContainer.IsHitTestVisible = false;
                    InstalledContainer.Opacity = 0;
                    InstalledContainer.IsHitTestVisible = false;
                    break;
                case ModInstallState.Installed:
                    IdleContainer.Opacity = 0;
                    IdleContainer.IsHitTestVisible = false;
                    InstallingContainer.Opacity = 0;
                    InstallingContainer.IsHitTestVisible = false;
                    InstalledContainer.Opacity = 1;
                    InstalledContainer.IsHitTestVisible = false;
                    break;
                default:
                    IdleContainer.Opacity = 1;
                    IdleContainer.IsHitTestVisible = true;
                    InstallingContainer.Opacity = 0;
                    InstallingContainer.IsHitTestVisible = false;
                    InstalledContainer.Opacity = 0;
                    InstalledContainer.IsHitTestVisible = false;
                    break;
            }
        }
    }

    /// <summary>
    /// Nạp icon ảnh bất đồng bộ trong Phase 2 với DecodePixelWidth=100.
    /// </summary>
    public void LoadIconImage()
    {
        if (Item == null) return;
        if (ModIconImage.Source != null) return;

        if (!string.IsNullOrWhiteSpace(Item.IconUrl) &&
            Uri.TryCreate(Item.IconUrl, UriKind.RelativeOrAbsolute, out var uri))
        {
            var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(uri)
            {
                DecodePixelWidth = 100
            };
            ModIconImage.Source = bitmap;
            ModIconImage.Visibility = Visibility.Visible;
        }
        else
        {
            ModIconImage.Source = Vaz.WinUI.Helpers.UiPlaceholders.Image;
            ModIconImage.Visibility = Visibility.Visible;
        }
    }

    /// <summary>
    /// Giải phóng tài nguyên hình ảnh khi card bị đẩy vào hàng đợi tái chế (RecycleQueue).
    /// </summary>
    public void Recycle()
    {
        ModIconImage.Source = null;
        HoverExitStoryboard.Stop();
        EntranceStoryboard.Stop();
        CardContainer.Opacity = 1;
        CardTranslate.Y = 0;
    }

    /// <summary>
    /// Kích hoạt hiệu ứng Staggered Entrance (làn sóng): trượt nhẹ từ dưới lên (20px -> 0)
    /// kèm fade-in (0 -> 1) trong 250ms với CubicEase, delay 30ms mỗi thẻ.
    /// </summary>
    public void AnimateEntrance(int index)
    {
        var delayMs = Math.Min(index, 15) * 30;
        EntranceTranslateAnim.BeginTime = TimeSpan.FromMilliseconds(delayMs);
        EntranceOpacityAnim.BeginTime = TimeSpan.FromMilliseconds(delayMs);

        CardContainer.Opacity = 0;
        CardTranslate.Y = 20;

        EntranceStoryboard.Stop();
        EntranceStoryboard.Begin();
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

    private void Card_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        CardScale.CenterX = e.NewSize.Width / 2.0;
        CardScale.CenterY = e.NewSize.Height / 2.0;
    }

    private void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (Item != null)
        {
            InstallClick?.Invoke(this, Item);
        }
    }

    private void IconImage_Failed(object sender, ExceptionRoutedEventArgs e)
    {
        if (sender is Image img)
        {
            img.Visibility = Visibility.Collapsed;
        }
    }
}
