using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Vaz.WinUI.Helpers;

/// <summary>bool → Visibility (bất kỳ giá trị nào).</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility v && v == Visibility.Visible;
}

/// <summary>bool → Visibility (ngược lại) — thay cho x:Bind !Property.</summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility v && v != Visibility.Visible;
}

/// <summary>int &gt; 0 → bool (InfoBar IsOpen khi có bản mới).</summary>
public sealed class CountToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is int c && c > 0;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

/// <summary>int → chuỗi mô tả "n mod có bản mới".</summary>
public sealed class CountToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is int c && c > 0 ? $"{c} mod có bản mới" : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

/// <summary>bool → Active instance highlight border brush (ngọc lục bảo khi active, stroke mặc định khi không).</summary>
public sealed class BoolToActiveBorderBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush FallbackActiveBrush = new(Windows.UI.Color.FromArgb(255, 0x4A, 0xDE, 0x80));
    private static readonly SolidColorBrush FallbackInactiveBrush = new(Windows.UI.Color.FromArgb(30, 255, 255, 255));

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is true)
        {
            if (Application.Current?.Resources.TryGetValue("MinecraftEmeraldBorderBrush", out var brush) == true && brush is Brush b)
                return b;
            return FallbackActiveBrush;
        }

        if (Application.Current?.Resources.TryGetValue("CardStrokeColorDefaultBrush", out var defaultBrush) == true && defaultBrush is Brush db)
            return db;

        return FallbackInactiveBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

/// <summary>bool → Active instance highlight border thickness (2px khi active, 1px khi không).</summary>
public sealed class BoolToActiveBorderThicknessConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? new Thickness(2) : new Thickness(1);

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

/// <summary>double &lt;-&gt; int cho Slider binding.</summary>
public sealed class DoubleToIntConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is int i ? (double)i : 0.0;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is double d ? (int)Math.Round(d) : 0;
}

/// <summary>
/// Settings category filter: hiển thị section khi category hiện tại là "all"
/// hoặc trùng <c>ConverterParameter</c> (launcher/java/appearance/network/discord/storage/about).
/// </summary>
public sealed class SettingsSectionVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var selected = value as string ?? "all";
        var section = parameter as string;

        var visible = string.IsNullOrEmpty(section)
            || string.Equals(selected, "all", StringComparison.OrdinalIgnoreCase)
            || string.Equals(selected, section, StringComparison.OrdinalIgnoreCase);

        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

/// <summary>
/// Placeholder dùng chung cho binding ImageSource: tránh ràng buộc chuỗi rỗng/null
/// đang làm crash `XamlBindingHelper.ConvertValue` (crash.txt — ImageSource/Uri conversion).
/// </summary>
public static class UiPlaceholders
{
    private const string PlaceholderAppx = "ms-appx:///Assets/blocks/grass_block_gray.png";

    public static Microsoft.UI.Xaml.Media.ImageSource Image
        => new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(PlaceholderAppx));

    public static readonly Uri FallbackLink = new("about:blank", UriKind.Absolute);
}

/// <summary>Mã màu hex (#10B981) → SolidColorBrush cho swatch accent.</summary>
public sealed class AccentHexToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush FallbackBrush = new(Windows.UI.Color.FromArgb(255, 0x10, 0xB9, 0x81));

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var hex = (value as string ?? string.Empty).Trim().TrimStart('#');
        if (hex.Length != 6
            || !byte.TryParse(hex[..2], System.Globalization.NumberStyles.HexNumber, null, out var r)
            || !byte.TryParse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g)
            || !byte.TryParse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
        {
            return FallbackBrush;
        }

        return new SolidColorBrush(Windows.UI.Color.FromArgb(255, r, g, b));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
