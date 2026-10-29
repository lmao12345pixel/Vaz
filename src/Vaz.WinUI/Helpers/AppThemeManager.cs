using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Vazer.Core.Settings;
using Windows.UI;

namespace Vaz.WinUI.Helpers;

/// <summary>
/// Áp theme / accent / reduce-motion cho toàn shell ở runtime (SET-AC-04, SET-AC-05).
///
/// Cách hoạt động:
/// - Brush surface của app (<c>VazShellBackgroundBrush</c>, <c>VazPaneBackgroundBrush</c>,
///   <c>VazDockBackgroundBrush</c>, pane background của NavigationView) là brush instance
///   khai báo trực tiếp trong <c>App.xaml</c>; đổi <see cref="SolidColorBrush.Color"/> là UI
///   cập nhật ngay, không cần reload trang.
/// - Accent control-level: WinUI dựng ToggleSwitch / Slider / NavigationView selection indicator
///   từ các brush <c>AccentFillColorDefaultBrush</c>, <c>AccentFillColorSecondaryBrush</c>,
///   <c>AccentFillColorTertiaryBrush</c> (xem themeresources của WinUI). Ta đổi màu chính các
///   brush đó trong mọi theme dictionary (trừ HighContrast) nên mọi control dẫn xuất đổi theo.
/// - <c>TextControlElevationBorderFocusedBrush</c> là LinearGradientBrush → đổi màu gradient stop.
/// - Focus ring dùng <c>FocusStrokeColorOuterBrush</c>.
///
/// Không thêm key mới vào theme dictionary của WinUI (tránh sai kiểu Brush/Color); chỉ sửa
/// instance đã tồn tại và bỏ qua HighContrast.
/// </summary>
public static class AppThemeManager
{
    private const string DefaultTheme = "dark";
    private const string DefaultAccent = "emerald";

    // Bảng accent lấy đúng ACCENT_PALETTES trong docs/design-preview/app.js;
    // hover/pressed suy ra từ primary (web chỉ đổi --accent + --accent-subtle).
    private static readonly Dictionary<string, AccentColors> Accents = new(StringComparer.OrdinalIgnoreCase)
    {
        ["emerald"] = new("#10B981", "#3FCB99", "#0D9C6E"),
        ["cyan"] = new("#06B6D4", "#3ACBE3", "#0596AE"),
        ["violet"] = new("#8B5CF6", "#A785F8", "#7448CF"),
        ["amber"] = new("#F59E0B", "#F8B43C", "#CC8409"),
        ["rose"] = new("#F43F5E", "#F76F87", "#CC344D")
    };

    private static FrameworkElement? _shellRoot;

    /// <summary>Theme đang áp dụng: dark | light | obsidian.</summary>
    public static string CurrentTheme { get; private set; } = DefaultTheme;

    /// <summary>Accent đang áp dụng: emerald | cyan | violet | amber | rose.</summary>
    public static string CurrentAccent { get; private set; } = DefaultAccent;

    /// <summary>Giảm chuyển động đang bật hay không.</summary>
    public static bool IsReduceMotion { get; private set; }

    /// <summary>Theme cho ContentDialog (dialog không thừa hưởng RequestedTheme của page).</summary>
    public static ElementTheme DialogTheme =>
        string.Equals(CurrentTheme, "light", StringComparison.OrdinalIgnoreCase)
            ? ElementTheme.Light
            : ElementTheme.Dark;

    /// <summary>Nạp giá trị đã lưu trước khi tạo MainWindow (SET-AC-04: áp ngay khi khởi động).</summary>
    public static void Initialize(GlobalSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        CurrentTheme = NormalizeTheme(settings.Theme);
        CurrentAccent = NormalizeAccent(settings.AccentColor);
        IsReduceMotion = settings.ReduceMotion;

        Timeline.AllowDependentAnimations = !settings.ReduceMotion;
    }

    /// <summary>
    /// Gắn root của shell và áp theme/accent ngay — phải gọi trước
    /// <c>ContentFrame.Navigate(...)</c> để trang đầu tiên render đúng theme.
    /// </summary>
    public static void AttachShell(FrameworkElement root)
    {
        _shellRoot = root;
        ApplyShellTheme();
        ApplyAccentToThemeDictionaries(Accents[CurrentAccent]);
    }

