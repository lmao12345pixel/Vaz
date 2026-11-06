using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Vaz.WinUI.Helpers;
using Vaz.WinUI.Models;
using Vazer.Core.Abstractions;
using Vazer.Core.Instances;
using Vazer.Core.Loaders;
using Vazer.Core.Versions;

namespace Vaz.WinUI.Views.Controls;

/// <summary>
/// Step 3 wizard "Tạo instance": xác nhận bản cài (preview card lớn + tên) rồi tạo.
/// Hiển thị trạng thái loading qua morph nút → vòng xoay + stage text,
/// và trạng thái success bằng checkmark vẽ dần (500ms) — dialog tự đóng sau 1.5s.
/// </summary>
public sealed partial class WizardStep3Confirm : UserControl
{
    private readonly DispatcherTimer _autoCloseTimer = new() { Interval = TimeSpan.FromMilliseconds(1500) };

    public WizardStep3Confirm()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        NameBox.TextChanged += (_, _) => ValidateCreate();
        NameBox.GotFocus += (_, _) => AnimateLabelFloat(isFloated: true);
        NameBox.LostFocus += (_, _) => AnimateLabelFloat(isFloated: !string.IsNullOrWhiteSpace(NameBox.Text));

        CreateButton.PointerEntered += CreateButton_PointerEntered;
        CreateButton.PointerExited += CreateButton_PointerExited;
        UiAnimationHelper.AttachButtonPressFeedback(CreateButton, targetScale: 0.95, durationMs: 100, useSpringPhysics: false);

