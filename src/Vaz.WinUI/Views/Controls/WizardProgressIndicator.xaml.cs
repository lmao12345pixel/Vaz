using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Vaz.WinUI.Views.Controls;

/// <summary>
/// Thanh tiến trình 3 dot + connecting lines đặt ở top của wizard.
/// Dot active: phóng to + glow (200ms BackEase); line fill gradient chạy 300ms.
/// </summary>
public sealed partial class WizardProgressIndicator : UserControl
{
    private int _currentStep = 1;

    public WizardProgressIndicator()
    {
        InitializeComponent();
        Unloaded += (_, _) =>
        {
            // Clean glow trackers when unloaded.
        };
    }

    /// <summary>Step hiện tại (1-based).</summary>
    public int CurrentStep => _currentStep;

    public void SetStep(int step)
    {
        if (step < 1) step = 1;
        if (step > 3) step = 3;

        var forward = step >= _currentStep;
        _currentStep = step;
        UpdateIndicator(animate: true, forward);
    }

    public void Reset()
    {
        _currentStep = 1;
        UpdateIndicator(animate: false, forward: true);
    }

    private void UpdateIndicator(bool animate, bool forward)
    {
        if (Dot1 == null) return;

        SetDot(Dot1, Label1, active: _currentStep == 1, visited: _currentStep > 1, animate, forward);
        SetDot(Dot2, Label2, active: _currentStep == 2, visited: _currentStep > 2, animate, forward);
        SetDot(Dot3, Label3, active: _currentStep == 3, visited: _currentStep == 3, animate, forward);

        SetLine(Line1, Fill1, _currentStep >= 2, animate);
        SetLine(Line2, Fill2, _currentStep >= 3, animate);
    }

    private void SetDot(Border dot, TextBlock label, bool active, bool visited, bool animate, bool forward)
    {
        var accent = (Brush)Application.Current.Resources["VazAccentBrush"];
        var subtle = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];
        var stroke = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];

        dot.Background = visited || active ? accent : subtle;
        dot.BorderBrush = visited || active ? accent : stroke;
        label.Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];

        if (!animate)
        {
            dot.Width = active ? 22 : 14;
            dot.Height = active ? 22 : 14;
            return;
        }

        AnimateDot(dot, active);
    }

    private static void AnimateDot(Border dot, bool active)
    {
        var target = active ? 22.0 : 14.0;
        var durationMs = active ? 200 : 160;

        var sb = new Storyboard();
        foreach (var prop in new[] { nameof(Width), nameof(Height) })
        {
            var anim = new DoubleAnimation
            {
                To = target,
                Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = active ? 0.4 : 0.0 }
            };
            Storyboard.SetTarget(anim, dot);
            Storyboard.SetTargetProperty(anim, prop);
            sb.Children.Add(anim);
        }

        if (active)
        {
            // Glow nhẹ: opacity pulse 0.7 -> 1 trong 200ms.
            var glow = new DoubleAnimation
            {
                From = 0.55,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.2 }
            };
            Storyboard.SetTarget(glow, dot);
            Storyboard.SetTargetProperty(glow, nameof(Opacity));
            sb.Children.Add(glow);
        }

        sb.Begin();
    }

    private void SetLine(Border line, Border fill, bool filled, bool animate)
    {
        var stroke = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
        line.Background = stroke;

        if (fill == null) return;

        if (!animate)
        {
            fill.RenderTransformOrigin = new Windows.Foundation.Point(0, 0.5);
            fill.RenderTransform = new ScaleTransform { ScaleX = filled ? 1.0 : 0.0, ScaleY = 1.0 };
            return;
        }

        fill.RenderTransformOrigin = new Windows.Foundation.Point(0, 0.5);
        fill.RenderTransform = new ScaleTransform();

        var scale = (ScaleTransform)fill.RenderTransform;
        var anim = new DoubleAnimation
        {
            To = filled ? 1.0 : 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(300)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTarget(anim, scale);
        Storyboard.SetTargetProperty(anim, nameof(ScaleTransform.ScaleX));
        var sb = new Storyboard();
        sb.Children.Add(anim);
        sb.Begin();
    }
}
