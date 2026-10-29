using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Vaz.WinUI.Controls;

/// <summary>
/// Panel wrap theo chiều ngang: đủ chỗ thì nằm một hàng, hẹp thì tự xuống hàng
/// (dùng cho hàng chip danh mục ở cửa sổ ~800px — SET-AC-11, không cắt nội dung).
/// </summary>
public sealed partial class WrapPanel : Panel
{
    public static readonly DependencyProperty HorizontalSpacingProperty =
        DependencyProperty.Register(nameof(HorizontalSpacing), typeof(double), typeof(WrapPanel), new PropertyMetadata(0.0, OnSpacingChanged));

    public static readonly DependencyProperty VerticalSpacingProperty =
        DependencyProperty.Register(nameof(VerticalSpacing), typeof(double), typeof(WrapPanel), new PropertyMetadata(0.0, OnSpacingChanged));

    public double HorizontalSpacing
    {
        get => (double)GetValue(HorizontalSpacingProperty);
        set => SetValue(HorizontalSpacingProperty, value);
    }

    public double VerticalSpacing
    {
        get => (double)GetValue(VerticalSpacingProperty);
        set => SetValue(VerticalSpacingProperty, value);
    }

    private static void OnSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WrapPanel panel)
        {
            panel.InvalidateMeasure();
            panel.InvalidateArrange();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var maxWidth = double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : availableSize.Width;
        var hSpacing = HorizontalSpacing;
        var vSpacing = VerticalSpacing;

        double lineWidth = 0;
        double lineHeight = 0;
        double totalHeight = 0;
        double widestLine = 0;
        bool isFirstInLine = true;

        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var size = child.DesiredSize;

            double itemWidth = isFirstInLine ? size.Width : size.Width + hSpacing;

            if (!isFirstInLine && lineWidth + itemWidth > maxWidth)
            {
                widestLine = Math.Max(widestLine, lineWidth);
                totalHeight += (totalHeight > 0 ? vSpacing : 0) + lineHeight;
                lineWidth = 0;
                lineHeight = 0;
                isFirstInLine = true;
                itemWidth = size.Width;
            }

            lineWidth += itemWidth;
            lineHeight = Math.Max(lineHeight, size.Height);
            isFirstInLine = false;
        }

        widestLine = Math.Max(widestLine, lineWidth);
        if (lineHeight > 0)
        {
            totalHeight += (totalHeight > 0 ? vSpacing : 0) + lineHeight;
        }

        var width = double.IsInfinity(maxWidth) ? widestLine : Math.Min(widestLine, maxWidth);
        return new Size(width, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var hSpacing = HorizontalSpacing;
        var vSpacing = VerticalSpacing;

        double lineWidth = 0;
        double lineHeight = 0;
        double y = 0;
        bool isFirstInLine = true;

        foreach (var child in Children)
        {
            var size = child.DesiredSize;
            double itemWidth = isFirstInLine ? size.Width : size.Width + hSpacing;

            if (!isFirstInLine && lineWidth + itemWidth > finalSize.Width)
            {
                lineWidth = 0;
                y += (y > 0 ? vSpacing : 0) + lineHeight;
                lineHeight = 0;
                isFirstInLine = true;
                itemWidth = size.Width;
            }

            double x = isFirstInLine ? lineWidth : lineWidth + hSpacing;
            child.Arrange(new Rect(x, y, size.Width, size.Height));
            lineWidth = x + size.Width;
            lineHeight = Math.Max(lineHeight, size.Height);
            isFirstInLine = false;
        }

        return new Size(finalSize.Width, y + lineHeight);
    }
}
