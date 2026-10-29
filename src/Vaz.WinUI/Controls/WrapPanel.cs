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
    protected override Size MeasureOverride(Size availableSize)
    {
        var maxWidth = double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : availableSize.Width;

        double lineWidth = 0;
        double lineHeight = 0;
        double totalHeight = 0;
        double widestLine = 0;

        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var size = child.DesiredSize;

            if (lineWidth > 0 && lineWidth + size.Width > maxWidth)
            {
                widestLine = Math.Max(widestLine, lineWidth);
                totalHeight += lineHeight;
                lineWidth = 0;
                lineHeight = 0;
            }

            lineWidth += size.Width;
            lineHeight = Math.Max(lineHeight, size.Height);
        }

        widestLine = Math.Max(widestLine, lineWidth);
        totalHeight += lineHeight;

        var width = double.IsInfinity(maxWidth) ? widestLine : Math.Min(widestLine, maxWidth);
        return new Size(width, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double lineWidth = 0;
        double lineHeight = 0;
        double y = 0;

        foreach (var child in Children)
        {
            var size = child.DesiredSize;

            if (lineWidth > 0 && lineWidth + size.Width > finalSize.Width)
            {
                lineWidth = 0;
                y += lineHeight;
                lineHeight = 0;
            }

            child.Arrange(new Rect(lineWidth, y, size.Width, size.Height));
            lineWidth += size.Width;
            lineHeight = Math.Max(lineHeight, size.Height);
        }

        return new Size(finalSize.Width, y + lineHeight);
    }
}
