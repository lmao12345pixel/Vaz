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
using Vaz.WinUI.Services;
using Vazer.Core.Abstractions;
using Vazer.Core.Instances;
using Vazer.Core.Loaders;
using Vazer.Core.Versions;
using Vaz.WinUI.ViewModels;

namespace Vaz.WinUI.Views.Controls;

/// <summary>
/// Step 3 wizard "Tạo instance": xác nhận bản cài (preview card lớn + tên) rồi tạo.
/// Hiển thị trạng thái loading qua morph nút → vòng xoay + stage text,
/// và trạng thái success bằng checkmark vẽ dần (500ms) — dialog tự đóng sau 1.5s.
/// </summary>
public sealed partial class WizardStep3Confirm : UserControl
{
    private readonly DispatcherTimer _autoCloseTimer = new() { Interval = TimeSpan.FromMilliseconds(2500) };
    private TaskCompletionSource<InstanceDetailViewModel.ModChangeOption>? _migrationTcs;

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

    /// <summary>Cập nhật toàn bộ preview card và trạng thái khi bước 3 được kích hoạt.</summary>
    public void PrepareStep3()
    {
        UpdatePreview();
        ValidateCreate();
        AnimateLabelFloat(isFloated: !string.IsNullOrWhiteSpace(NameBox.Text));
    }

    /// <summary>Breadcrumb "Bước 3/3 · {Loader} {version}".</summary>
    public void SetBreadcrumb(string text) => BreadcrumbText.Text = text;

    /// <summary>
    /// Chế độ "Đổi phiên bản": hiện overlay cảnh báo migration (3 lựa chọn: giữ mods / xóa mods / hủy).
    /// Trả về lựa chọn; panel tự ẩn sau khi chọn.
    /// </summary>
    public Task<InstanceDetailViewModel.ModChangeOption> RequestMigrationConfirmationAsync()
    {
        if (_migrationTcs != null)
        {
            return _migrationTcs.Task;
        }

        _migrationTcs = new TaskCompletionSource<InstanceDetailViewModel.ModChangeOption>();
        MigrationWarningPanel.Visibility = Visibility.Visible;

        if (AppThemeManager.IsReduceMotion)
        {
            MigrationWarningPanel.Opacity = 1;
            return _migrationTcs.Task;
        }

        MigrationWarningPanel.Opacity = 0;
        MigrationInnerPanel.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        var translate = new TranslateTransform { Y = 16 };
        MigrationInnerPanel.RenderTransform = translate;

        var duration = new Duration(TimeSpan.FromMilliseconds(220));
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var sb = new Storyboard();

        var fade = new DoubleAnimation { To = 1.0, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(fade, MigrationWarningPanel);
        Storyboard.SetTargetProperty(fade, nameof(UIElement.Opacity));

        var slide = new DoubleAnimation { To = 0.0, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(slide, translate);
        Storyboard.SetTargetProperty(slide, nameof(TranslateTransform.Y));

        sb.Children.Add(fade);
        sb.Children.Add(slide);
        sb.Begin();

        return _migrationTcs.Task;
    }

    private void CompleteMigration(InstanceDetailViewModel.ModChangeOption option)
    {
        MigrationWarningPanel.Visibility = Visibility.Collapsed;
        var tcs = _migrationTcs;
        _migrationTcs = null;
        tcs?.TrySetResult(option);
    }

    private void KeepModsButton_Click(object sender, RoutedEventArgs e)
        => CompleteMigration(InstanceDetailViewModel.ModChangeOption.KeepMods);

    private void DeleteModsButton_Click(object sender, RoutedEventArgs e)
        => CompleteMigration(InstanceDetailViewModel.ModChangeOption.DeleteMods);

    private void CancelMigrationButton_Click(object sender, RoutedEventArgs e)
        => CompleteMigration(InstanceDetailViewModel.ModChangeOption.Cancel);

    /// <summary>Tên instance user nhập trong Step 3.</summary>
    public string InstanceName => NameBox.Text;

    /// <summary>Đặt label nút hành động cuối cùng (Create: "Tạo Instance", change: "Đổi phiên bản").</summary>
    public void SetActionLabel(string text) => ActionLabel.Text = text;

    /// <summary>
    /// Chế độ "Đổi phiên bản": khóa tên instance (không cho sửa) và pin tên hiện tại.
    /// </summary>
    public void SetNameReadOnly(string name)
    {
        NameBox.Text = name;
        NameBox.IsReadOnly = true;
        NameBox.IsEnabled = true; // giữ khả năng focus/scroll vào giữa text nếu cần
        AnimateLabelFloat(isFloated: true);
    }

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
        "forge" => "Forge",
        _ => "Fabric"
    };

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdatePreview();
        ValidateCreate();
        SetFooterButtonsEnabled(true);

        // Staggered entrance (#1) — tắt trên GPU yếu.
        if (GpuTierDetector.Current.IsHeavyEffectsEnabled)
        {
            foreach (UIElement child in ((Grid)Content).Children)
            {
                UiAnimationHelper.RunStaggeredEntrance(child, 0, fromY: 16, durationMs: 250);
            }
        }

        // Animated gradient cho preview card (10s loop) — chỉ khi GPU đủ mạnh.
        if (!AppThemeManager.IsReduceMotion && GpuTierDetector.Current.IsHeavyEffectsEnabled)
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
        LoaderTile.Background = GetLoaderGradient(loader);

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

    private Brush GetLoaderGradient(string loader)
    {
        var key = loader.ToLowerInvariant() switch
        {
            "vanilla" => "VanillaGradient",
            "quilt" => "QuiltGradient",
            "neoforge" => "NeoForgeGradient",
            _ => "FabricGradient"
        };

        if (Resources.TryGetValue(key, out var res) && res is Brush brush)
        {
            return brush;
        }

        if (Application.Current?.Resources.TryGetValue(key, out var appRes) == true && appRes is Brush appBrush)
        {
            return appBrush;
        }

        var accent = Application.Current?.Resources["VazAccentBrush"];
        return accent as Brush
            ?? throw new InvalidOperationException("VazAccentBrush resource is missing.");
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
        if (AppThemeManager.IsReduceMotion || !GpuTierDetector.Current.IsHeavyEffectsEnabled)
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
            From = Windows.UI.Color.FromArgb(255, 0x8B, 0x5A, 0x2B),   // wood brown light
            To = Windows.UI.Color.FromArgb(255, 0xA0, 0x6C, 0x3A),     // wood brown lighter
            Duration = new Duration(TimeSpan.FromSeconds(5))
        };
        Storyboard.SetTarget(anim, GradientStop1);
        Storyboard.SetTargetProperty(anim, "Color");

        var anim2 = new ColorAnimation
        {
            From = Windows.UI.Color.FromArgb(255, 0x6B, 0x44, 0x23),   // wood brown dark
            To = Windows.UI.Color.FromArgb(255, 0x7A, 0x50, 0x2B),     // wood brown mid
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
        var fg = isFloated
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(235, 255, 255, 255))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(180, 255, 255, 255));

