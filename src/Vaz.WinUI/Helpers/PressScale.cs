using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Vaz.WinUI.Helpers;

/// <summary>
/// Attached property để bật/tắt press-scale animation trên Button.
/// Dùng trong XAML: helpers:PressScale.IsEnabled="True"
/// Hoặc global style trong App.xaml để áp cho tất cả buttons.
/// 
/// RAM optimization: dùng ConditionalWeakTable để tránh leak,
/// StoryboardPool để reuse, GPU-tier gating.
/// </summary>
public static class PressScale
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Button, object> _attached = new();

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(PressScale),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Button button) return;

        if ((bool)e.NewValue)
        {
            Attach(button);
        }
        else
        {
            Detach(button);
        }
    }

    private static void Attach(Button button)
    {
        // Tránh attach trùng (ItemsRepeater recycle)
        if (_attached.TryGetValue(button, out _)) return;
        _attached.Add(button, new object());

        button.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);

        button.PointerPressed -= OnPointerPressed;
        button.PointerReleased -= OnPointerReleased;
        button.PointerCaptureLost -= OnPointerReleased;
        button.PointerCanceled -= OnPointerReleased;

        button.PointerPressed += OnPointerPressed;
        button.PointerReleased += OnPointerReleased;
        button.PointerCaptureLost += OnPointerReleased;
        button.PointerCanceled += OnPointerReleased;
    }

    private static void Detach(Button button)
    {
        _attached.Remove(button);
        button.PointerPressed -= OnPointerPressed;
        button.PointerReleased -= OnPointerReleased;
        button.PointerCaptureLost -= OnPointerReleased;
        button.PointerCanceled -= OnPointerReleased;
    }

    private static void OnPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is not Button button || !button.IsLoaded) return;
        if (AppThemeManager.IsReduceMotion) return;

        try
        {
            var scale = UiAnimationHelper.EnsureScaleTransform(button);
            var sb = UiAnimationHelper.RentStoryboard();
            if (sb == null) return;

            var animX = new DoubleAnimation
            {
                To = 0.94,
                Duration = new Duration(System.TimeSpan.FromMilliseconds(80)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(animX, scale);
            Storyboard.SetTargetProperty(animX, nameof(ScaleTransform.ScaleX));

            var animY = new DoubleAnimation
            {
                To = 0.94,
                Duration = new Duration(System.TimeSpan.FromMilliseconds(80)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(animY, scale);
            Storyboard.SetTargetProperty(animY, nameof(ScaleTransform.ScaleY));

            sb.Children.Add(animX);
            sb.Children.Add(animY);

            void OnCompleted(object? s, object? e)
            {
                try
                {
                    sb.Completed -= OnCompleted;
                    UiAnimationHelper.ReturnStoryboard(sb);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PressScale] ReturnStoryboard error: {ex.Message}");
                }
            }

            sb.Completed += OnCompleted;
            sb.Begin();
        }
        catch (System.Runtime.InteropServices.COMException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PressScale] Ignored COMException (0x{ex.HResult:X8}): {ex.Message}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PressScale] Ignored error: {ex.Message}");
        }
    }

    private static void OnPointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is not Button button || !button.IsLoaded) return;
        if (AppThemeManager.IsReduceMotion) return;

        try
        {
            var scale = UiAnimationHelper.EnsureScaleTransform(button);
            var sb = UiAnimationHelper.RentStoryboard();
            if (sb == null) return;

            var animX = new DoubleAnimation
            {
                To = 1.0,
                Duration = new Duration(System.TimeSpan.FromMilliseconds(120)),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };
            Storyboard.SetTarget(animX, scale);
            Storyboard.SetTargetProperty(animX, nameof(ScaleTransform.ScaleX));

            var animY = new DoubleAnimation
            {
                To = 1.0,
                Duration = new Duration(System.TimeSpan.FromMilliseconds(120)),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };
            Storyboard.SetTarget(animY, scale);
            Storyboard.SetTargetProperty(animY, nameof(ScaleTransform.ScaleY));

            sb.Children.Add(animX);
            sb.Children.Add(animY);

            void OnCompleted(object? s, object? e)
            {
                try
                {
                    sb.Completed -= OnCompleted;
                    UiAnimationHelper.ReturnStoryboard(sb);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PressScale] ReturnStoryboard error: {ex.Message}");
                }
            }

            sb.Completed += OnCompleted;
            sb.Begin();
        }
        catch (System.Runtime.InteropServices.COMException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PressScale] Ignored COMException (0x{ex.HResult:X8}): {ex.Message}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PressScale] Ignored error: {ex.Message}");
        }
    }
}