    /// <summary>Đổi theme (dark/light/obsidian) và áp ngay lập tức.</summary>
    public static void SetTheme(string theme)
    {
        CurrentTheme = NormalizeTheme(theme);
        ApplyShellTheme();
        ApplyAccentToThemeDictionaries(Accents[CurrentAccent]);

        // ThemeResource bên trong theme dictionary có thể được WinUI đánh giá lại sau khi
        // RequestedTheme đổi; áp lại accent ở tick kế tiếp để màu accent không bị reset.
        App.Dispatcher?.TryEnqueue(() => ApplyAccentToThemeDictionaries(Accents[CurrentAccent]));
    }

    /// <summary>Đổi accent và áp ngay lập tức cho cả Light lẫn Dark dictionary.</summary>
    public static void SetAccent(string accent)
    {
        CurrentAccent = NormalizeAccent(accent);
        var palette = Accents[CurrentAccent];

        SetAppBrushColor("VazAccentBrush", palette.Primary);
        SetAppBrushColor("VazAccentHoverBrush", palette.Hover);
        SetAppBrushColor("VazAccentPressedBrush", palette.Pressed);
        SetAppBrushColor("VazAccentSubtleBrush", WithAlpha(ParseColor(palette.Primary), 38)); // 15% như web

        ApplyAccentToThemeDictionaries(palette);
    }

    /// <summary>Sự kiện khi trạng thái giảm chuyển động thay đổi.</summary>
    public static event Action<bool>? ReduceMotionChanged;

    /// <summary>Bật/tắt dependent animation (SET-AC-05 — best-effort, cần user xác nhận cảm nhận).</summary>
    public static void SetReduceMotion(bool reduceMotion)
    {
        IsReduceMotion = reduceMotion;
        Timeline.AllowDependentAnimations = !reduceMotion;
        ReduceMotionChanged?.Invoke(reduceMotion);
    }

    // === Theme surface ===

    private static void ApplyShellTheme()
    {
        var palette = GetSurfacePalette(CurrentTheme);

        SetAppBrushColor("VazShellBackgroundBrush", palette.Shell);
        SetAppBrushColor("VazPaneBackgroundBrush", palette.Pane);
        SetAppBrushColor("VazDockBackgroundBrush", palette.Dock);
        SetAppBrushColor("NavigationViewDefaultPaneBackground", palette.Pane);
        SetAppBrushColor("NavigationViewExpandedPaneBackground", palette.Pane);

        if (_shellRoot != null)
        {
            _shellRoot.RequestedTheme = string.Equals(CurrentTheme, "light", StringComparison.OrdinalIgnoreCase)
                ? ElementTheme.Light
                : ElementTheme.Dark;
        }
    }

    /// <summary>
    /// Surface palette suy từ docs/design/tokens.json + giá trị nền của web prototype.
    /// Web không có CSS riêng cho "obsidian" (data-theme="obsidian" rơi về palette dark),
    /// nên obsidian = dark sâu hơn để vẫn phân biệt được bằng mắt.
    /// </summary>
    private static SurfacePalette GetSurfacePalette(string theme) =>
        NormalizeTheme(theme) switch
        {
            "light" => new SurfacePalette("#F2F5F8", "#FAFCFD", "#FAFCFD"), // surface.light #f2f5f8
            "obsidian" => new SurfacePalette("#05070C", "#0A0F18", "#0A0F18"),
            _ => new SurfacePalette("#080B11", "#0B0F17", "#0B0F17") // --bg / --nav-bg của web dark
        };

    // === Accent ===