        if (AppThemeManager.IsReduceMotion)
        {
            FloatingLabel.RenderTransform = null;
            FloatingLabel.FontSize = isFloated ? 11 : 15;
            FloatingLabel.Foreground = fg;
            return;
        }

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
        if (FloatingLabel.RenderTransform is not TranslateTransform translate)
        {
            translate = new TranslateTransform();
            FloatingLabel.RenderTransform = translate;
        }
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
        CreateButton.IsHitTestVisible = false;
        StopCreatePulse();
        CreateGlow.Opacity = 0;

        NormalContent.Visibility = Visibility.Collapsed;
        SuccessContent.Visibility = Visibility.Collapsed;
        LoadingContent.Visibility = Visibility.Visible;
        LoadingContent.Opacity = 1.0;

        // Morph nút: thu nhỏ thành hình tròn + thay đổi layout (500ms BackEase).
        AnimateCreateButtonMorph();
    }

    /// <summary>Dialog báo lỗi tạo instance — quay nút về trạng thái bình thường.</summary>
    public void CreationFailed()
    {
        IsCreating = false;
        CreateButton.IsHitTestVisible = true;

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
        SuccessContent.Opacity = 1.0;

        // Success animation #10: scale 0 -> 1.2 -> 1 (500ms BackEase)
        if (!AppThemeManager.IsReduceMotion)
        {
            SuccessContent.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
            var scale = new ScaleTransform { ScaleX = 0.0, ScaleY = 0.0 };
            SuccessContent.RenderTransform = scale;

            var sb = new Storyboard();
            var duration = new Duration(TimeSpan.FromMilliseconds(500));
            var ease = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.6 };

            var animX = new DoubleAnimation { From = 0.0, To = 1.0, Duration = duration, EasingFunction = ease };
            Storyboard.SetTarget(animX, scale);
            Storyboard.SetTargetProperty(animX, nameof(ScaleTransform.ScaleX));

            var animY = new DoubleAnimation { From = 0.0, To = 1.0, Duration = duration, EasingFunction = ease };
            Storyboard.SetTarget(animY, scale);
            Storyboard.SetTargetProperty(animY, nameof(ScaleTransform.ScaleY));

            sb.Children.Add(animX);
            sb.Children.Add(animY);
            sb.Begin();
        }

        // Đóng dialog sau 1.5s
        _autoCloseTimer.Start();
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
        if (StageText != null)
        {
            StageText.Text = stageText;
        }
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
