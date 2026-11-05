using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media;
using Vazer.Core.Versions;
using Vaz.WinUI.Helpers;

namespace Vaz.WinUI.Views.Controls;

/// <summary>
/// Card một MC version trong Step 1 wizard: cover (hoặc gradient fallback) + version + chip + date.
/// Hỗ trợ: hover lift (#2), press bounce (#4), selected state (#3) qua code-behind.
/// </summary>
public sealed partial class WizardVersionCard : UserControl
{
    public WizardVersionCard()
    {
        InitializeComponent();
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
        PointerPressed += OnPointerPressed;
        Tapped += OnTapped;
    }

    /// <summary>Version được chọn, null = chưa set.</summary>
    public static readonly DependencyProperty VersionProperty = DependencyProperty.Register(
        nameof(Version), typeof(MinecraftVersionInfo), typeof(WizardVersionCard),
        new PropertyMetadata(null, OnVersionChanged));

    /// <summary>True khi card đang được chọn (hiện check badge + viền accent).</summary>
    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
        nameof(IsSelected), typeof(bool), typeof(WizardVersionCard),
        new PropertyMetadata(false, OnIsSelectedChanged));

    public MinecraftVersionInfo? Version
    {
        get => (MinecraftVersionInfo?)GetValue(VersionProperty);
        set => SetValue(VersionProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>User click/tap card → parent cập nhật selection.</summary>
    public event EventHandler<MinecraftVersionInfo>? CardClicked;

    private static void OnVersionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WizardVersionCard self) return;
        if (e.NewValue is not MinecraftVersionInfo v) return;

        self.VersionText.Text = v.Id;
        self.DateText.Text = DateTimeOffset.TryParse(v.ReleaseTime, out var dt)
            ? dt.ToLocalTime().ToString("dd/MM/yyyy")
            : v.ReleaseTime;
        self.TypeText.Text = v.Type?.ToLowerInvariant() switch
        {
            "release" => "Release",
            "snapshot" => "Snapshot",
            "old_beta" => "Old Beta",
            "old_alpha" => "Old Alpha",
            _ => v.Type ?? "Other"
        };
        self.Cover.VersionId = v.Id;
        self.InstalledBadge.Visibility = v.IsInstalled ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WizardVersionCard self) return;
        var selected = (bool)e.NewValue;

        // #3 Card Selected: check badge bounce (Scale 0 → 1.2 → 1, BackEase 400ms).
        self.CheckBadge.Visibility = selected ? Visibility.Visible : Visibility.Collapsed;
        if (selected)
        {
            UiAnimationHelper.AnimateInstallSuccess(self.CheckBadge);
        }
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        // #2 Card Hover Lift: scale 1.05 + viền accent + 200ms CubicEase.
        => UiAnimationHelper.AnimateCardHover(CardBorder, true, targetScale: 1.05, durationMs: 200);

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        => UiAnimationHelper.AnimateCardHover(CardBorder, false, targetScale: 1.05, durationMs: 200);

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // #4 Card Click: scale 0.95 → 1 bounce (150ms).
        if (AppThemeManager.IsReduceMotion) return;

        CardBorder.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        var scale = EnsureScaleTransform(CardBorder);

        var sb = new Storyboard();
        var s = new Duration(TimeSpan.FromMilliseconds(150));
        var kfX = new DoubleAnimationUsingKeyFrames();
        kfX.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.95, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        kfX.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(150)), Value = 1.0, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        var kfY = new DoubleAnimationUsingKeyFrames();
        kfY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.95, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        kfY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(150)), Value = 1.0, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        Storyboard.SetTarget(kfX, scale);
        Storyboard.SetTargetProperty(kfX, nameof(ScaleTransform.ScaleX));
        Storyboard.SetTarget(kfY, scale);
        Storyboard.SetTargetProperty(kfY, nameof(ScaleTransform.ScaleY));
        sb.Children.Add(kfX);
        sb.Children.Add(kfY);
        sb.Begin();
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (Version != null)
        {
            CardClicked?.Invoke(this, Version);
        }
    }

    private static ScaleTransform EnsureScaleTransform(UIElement element)
    {
        if (element.RenderTransform is ScaleTransform st) return st;
        var scale = new ScaleTransform { ScaleX = 1.0, ScaleY = 1.0 };
        element.RenderTransform = scale;
        return scale;
    }
}
