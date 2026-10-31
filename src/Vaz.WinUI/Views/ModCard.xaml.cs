using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Vaz.WinUI.ViewModels;

namespace Vaz.WinUI.Views;

public sealed partial class ModCard : UserControl
{
    public static readonly DependencyProperty ItemProperty =
        DependencyProperty.Register(
            nameof(Item),
            typeof(SearchHitRow),
            typeof(ModCard),
            new PropertyMetadata(null));

    public SearchHitRow? Item
    {
        get => (SearchHitRow?)GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    public event EventHandler<SearchHitRow>? InstallClick;

    private static readonly SolidColorBrush HoverBrush = new(Windows.UI.Color.FromArgb(255, 16, 185, 129)); // #10B981 Emerald
    private Brush? _defaultBorderBrush;

    public ModCard()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            _defaultBorderBrush = CardBorder.BorderBrush;
        };
    }

    private void Card_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        HoverEnterStoryboard.Begin();
        CardBorder.BorderBrush = HoverBrush;
    }

    private void Card_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        HoverExitStoryboard.Begin();
        CardBorder.BorderBrush = _defaultBorderBrush ?? (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
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