    private static void ApplyAccentToThemeDictionaries(AccentColors palette)
    {
        var primary = ParseColor(palette.Primary);
        var hover = ParseColor(palette.Hover);
        var pressed = ParseColor(palette.Pressed);

        foreach (var (dictionary, themeKey) in EnumerateThemeDictionaries())
        {
            if (string.Equals(themeKey, "HighContrast", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            MutateBrush(dictionary, "AccentFillColorDefaultBrush", primary);
            MutateBrush(dictionary, "AccentFillColorSecondaryBrush", hover);
            MutateBrush(dictionary, "AccentFillColorTertiaryBrush", pressed);
            MutateBrush(dictionary, "AccentFillColorSelectedTextBackgroundBrush", primary);

            MutateBrush(dictionary, "AccentTextFillColorPrimaryBrush", primary);
            MutateBrush(dictionary, "AccentTextFillColorSecondaryBrush", hover);
            MutateBrush(dictionary, "AccentTextFillColorTertiaryBrush", pressed);

            MutateBrush(dictionary, "FocusStrokeColorOuterBrush", primary);
            MutateGradient(dictionary, "TextControlElevationBorderFocusedBrush", primary);
        }
    }

    private static IEnumerable<(ResourceDictionary Dictionary, string? ThemeKey)> EnumerateThemeDictionaries()
        => Enumerate(Application.Current.Resources, null);

    private static IEnumerable<(ResourceDictionary Dictionary, string? ThemeKey)> Enumerate(
        ResourceDictionary root,
        string? themeKey)
    {
        yield return (root, themeKey);

        foreach (var pair in root.ThemeDictionaries.ToList())
        {
            if (pair.Value is ResourceDictionary themed)
            {
                foreach (var item in Enumerate(themed, pair.Key?.ToString()))
                {
                    yield return item;
                }
            }
        }

        foreach (var merged in root.MergedDictionaries)
        {
            foreach (var item in Enumerate(merged, themeKey))
            {
                yield return item;
            }
        }
    }

    private static void MutateBrush(ResourceDictionary dictionary, string key, Color color)
    {
        if (!dictionary.TryGetValue(key, out var value))
        {
            return;
        }

        switch (value)
        {
            case SolidColorBrush brush:
                brush.Color = color;
                break;

            // Một số key là Color thuần; giữ alpha gốc để không phá design của WinUI.
            case Color existing:
                dictionary[key] = Color.FromArgb(existing.A, color.R, color.G, color.B);
                break;
        }
    }

    private static void MutateGradient(ResourceDictionary dictionary, string key, Color color)
    {
        if (dictionary.TryGetValue(key, out var value) && value is LinearGradientBrush gradient)
        {
            foreach (var stop in gradient.GradientStops)
            {
                stop.Color = Color.FromArgb(stop.Color.A, color.R, color.G, color.B);
            }
        }
    }

    // === Helpers ===

    private static void SetAppBrushColor(string key, Color color)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var value) == true
            && value is SolidColorBrush brush)
        {
            brush.Color = color;
        }
    }

    private static void SetAppBrushColor(string key, string hex)
        => SetAppBrushColor(key, ParseColor(hex));

    private static string NormalizeTheme(string? theme) =>
        theme?.Trim().ToLowerInvariant() switch
        {
            "light" => "light",
            "obsidian" => "obsidian",
            _ => DefaultTheme
        };

    private static string NormalizeAccent(string? accent) =>
        accent?.Trim().ToLowerInvariant() is { } key && Accents.ContainsKey(key)
            ? key
            : DefaultAccent;

    private static Color WithAlpha(Color color, byte alpha) =>
        Color.FromArgb(alpha, color.R, color.G, color.B);

    private static string ToHex(Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    /// <summary>Đọc hex dạng <c>#RRGGBB</c> hoặc <c>#AARRGGBB</c>.</summary>
    private static Color ParseColor(string hex)
    {
        var value = hex.TrimStart('#');

        if (value.Length == 8)
        {
            return Color.FromArgb(
                byte.Parse(value[..2], System.Globalization.NumberStyles.HexNumber),
                byte.Parse(value.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(value.Substring(4, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(value.Substring(6, 2), System.Globalization.NumberStyles.HexNumber));
        }

        return Color.FromArgb(
            255,
            byte.Parse(value[..2], System.Globalization.NumberStyles.HexNumber),
            byte.Parse(value.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
            byte.Parse(value.Substring(4, 2), System.Globalization.NumberStyles.HexNumber));
    }

    private sealed record AccentColors(string Primary, string Hover, string Pressed);

    private sealed record SurfacePalette(string Shell, string Pane, string Dock);
}