        _autoCloseTimer.Tick += OnAutoCloseTimerTick;
    }

    /// <summary>State dùng chung với wizard (dialog giữ instance).</summary>
    public WizardState State { get; set; } = new();

    /// <summary>Breadcrumb "Bước 3/3 · {Loader} {version}".</summary>
    public void SetBreadcrumb(string text) => BreadcrumbText.Text = text;

    /// <summary>Tên instance user nhập trong Step 3.</summary>
    public string InstanceName => NameBox.Text;

    /// <summary>Đang hiển thị preview hay đã vào trạng thái loading.</summary>
    public bool IsCreating { get; private set; }

    /// <summary>User bấm "Quay lại" (chỉ khi chưa bắt đầu cài).</summary>
    public event EventHandler? BackRequested;

    /// <summary>User bấm "Hủy" (chỉ khi chưa bắt đầu cài).</summary>
    public event EventHandler? CancelRequested;

    /// <summary>User bấm "Tạo Instance".</summary>
    public event EventHandler? CreateRequested;

    /// <summary>User bấm nút đóng sau khi tạo xong (dialog tự đóng nếu user không bấm).</summary>
    public event EventHandler? SuccessConfirmed;

    /// <summary>
    /// Đặt tên mặc định auto-generated (ví dụ "Fabric 1.21.1 #1").
    /// Gọi sau khi State đã có loader + version.
    /// </summary>
    public async Task SetDefaultNameAsync(IInstanceCreationService? creationService, string loader, string mcVersion)
    {
        var baseName = LoaderBaseName(loader) + " " + mcVersion;
        var existing = 0;
        try
        {
            var app = Application.Current as App;
            var manager = app?.Services?.GetService<IInstanceManager>();
            if (manager != null)
            {
                var instances = await manager.ListAsync();
                existing = instances.Count(i =>
                    string.Equals(i.LoaderType, loader.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(i.MinecraftVersion, mcVersion, StringComparison.OrdinalIgnoreCase));
            }
        }
        catch
        {
            existing = 0;
        }

        NameBox.Text = $"{baseName} #{existing + 1}";
        AnimateLabelFloat(isFloated: true);
    }

    private static string LoaderBaseName(string loader) => loader.ToLowerInvariant() switch
    {
        "vanilla" => "Vanilla",
        "quilt" => "Quilt",
        "neoforge" => "NeoForge",
        _ => "Fabric"
    };

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdatePreview();
        ValidateCreate();
        SetFooterButtonsEnabled(true);

        // Staggered entrance cho hero + card + footer (#1 chỉ khi first load).
        foreach (UIElement child in ((Grid)Content).Children)
        {
            UiAnimationHelper.RunStaggeredEntrance(child, 0, fromY: 16, durationMs: 250);
        }

        // Animated gradient cho preview card (10s loop, chậm, chỉ khi không reduce-motion).
        if (!AppThemeManager.IsReduceMotion)
        {
            StartGradientLoop();
        }

        // Pulse glow khi đủ điều kiện.
        UpdateCreatePulse();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _autoCloseTimer.Stop();
        StopGradientLoop();
        CreateButton.PointerEntered -= CreateButton_PointerEntered;
        CreateButton.PointerExited -= CreateButton_PointerExited;
    }

    // === Preview content ===

    private void UpdatePreview()
    {
        var loader = State.SelectedLoader ?? "Fabric";
        LoaderTile.Background = loader.ToLowerInvariant() switch
        {
            "vanilla" => (Brush)Application.Current.Resources["VanillaGradient"],
            "quilt" => (Brush)Application.Current.Resources["QuiltGradient"],
            "neoforge" => (Brush)Application.Current.Resources["NeoForgeGradient"],
            _ => (Brush)Application.Current.Resources["FabricGradient"]
        };

        LoaderGlyph.Text = loader.ToLowerInvariant() switch
        {
            "vanilla" => "V",
            "quilt" => "Q",
            "neoforge" => "N",
            _ => "F"
        };

        var mcVersion = State.SelectedVersion ?? "?";
        VersionLine.Text = $"Minecraft {mcVersion}";
        LoaderLine.Text = loader switch
        {
            "Vanilla" => "Vanilla (không mod)",
            "Fabric" => $"Fabric {State.SelectedLoaderVersion ?? ""}".Trim(),
            "Quilt" => $"Quilt {State.SelectedLoaderVersion ?? ""}".Trim(),
            _ => State.SelectedLoaderVersion != null ? $"{loader} {State.SelectedLoaderVersion}" : loader
        };

        BreadcrumbText.Text = $"Bước 3/3 · {LoaderLine.Text}";
    }

    // === Footer / validation ===

    private void ValidateCreate()
    {
        CreateButton.IsEnabled = !IsCreating && !string.IsNullOrWhiteSpace(NameBox.Text);
        UpdateCreatePulse();
    }

    private void SetFooterButtonsEnabled(bool enabled)
    {
        BackButton.IsEnabled = enabled && !IsCreating;
        CancelButton.IsEnabled = enabled && !IsCreating;
    }

    /// <summary>Pulse glow loop khi nút tạo đủ điều kiện (#6).</summary>
    private void UpdateCreatePulse()
    {
        if (AppThemeManager.IsReduceMotion)
        {
            CreateGlow.Opacity = CreateButton.IsEnabled ? 0.9 : 0;
            return;
        }

        if (CreateButton.IsEnabled && !IsCreating)
        {
            StartCreatePulse();
        }
        else
        {
            StopCreatePulse();
            CreateGlow.Opacity = 0;
        }
    }

    // === Animated gradient (10s loop) ===

    private void StartGradientLoop()
    {
        var sb = new Storyboard { RepeatBehavior = RepeatBehavior.Forever, AutoReverse = true };

        var anim = new ColorAnimation
        {
            From = Windows.UI.Color.FromArgb(255, 0x10, 0xB9, 0x81),   // VazAccentBrush base emerald
            To = Windows.UI.Color.FromArgb(255, 0x06, 0xD6, 0xA0),     // teal-mint
            Duration = new Duration(TimeSpan.FromSeconds(5))
        };
        Storyboard.SetTarget(anim, GradientStop1);
        Storyboard.SetTargetProperty(anim, "Color");

        var anim2 = new ColorAnimation
        {
            From = Windows.UI.Color.FromArgb(255, 0x0D, 0x9C, 0x6E),   // pressed emerald
            To = Windows.UI.Color.FromArgb(255, 0x0E, 0x9F, 0x8A),     // jade-teal
            Duration = new Duration(TimeSpan.FromSeconds(5))
        };
        Storyboard.SetTarget(anim2, GradientStop2);
        Storyboard.SetTargetProperty(anim2, "Color");

        sb.Children.Add(anim);
        sb.Children.Add(anim2);
        GradientLoop = sb;
        sb.Begin();
    }

    private Storyboard? GradientLoop { get; set; }

    private void StopGradientLoop()
    {
        GradientLoop?.Stop();
        GradientLoop = null;
    }

    // === Glow pulse (#6) ===

    private void StartCreatePulse()
    {
        var sb = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };
        var anim = new DoubleAnimation
        {
            From = 0.25,
            To = 0.85,
            Duration = new Duration(TimeSpan.FromSeconds(1)),
            AutoReverse = true,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTarget(anim, CreateGlow);
        Storyboard.SetTargetProperty(anim, nameof(Opacity));
        sb.Children.Add(anim);
        CreatePulse = sb;
        sb.Begin();
    }

    private Storyboard? CreatePulse { get; set; }

    private void StopCreatePulse()
    {
        CreatePulse?.Stop();
        CreatePulse = null;
    }

    // === Form interactions ===

    private void AnimateLabelFloat(bool isFloated)
    {
        if (AppThemeManager.IsReduceMotion)
        {
            FloatingLabel.RenderTransform = null;
            FloatingLabel.FontSize = isFloated ? 11 : 15;
            FloatingLabel.Foreground = isFloated
                ? (Brush)Application.Current.Resources["VazAccentBrush"]
                : (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
            return;
        }

        var target = isFloated ? new Thickness(0, -18, 0, 0) : new Thickness(12, 0, 0, 0);
        var fg = isFloated
            ? (Brush)Application.Current.Resources["VazAccentBrush"]
            : (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];

        var sb = new Storyboard();

        // FontSize shrink
        var fontAnim = new DoubleAnimation
        {
            To = isFloated ? 11.0 : 15.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(150)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(fontAnim, FloatingLabel);
        Storyboard.SetTargetProperty(fontAnim, nameof(TextBlock.FontSize));
        sb.Children.Add(fontAnim);

        // Translate lên
        FloatingLabel.RenderTransform = FloatingLabel.RenderTransform ?? new TranslateTransform();
        var translate = (TranslateTransform)FloatingLabel.RenderTransform;
        var translateY = new DoubleAnimation
        {
            To = isFloated ? -14.0 : 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(150)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(translateY, translate);
        Storyboard.SetTargetProperty(translateY, nameof(TranslateTransform.Y));
        sb.Children.Add(translateY);

        sb.Completed += (_, _) => FloatingLabel.Foreground = fg;
        sb.Begin();
    }

    private void CreateButton_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        // Hover glow pulse mạnh hơn bình thường (không loop vô hạn, chỉ khi đang hover).
        if (!AppThemeManager.IsReduceMotion && CreateButton.IsEnabled && !IsCreating)
        {
            StopCreatePulse();
            CreateGlow.Opacity = 0.95;
        }
    }

    private void CreateButton_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        UpdateCreatePulse();
    }

    // === Creation states (loading → success) ===

    /// <summary>Dialog gọi khi bắt đầu tạo instance (đang transition sang trạng thái loading).</summary>
    public void BeginCreating()
    {
        IsCreating = true;
        SetFooterButtonsEnabled(false);
        StopCreatePulse();
        CreateGlow.Opacity = 0;

        // Cross-fade: normal content → loading content (500ms BackEase).
        NormalContent.Opacity = 1;
        LoadingContent.Opacity = 0;
        LoadingContent.Visibility = Visibility.Visible;
        SuccessContent.Visibility = Visibility.Collapsed;

        var sb = new Storyboard();
        sb.Duration = new Duration(TimeSpan.FromMilliseconds(500));
        var fadeOut = new DoubleAnimation { To = 0.0, Duration = new Duration(TimeSpan.FromMilliseconds(350)), EasingFunction = new BackEase { EasingMode = EasingMode.EaseIn, Amplitude = 0.1 } };
        Storyboard.SetTarget(fadeOut, NormalContent);
        Storyboard.SetTargetProperty(fadeOut, nameof(Opacity));

        var fadeIn = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            BeginTime = TimeSpan.FromMilliseconds(120),
            Duration = new Duration(TimeSpan.FromMilliseconds(380)),
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.15 }
        };
        Storyboard.SetTarget(fadeIn, LoadingContent);
        Storyboard.SetTargetProperty(fadeIn, nameof(Opacity));

        sb.Children.Add(fadeOut);
        sb.Children.Add(fadeIn);
        sb.Completed += (_, _) =>
        {
            NormalContent.Visibility = Visibility.Collapsed;
        };
        sb.Begin();

        // Morph nút: thu nhỏ thành hình tròn + thay đổi layout (500ms BackEase).
        AnimateCreateButtonMorph();
    }

    /// <summary>Dialog báo lỗi tạo instance — quay nút về trạng thái bình thường.</summary>
    public void CreationFailed()
    {
        IsCreating = false;

        // Quay về trạng thái ban đầu.
        LoadingContent.Visibility = Visibility.Collapsed;
        SuccessContent.Visibility = Visibility.Collapsed;
        NormalContent.Visibility = Visibility.Visible;
        AnimateCreateButtonRestore();

        SetFooterButtonsEnabled(true);
        ValidateCreate();
    }

    /// <summary>Dialog báo tạo instance thành công — chạy checkmark draw (500ms) rồi báo đóng.</summary>
    public void CreationSucceeded()
    {
        IsCreating = false;

        LoadingContent.Visibility = Visibility.Collapsed;
        NormalContent.Visibility = Visibility.Collapsed;
        SuccessContent.Visibility = Visibility.Visible;
        SuccessContent.Opacity = 0;

        // Vẽ vòng tròn (300ms) + check path (200ms).
        var circleLength = Math.PI * SuccessCircle.Width; // ước lượng circumference
        SuccessCircle.StrokeDashArray = new DoubleCollection { circleLength, circleLength };
        SuccessCircle.StrokeDashOffset = circleLength;
        SuccessCheck.StrokeDashArray = new DoubleCollection { 200, 200 };
        SuccessCheck.StrokeDashOffset = 200;

        var sb = new Storyboard();
        sb.Duration = new Duration(TimeSpan.FromMilliseconds(560));

        var circleAnim = new DoubleAnimation
        {
            From = circleLength,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(300)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(circleAnim, SuccessCircle);
        Storyboard.SetTargetProperty(circleAnim, "(Path.StrokeDashOffset)");
        sb.Children.Add(circleAnim);

        var checkAnim = new DoubleAnimation
        {
            From = 200,
            To = 0,
            BeginTime = TimeSpan.FromMilliseconds(300),
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(checkAnim, SuccessCheck);
        Storyboard.SetTargetProperty(checkAnim, "(Path.StrokeDashOffset)");
        sb.Children.Add(checkAnim);

        var fadeIn = new DoubleAnimation { From = 0, To = 1, Duration = new Duration(TimeSpan.FromMilliseconds(220)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        Storyboard.SetTarget(fadeIn, SuccessContent);
        Storyboard.SetTargetProperty(fadeIn, nameof(Opacity));
        sb.Children.Add(fadeIn);

        sb.Completed += (_, _) =>
        {
            // Đóng dialog sau 1.5s.
            _autoCloseTimer.Start();
        };
        sb.Begin();
    }

    private void OnAutoCloseTimerTick(object? sender, object e)
    {
        _autoCloseTimer.Stop();
        SuccessConfirmed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Cập nhật stage text (đang chạy) — gọi từ dialog với message thật từ VM.</summary>
    public void UpdateProgress(double percent, string stageText)
    {
        if (percent >= 0 && percent <= 100)
        {
            PercentText.Visibility = Visibility.Visible;
            PercentText.Text = $"{Math.Round(percent)}%";
        }
        else
        {
            PercentText.Visibility = Visibility.Collapsed;
        }

        UpdateStageText(stageText);
    }

    /// <summary>Cập nhật stage text (đang chạy) — gọi từ dialog với message thật từ VM.</summary>
    public void UpdateStageText(string stageText)
    {
        // Cross-fade 200ms: text cũ fade out → text mới fade in.
        StageTextOld.Text = StageTextNew.Text;
        StageTextNew.Text = stageText;

        if (AppThemeManager.IsReduceMotion)
        {
            StageTextOld.Opacity = 0;
            StageTextNew.Opacity = 1;
            return;
        }

        var sb = new Storyboard();
        sb.Duration = new Duration(TimeSpan.FromMilliseconds(220));

        var fadeOut = new DoubleAnimation { To = 0, Duration = new Duration(TimeSpan.FromMilliseconds(120)), EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        Storyboard.SetTarget(fadeOut, StageTextOld);
        Storyboard.SetTargetProperty(fadeOut, nameof(Opacity));

        var fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            BeginTime = TimeSpan.FromMilliseconds(100),
            Duration = new Duration(TimeSpan.FromMilliseconds(160)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(fadeIn, StageTextNew);
        Storyboard.SetTargetProperty(fadeIn, nameof(Opacity));

        sb.Children.Add(fadeOut);
        sb.Children.Add(fadeIn);
        sb.Begin();
    }

    private void AnimateCreateButtonMorph()
    {
        // Morph 500ms BackEase: scale + width collapse nhẹ để nút gọn lại thành loading pill.
        if (AppThemeManager.IsReduceMotion) return;

        CreateButton.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        if (CreateButton.RenderTransform is not TransformGroup tg)
        {
            tg = new TransformGroup();
            if (CreateButton.RenderTransform != null) tg.Children.Add(CreateButton.RenderTransform);
            tg.Children.Add(new ScaleTransform { ScaleX = 1, ScaleY = 1 });
            CreateButton.RenderTransform = tg;
        }

        var scale = (ScaleTransform)tg.Children[^1];
        var sb = new Storyboard();
        foreach (var prop in new[] { nameof(ScaleTransform.ScaleX), nameof(ScaleTransform.ScaleY) })
        {
            var anim = new DoubleAnimation
            {
                To = 0.88,
                Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseInOut, Amplitude = 0.35 }
            };
            Storyboard.SetTarget(anim, scale);
            Storyboard.SetTargetProperty(anim, prop);
            sb.Children.Add(anim);
        }
        sb.Begin();
    }

    private void AnimateCreateButtonRestore()
    {
        if (AppThemeManager.IsReduceMotion) return;

        CreateButton.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        if (CreateButton.RenderTransform is TransformGroup tg && tg.Children.Count > 0)
        {
            var scale = tg.Children[^1] as ScaleTransform;
            if (scale != null)
            {
                var sb = new Storyboard();
                foreach (var prop in new[] { nameof(ScaleTransform.ScaleX), nameof(ScaleTransform.ScaleY) })
                {
                    var anim = new DoubleAnimation
                    {
                        To = 1.0,
                        Duration = new Duration(TimeSpan.FromMilliseconds(350)),
                        EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.35 }
                    };
                    Storyboard.SetTarget(anim, scale);
                    Storyboard.SetTargetProperty(anim, prop);
                    sb.Children.Add(anim);
                }
                sb.Begin();
            }
        }
    }

    // === Button click handlers ===

    private void BackButton_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke(this, EventArgs.Empty);
    private void CancelButton_Click(object sender, RoutedEventArgs e) => CancelRequested?.Invoke(this, EventArgs.Empty);

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsCreating) return;
        CreateRequested?.Invoke(this, EventArgs.Empty);
    }
}
