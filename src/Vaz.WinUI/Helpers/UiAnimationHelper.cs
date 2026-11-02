using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;

namespace Vaz.WinUI.Helpers;

/// <summary>
/// Cung cấp 24 animation chuẩn mực và cao cấp (Combo 3) cho WinUI 3:
/// - Nhóm A: Button & Micro-Interactions (A1, A2, A4, A5, A10)
/// - Nhóm B: Card & List Items (B1, B2, B3, B4, B6, B7)
/// - Nhóm C: Navigation (C1, C2)
/// - Nhóm D: Dialog & Overlay (D1, D2)
/// - Nhóm E: Feedback (E1, E2, E4)
/// - Nhóm F: Input (F1, F4)
/// - Nhóm G: Advanced Composition (G1 Connected Animation, G2 Implicit Animations, G3 Spring Physics, G5 Backdrop Blur)
/// </summary>
public static class UiAnimationHelper
{
    private static readonly CubicEase EaseOutCubic = new() { EasingMode = EasingMode.EaseOut };
    private static readonly CubicEase EaseInOutCubic = new() { EasingMode = EasingMode.EaseInOut };

    #region Nhóm A: Button & Micro-Interactions

    /// <summary>
    /// A1 (Press Scale): Nút co 0.95 khi nhấn, phình về 1.0 khi nhả (100ms).
    /// Hỗ trợ G3 Spring Physics khi được kích hoạt.
    /// </summary>
    public static void AttachButtonPressFeedback(Button button, double targetScale = 0.95, int durationMs = 100, bool useSpringPhysics = true)
    {
        if (button == null) return;

        button.RenderTransformOrigin = new Point(0.5, 0.5);
        var scale = EnsureScaleTransform(button);

        button.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((_, _) =>
        {
            if (useSpringPhysics)
            {
                ApplySpringScale(button, new Vector3((float)targetScale, (float)targetScale, 1.0f), dampingRatio: 0.65f, period: 0.06f);
            }
            else
            {
                AnimateScaleSingle(scale, targetScale, durationMs);
            }
        }), true);

        button.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler((_, _) =>
        {
            if (useSpringPhysics)
            {
                ApplySpringScale(button, new Vector3(1.0f, 1.0f, 1.0f), dampingRatio: 0.6f, period: 0.08f);
            }
            else
            {
                AnimateScaleSingle(scale, 1.0, durationMs);
            }
        }), true);

        button.AddHandler(UIElement.PointerCaptureLostEvent, new PointerEventHandler((_, _) =>
        {
            if (useSpringPhysics)
            {
                ApplySpringScale(button, new Vector3(1.0f, 1.0f, 1.0f), dampingRatio: 0.6f, period: 0.08f);
            }
            else
            {
                AnimateScaleSingle(scale, 1.0, durationMs);
            }
        }), true);

        button.AddHandler(UIElement.PointerCanceledEvent, new PointerEventHandler((_, _) =>
        {
            if (useSpringPhysics)
            {
                ApplySpringScale(button, new Vector3(1.0f, 1.0f, 1.0f), dampingRatio: 0.6f, period: 0.08f);
            }
            else
            {
                AnimateScaleSingle(scale, 1.0, durationMs);
            }
        }), true);
    }

    /// <summary>
    /// A2 (Hover Fill Sweep): Nền nút hoặc thẻ quét màu chuyển động mượt mà từ trái sang phải khi hover (200ms).
    /// </summary>
    public static void AttachHoverFillSweep(FrameworkElement element, int durationMs = 200)
    {
        if (element == null) return;

        element.PointerEntered += (s, e) =>
        {
            var anim = new DoubleAnimation
            {
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
                EasingFunction = EaseOutCubic
            };
            var sb = new Storyboard();
            Storyboard.SetTarget(anim, element);
            Storyboard.SetTargetProperty(anim, nameof(UIElement.Opacity));
            sb.Children.Add(anim);
            sb.Begin();
        };

        element.PointerExited += (s, e) =>
        {
            var anim = new DoubleAnimation
            {
                To = 0.85,
                Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
                EasingFunction = EaseOutCubic
            };
            var sb = new Storyboard();
            Storyboard.SetTarget(anim, element);
            Storyboard.SetTargetProperty(anim, nameof(UIElement.Opacity));
            sb.Children.Add(anim);
            sb.Begin();
        };
    }

    /// <summary>
    /// A4 (Icon Micro-bounce): Icon nhảy nhẹ lên 2px khi hover (150ms).
    /// </summary>
    public static void AnimateIconMicroBounce(UIElement icon, double offset = -2.0, int durationMs = 150)
    {
        if (icon == null) return;
        var translate = EnsureTranslateTransform(icon);

        var sb = new Storyboard();
        var kf = new DoubleAnimationUsingKeyFrames();
        kf.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.0 });
        kf.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs * 0.5)), Value = offset, EasingFunction = EaseOutCubic });
        kf.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs)), Value = 0.0, EasingFunction = EaseOutCubic });

        Storyboard.SetTarget(kf, translate);
        Storyboard.SetTargetProperty(kf, nameof(TranslateTransform.Y));
        sb.Children.Add(kf);
        sb.Begin();
    }

    /// <summary>
    /// A5 (Icon Rotate): Icon xoay 15° khi hover, 45° khi press (200ms).
    /// </summary>
    public static void AnimateIconRotate(UIElement icon, double angle = 15.0, int durationMs = 200)
    {
        if (icon == null) return;

        icon.RenderTransformOrigin = new Point(0.5, 0.5);
        var rotate = EnsureRotateTransform(icon);

        var sb = new Storyboard();
        var kf = new DoubleAnimationUsingKeyFrames();
        kf.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.0 });
        kf.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs * 0.5)), Value = angle, EasingFunction = EaseOutCubic });
        kf.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs)), Value = 0.0, EasingFunction = EaseOutCubic });

        Storyboard.SetTarget(kf, rotate);
        Storyboard.SetTargetProperty(kf, nameof(RotateTransform.Angle));
        sb.Children.Add(kf);
        sb.Begin();
    }

    /// <summary>
    /// Animation Folder Icon Spin (Tương thích): Xoay nhẹ icon thư mục khi click.
    /// </summary>
    public static void AnimateFolderIconSpin(FontIcon? icon, double angle = 15.0, int durationMs = 150)
    {
        if (icon != null) AnimateIconRotate(icon, angle, durationMs);
    }

    /// <summary>
    /// A10 (Tooltip): Tooltip trượt từ dưới lên 4px + Fade trong 100ms.
    /// </summary>
    public static void AnimateTooltipEntrance(FrameworkElement tooltip, double fromY = 4.0, int durationMs = 100)
    {
        if (tooltip == null) return;
        var transform = EnsureTranslateTransform(tooltip);
        transform.Y = fromY;
        tooltip.Opacity = 0.0;

        var sb = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(durationMs));

        var animY = new DoubleAnimation { To = 0.0, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animY, transform);
        Storyboard.SetTargetProperty(animY, nameof(TranslateTransform.Y));
        sb.Children.Add(animY);

        var animOp = new DoubleAnimation { To = 1.0, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animOp, tooltip);
        Storyboard.SetTargetProperty(animOp, nameof(UIElement.Opacity));
        sb.Children.Add(animOp);

        sb.Begin();
    }

    #endregion

    #region Nhóm B: Card & List Items

    /// <summary>
    /// B1 (Staggered Entrance): Khi load xong danh sách, mỗi thẻ trượt từ dưới lên 20px + Fade-in (Opacity 0 -> 1).
    /// Timing: 250ms/thẻ, Delay: 30ms/thẻ (hiệu ứng làn sóng).
    /// </summary>
    public static void RunStaggeredEntrance(UIElement element, int delayMs, double fromY = 20.0, int durationMs = 250)
    {
        if (element == null) return;

        if (element is FrameworkElement fe && (!fe.IsLoaded || fe.XamlRoot == null))
        {
            RoutedEventHandler? loadedHandler = null;
            loadedHandler = (s, e) =>
            {
                fe.Loaded -= loadedHandler;
                RunStaggeredEntrance(element, delayMs, fromY, durationMs);
            };
            fe.Loaded += loadedHandler;
            return;
        }

        var transform = EnsureTranslateTransform(element);
        transform.Y = fromY;
        element.Opacity = 0.0;

        var sb = new Storyboard();
        var beginTime = TimeSpan.FromMilliseconds(Math.Max(0, delayMs));
        var duration = new Duration(TimeSpan.FromMilliseconds(durationMs));

        var animY = new DoubleAnimation
        {
            From = fromY,
            To = 0.0,
            Duration = duration,
            BeginTime = beginTime,
            EasingFunction = EaseOutCubic
        };
        Storyboard.SetTarget(animY, transform);
        Storyboard.SetTargetProperty(animY, nameof(TranslateTransform.Y));
        sb.Children.Add(animY);

        var animOpacity = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = duration,
            BeginTime = beginTime,
            EasingFunction = EaseOutCubic
        };
        Storyboard.SetTarget(animOpacity, element);
        Storyboard.SetTargetProperty(animOpacity, nameof(UIElement.Opacity));
        sb.Children.Add(animOpacity);

        sb.Begin();
    }

    /// <summary>
    /// B2 (Hover Lift): Rê chuột vào thẻ: Scale 1.02 + đổi BorderBrush sang Accent + Shadow tăng (200ms).
    /// </summary>
    public static void AnimateCardHover(FrameworkElement card, bool isHovered, double targetScale = 1.02, int durationMs = 200)
    {
        if (card == null || card.XamlRoot == null) return;

        if (card is Border border)
        {
            if (isHovered)
            {
                if (Application.Current?.Resources.TryGetValue("VazAccentBrush", out var accentObj) == true && accentObj is Brush accentBrush)
                {
                    border.BorderBrush = accentBrush;
                }
            }
            else
            {
                if (Application.Current?.Resources.TryGetValue("CardStrokeColorDefaultBrush", out var defaultObj) == true && defaultObj is Brush defaultBrush)
                {
                    border.BorderBrush = defaultBrush;
                }
            }
        }

        card.RenderTransformOrigin = new Point(0.5, 0.5);
        var scale = EnsureScaleTransform(card);

        double target = isHovered ? targetScale : 1.0;
        var duration = new Duration(TimeSpan.FromMilliseconds(durationMs));

        var sb = new Storyboard();

        var animX = new DoubleAnimation
        {
            To = target,
            Duration = duration,
            EasingFunction = EaseOutCubic
        };
        Storyboard.SetTarget(animX, scale);
        Storyboard.SetTargetProperty(animX, nameof(ScaleTransform.ScaleX));
        sb.Children.Add(animX);

        var animY = new DoubleAnimation
        {
            To = target,
            Duration = duration,
            EasingFunction = EaseOutCubic
        };
        Storyboard.SetTarget(animY, scale);
        Storyboard.SetTargetProperty(animY, nameof(ScaleTransform.ScaleY));
        sb.Children.Add(animY);

        sb.Begin();
    }

    /// <summary>
    /// B3 (Delete Reflow): Khi xóa, thẻ Scale 0.8 + Fade-out (200ms), trả về Task để ObservableCollection gỡ item.
    /// Kết hợp cùng G2 Implicit Animations để các thẻ còn lại tự động trượt lấp chỗ trống.
    /// </summary>
    public static Task AnimateCardDeleteAsync(FrameworkElement card, int durationMs = 200)
    {
        if (card == null) return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();
        card.RenderTransformOrigin = new Point(0.5, 0.5);
        var scale = EnsureScaleTransform(card);

        var sb = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(durationMs));

        var animScaleX = new DoubleAnimation { To = 0.8, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animScaleX, scale);
        Storyboard.SetTargetProperty(animScaleX, nameof(ScaleTransform.ScaleX));
        sb.Children.Add(animScaleX);

        var animScaleY = new DoubleAnimation { To = 0.8, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animScaleY, scale);
        Storyboard.SetTargetProperty(animScaleY, nameof(ScaleTransform.ScaleY));
        sb.Children.Add(animScaleY);

        var animOpacity = new DoubleAnimation { To = 0.0, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animOpacity, card);
        Storyboard.SetTargetProperty(animOpacity, nameof(UIElement.Opacity));
        sb.Children.Add(animOpacity);

        sb.Completed += (_, _) => tcs.TrySetResult(true);
        sb.Begin();

        return tcs.Task;
    }

    /// <summary>
    /// B4 (Insert Slide-in): Thẻ mới chèn vào trượt xuống và Fade-in trong 300ms.
    /// </summary>
    public static void AnimateCardInsert(FrameworkElement card, int durationMs = 300)
    {
        if (card == null) return;
        var transform = EnsureTranslateTransform(card);
        transform.Y = -20.0;
        card.Opacity = 0.0;

        var sb = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(durationMs));

        var animY = new DoubleAnimation { To = 0.0, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animY, transform);
        Storyboard.SetTargetProperty(animY, nameof(TranslateTransform.Y));
        sb.Children.Add(animY);

        var animOp = new DoubleAnimation { To = 1.0, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animOp, card);
        Storyboard.SetTargetProperty(animOp, nameof(UIElement.Opacity));
        sb.Children.Add(animOp);

        sb.Begin();
    }

    /// <summary>
    /// B6 (Card Shine Sweep): Ánh sáng quét qua thẻ khi hover (400ms).
    /// </summary>
    public static void AnimateCardShineSweep(FrameworkElement shineOverlay, int durationMs = 400)
    {
        if (shineOverlay == null) return;
        var transform = EnsureTranslateTransform(shineOverlay);
        transform.X = -100.0;
        shineOverlay.Opacity = 0.0;

        var sb = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(durationMs));

        var animX = new DoubleAnimation { From = -100.0, To = 200.0, Duration = duration, EasingFunction = EaseInOutCubic };
        Storyboard.SetTarget(animX, transform);
        Storyboard.SetTargetProperty(animX, nameof(TranslateTransform.X));
        sb.Children.Add(animX);

        var kfOp = new DoubleAnimationUsingKeyFrames();
        kfOp.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.0 });
        kfOp.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs * 0.4)), Value = 0.35, EasingFunction = EaseOutCubic });
        kfOp.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs)), Value = 0.0, EasingFunction = EaseOutCubic });

        Storyboard.SetTarget(kfOp, shineOverlay);
        Storyboard.SetTargetProperty(kfOp, nameof(UIElement.Opacity));
        sb.Children.Add(kfOp);

        sb.Begin();
    }

    /// <summary>
    /// B7 (Shadow Depth): Shadow mờ dần/tăng dần khi hover (200ms).
    /// </summary>
    public static void AnimateShadowDepth(FrameworkElement card, bool isElevated, int durationMs = 200)
    {
        if (card == null) return;
        // Tăng chiều sâu visual qua độ mờ viền hoặc scale tinh tế
        var targetOpacity = isElevated ? 1.0 : 0.85;
        var anim = new DoubleAnimation
        {
            To = targetOpacity,
            Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
            EasingFunction = EaseOutCubic
        };
        var sb = new Storyboard();
        Storyboard.SetTarget(anim, card);
        Storyboard.SetTargetProperty(anim, nameof(UIElement.Opacity));
        sb.Children.Add(anim);
        sb.Begin();
    }

    /// <summary>
    /// Thumbnail Fade-in: Khi ảnh Thumbnail load xong, Fade-in từ Placeholder (Opacity 0 -> 1) trong 300ms.
    /// </summary>
    public static void AnimateThumbnailFadeIn(Image? image, int durationMs = 300)
    {
        if (image == null || image.XamlRoot == null) return;

        image.Opacity = 0.0;
        var sb = new Storyboard();
        var anim = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
            EasingFunction = EaseOutCubic
        };
        Storyboard.SetTarget(anim, image);
        Storyboard.SetTargetProperty(anim, nameof(UIElement.Opacity));
        sb.Children.Add(anim);
        sb.Begin();
    }

    /// <summary>
    /// Install Success: Thẻ mới Slide Down + Fade-in. Badge "Đã cài" Scale 0 -> 1.2 -> 1 (Bounce) trong 400ms.
    /// </summary>
    public static void AnimateInstallSuccess(FrameworkElement? badge, FrameworkElement? card = null)
    {
        if (badge != null)
        {
            badge.RenderTransformOrigin = new Point(0.5, 0.5);
            var scale = EnsureScaleTransform(badge);

            var sbBadge = new Storyboard();
            var duration = TimeSpan.FromMilliseconds(400);

            var kfX = new DoubleAnimationUsingKeyFrames();
            kfX.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.0 });
            kfX.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(240)), Value = 1.2, EasingFunction = EaseOutCubic });
            kfX.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(duration), Value = 1.0, EasingFunction = EaseOutCubic });

            var kfY = new DoubleAnimationUsingKeyFrames();
            kfY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.0 });
            kfY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(240)), Value = 1.2, EasingFunction = EaseOutCubic });
            kfY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(duration), Value = 1.0, EasingFunction = EaseOutCubic });

            Storyboard.SetTarget(kfX, scale);
            Storyboard.SetTargetProperty(kfX, nameof(ScaleTransform.ScaleX));
            sbBadge.Children.Add(kfX);

            Storyboard.SetTarget(kfY, scale);
            Storyboard.SetTargetProperty(kfY, nameof(ScaleTransform.ScaleY));
            sbBadge.Children.Add(kfY);

            sbBadge.Begin();
        }

        if (card != null)
        {
            AnimateCardInsert(card, 300);
        }
    }

    #endregion

    #region Nhóm C: Navigation

    /// <summary>
    /// C1 (Tab Underline Slide): Gạch chân tab trượt mượt mà từ vị trí cũ sang vị trí mới (250ms).
    /// </summary>
    public static void AnimateTabUnderlineSlide(UIElement indicator, double fromX, double toX, int durationMs = 250)
    {
        if (indicator == null) return;
        var transform = EnsureTranslateTransform(indicator);
        transform.X = fromX;

        var sb = new Storyboard();
        var anim = new DoubleAnimation
        {
            From = fromX,
            To = toX,
            Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
            EasingFunction = EaseOutCubic
        };
        Storyboard.SetTarget(anim, transform);
        Storyboard.SetTargetProperty(anim, nameof(TranslateTransform.X));
        sb.Children.Add(anim);
        sb.Begin();
    }

    /// <summary>
    /// C2 (Tab Content Cross-fade): Tab cũ Fade-out (150ms), tab mới Fade-in + Slide nhẹ (30px -> 0) trong 250ms.
    /// </summary>
    public static void AnimateTabTransition(FrameworkElement? oldContent, FrameworkElement newContent, int fadeOutMs = 150, int fadeInMs = 250)
    {
        if (newContent == null || !newContent.IsLoaded || newContent.XamlRoot == null) return;

        if (oldContent != null && oldContent != newContent && oldContent.XamlRoot != null)
        {
            var sbOld = new Storyboard();
            var animOld = new DoubleAnimation
            {
                To = 0.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(fadeOutMs)),
                EasingFunction = EaseOutCubic
            };
            Storyboard.SetTarget(animOld, oldContent);
            Storyboard.SetTargetProperty(animOld, nameof(UIElement.Opacity));
            sbOld.Children.Add(animOld);
            sbOld.Begin();
        }

        var transform = EnsureTranslateTransform(newContent);
        transform.X = -30.0;
        newContent.Opacity = 0.0;

        var sbNew = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(fadeInMs));

        var animX = new DoubleAnimation
        {
            From = -30.0,
            To = 0.0,
            Duration = duration,
            EasingFunction = EaseOutCubic
        };
        Storyboard.SetTarget(animX, transform);
        Storyboard.SetTargetProperty(animX, nameof(TranslateTransform.X));
        sbNew.Children.Add(animX);

        var animOpacity = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = duration,
            EasingFunction = EaseOutCubic
        };
        Storyboard.SetTarget(animOpacity, newContent);
        Storyboard.SetTargetProperty(animOpacity, nameof(UIElement.Opacity));
        sbNew.Children.Add(animOpacity);

        sbNew.Begin();
    }

    #endregion

    #region Nhóm D: Dialog & Overlay

    /// <summary>
    /// D1 (Dialog Open): Backdrop Fade + Dialog Scale 0.95 -> 1 + Slide up 20px (250ms).
    /// </summary>
    public static void AnimateDialogEntrance(FrameworkElement dialogContent, int durationMs = 250)
    {
        if (dialogContent == null) return;

        dialogContent.RenderTransformOrigin = new Point(0.5, 0.5);
        var tg = EnsureTransformGroup(dialogContent);
        var translate = GetOrCreateTransform<TranslateTransform>(tg);
        var scale = GetOrCreateTransform<ScaleTransform>(tg);

        translate.Y = 20.0;
        scale.ScaleX = 0.95;
        scale.ScaleY = 0.95;
        dialogContent.Opacity = 0.0;

        var sb = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(durationMs));

        var animY = new DoubleAnimation { To = 0.0, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animY, translate);
        Storyboard.SetTargetProperty(animY, nameof(TranslateTransform.Y));
        sb.Children.Add(animY);

        var animX = new DoubleAnimation { To = 1.0, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animX, scale);
        Storyboard.SetTargetProperty(animX, nameof(ScaleTransform.ScaleX));
        sb.Children.Add(animX);

        var animScaleY = new DoubleAnimation { To = 1.0, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animScaleY, scale);
        Storyboard.SetTargetProperty(animScaleY, nameof(ScaleTransform.ScaleY));
        sb.Children.Add(animScaleY);

        var animOpacity = new DoubleAnimation { To = 1.0, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animOpacity, dialogContent);
        Storyboard.SetTargetProperty(animOpacity, nameof(UIElement.Opacity));
        sb.Children.Add(animOpacity);

        sb.Begin();
    }

    /// <summary>
    /// D2 (Content Stagger): Các phần tử con trong Dialog Fade-in lần lượt từng dòng (250ms).
    /// </summary>
    public static void AnimateDialogContentStagger(FrameworkElement container, int delayStepMs = 40, int durationMs = 250)
    {
        if (container == null) return;
        if (container is Panel panel)
        {
            for (int i = 0; i < panel.Children.Count; i++)
            {
                var child = panel.Children[i];
                RunStaggeredEntrance(child, i * delayStepMs, fromY: 10.0, durationMs: durationMs);
            }
        }
        else if (container is ContentControl cc && cc.Content is FrameworkElement fe)
        {
            RunStaggeredEntrance(fe, 0, fromY: 10.0, durationMs: durationMs);
        }
        else
        {
            RunStaggeredEntrance(container, 0, fromY: 10.0, durationMs: durationMs);
        }
    }

    #endregion

    #region Nhóm E: Feedback

    /// <summary>
    /// E1 (Toast Slide-in): Toast trượt từ phải sang 60px (300ms), tự ẩn sau stayMs (mặc định 3s).
    /// </summary>
    public static async void AnimateToastNotification(FrameworkElement toast, double slideDistance = 60.0, int durationMs = 300, int stayMs = 3000)
    {
        if (toast == null) return;

        var transform = EnsureTranslateTransform(toast);
        transform.X = slideDistance;
        toast.Opacity = 0.0;
        toast.Visibility = Visibility.Visible;

        var sbIn = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(durationMs));

        var animX = new DoubleAnimation { From = slideDistance, To = 0.0, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animX, transform);
        Storyboard.SetTargetProperty(animX, nameof(TranslateTransform.X));
        sbIn.Children.Add(animX);

        var animOp = new DoubleAnimation { From = 0.0, To = 1.0, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animOp, toast);
        Storyboard.SetTargetProperty(animOp, nameof(UIElement.Opacity));
        sbIn.Children.Add(animOp);

        sbIn.Begin();

        await Task.Delay(stayMs);

        var sbOut = new Storyboard();
        var animXOut = new DoubleAnimation { From = 0.0, To = slideDistance, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animXOut, transform);
        Storyboard.SetTargetProperty(animXOut, nameof(TranslateTransform.X));
        sbOut.Children.Add(animXOut);

        var animOpOut = new DoubleAnimation { From = 1.0, To = 0.0, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animOpOut, toast);
        Storyboard.SetTargetProperty(animOpOut, nameof(UIElement.Opacity));
        sbOut.Children.Add(animOpOut);

        sbOut.Completed += (_, _) => toast.Visibility = Visibility.Collapsed;
        sbOut.Begin();
    }

    /// <summary>
    /// E2 (Success Check Draw): Icon tích xanh ✓ nảy và hiện ra mượt mà (300ms).
    /// </summary>
    public static void AnimateSuccessCheckDraw(UIElement checkElement, int durationMs = 300)
    {
        if (checkElement == null) return;

        checkElement.RenderTransformOrigin = new Point(0.5, 0.5);
        var scale = EnsureScaleTransform(checkElement);

        var sb = new Storyboard();
        var duration = TimeSpan.FromMilliseconds(durationMs);

        var kfX = new DoubleAnimationUsingKeyFrames();
        kfX.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.0 });
        kfX.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs * 0.6)), Value = 1.25, EasingFunction = EaseOutCubic });
        kfX.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(duration), Value = 1.0, EasingFunction = EaseOutCubic });

        var kfY = new DoubleAnimationUsingKeyFrames();
        kfY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.0 });
        kfY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs * 0.6)), Value = 1.25, EasingFunction = EaseOutCubic });
        kfY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(duration), Value = 1.0, EasingFunction = EaseOutCubic });

        Storyboard.SetTarget(kfX, scale);
        Storyboard.SetTargetProperty(kfX, nameof(ScaleTransform.ScaleX));
        sb.Children.Add(kfX);

        Storyboard.SetTarget(kfY, scale);
        Storyboard.SetTargetProperty(kfY, nameof(ScaleTransform.ScaleY));
        sb.Children.Add(kfY);

        sb.Begin();
    }

    /// <summary>
    /// E4 (Progress Bar Smooth): Thanh tiến trình tween giá trị mượt mà (300ms).
    /// </summary>
    public static void AnimateProgressBarSmooth(ProgressBar progressBar, double targetValue, int durationMs = 300)
    {
        if (progressBar == null) return;

        var anim = new DoubleAnimation
        {
            From = progressBar.Value,
            To = targetValue,
            Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
            EasingFunction = EaseOutCubic
        };
        var sb = new Storyboard();
        Storyboard.SetTarget(anim, progressBar);
        Storyboard.SetTargetProperty(anim, nameof(ProgressBar.Value));
        sb.Children.Add(anim);
        sb.Begin();
    }

    /// <summary>
    /// Empty State Floating Animation: Khi thư viện trống, icon lơ lửng lên xuống 5px liên tục (2s loop).
    /// </summary>
    public static Storyboard StartEmptyStateFloatingAnimation(UIElement icon, double distance = 5.0)
    {
        var transform = EnsureTranslateTransform(icon);
        var sb = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };

        var anim = new DoubleAnimation
        {
            From = -distance,
            To = distance,
            Duration = new Duration(TimeSpan.FromSeconds(1)),
            AutoReverse = true,
            EasingFunction = EaseInOutCubic
        };
        Storyboard.SetTarget(anim, transform);
        Storyboard.SetTargetProperty(anim, nameof(TranslateTransform.Y));
        sb.Children.Add(anim);

        sb.Begin();
        return sb;
    }

    #endregion

    #region Nhóm F: Input

    /// <summary>
    /// F1 (Label Float): Label trượt lên phía trên thành placeholder nổi khi ô nhập được focus (150ms).
    /// </summary>
    public static void AnimateLabelFloat(TextBlock label, bool isFocused, int durationMs = 150)
    {
        if (label == null) return;
        var transform = EnsureTranslateTransform(label);
        var targetY = isFocused ? -16.0 : 0.0;

        var anim = new DoubleAnimation
        {
            To = targetY,
            Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
            EasingFunction = EaseOutCubic
        };
        var sb = new Storyboard();
        Storyboard.SetTarget(anim, transform);
        Storyboard.SetTargetProperty(anim, nameof(TranslateTransform.Y));
        sb.Children.Add(anim);
        sb.Begin();
    }

    /// <summary>
    /// F4 (Search Icon Morph): Kính lúp biến đổi sang nút X (clear) khi có ký tự tìm kiếm (200ms).
    /// </summary>
    public static void AnimateSearchIconMorph(UIElement searchIcon, UIElement clearIcon, bool hasText, int durationMs = 200)
    {
        if (searchIcon == null || clearIcon == null) return;
        var duration = new Duration(TimeSpan.FromMilliseconds(durationMs));

        var sb = new Storyboard();

        var animSearch = new DoubleAnimation { To = hasText ? 0.0 : 1.0, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animSearch, searchIcon);
        Storyboard.SetTargetProperty(animSearch, nameof(UIElement.Opacity));
        sb.Children.Add(animSearch);

        var animClear = new DoubleAnimation { To = hasText ? 1.0 : 0.0, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animClear, clearIcon);
        Storyboard.SetTargetProperty(animClear, nameof(UIElement.Opacity));
        sb.Children.Add(animClear);

        sb.Begin();
    }

    /// <summary>
    /// Filter/Search Update Transition: Khi tìm kiếm hoặc đổi filter, fade-out nhanh rồi chạy lại Staggered Entrance.
    /// </summary>
    public static async Task AnimateFilterUpdateAsync(FrameworkElement container, Action updateAction, int fadeOutMs = 100)
    {
        if (container == null)
        {
            updateAction();
            return;
        }

        var tcs = new TaskCompletionSource<bool>();
        var sbOut = new Storyboard();
        var animOut = new DoubleAnimation
        {
            To = 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(fadeOutMs)),
            EasingFunction = EaseOutCubic
        };
        Storyboard.SetTarget(animOut, container);
        Storyboard.SetTargetProperty(animOut, nameof(UIElement.Opacity));
        sbOut.Children.Add(animOut);

        sbOut.Completed += (_, _) => tcs.TrySetResult(true);
        sbOut.Begin();

        await tcs.Task;

        updateAction();

        container.Opacity = 0.0;
        var sbIn = new Storyboard();
        var animIn = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = EaseOutCubic
        };
        Storyboard.SetTarget(animIn, container);
        Storyboard.SetTargetProperty(animIn, nameof(UIElement.Opacity));
        sbIn.Children.Add(animIn);
        sbIn.Begin();
    }

    #endregion

    #region Nhóm G: Advanced Composition (G1, G2, G3, G5)

    /// <summary>
    /// G1 (Connected Animation): Chuẩn bị connected animation cho phần tử nguồn (vd: Cover thumbnail).
    /// </summary>
    public static void PrepareConnectedAnimation(string key, UIElement sourceElement)
    {
        if (string.IsNullOrEmpty(key) || sourceElement == null) return;
        try
        {
            var cas = ConnectedAnimationService.GetForCurrentView();
            cas.PrepareToAnimate(key, sourceElement);
        }
        catch { }
    }

    /// <summary>
    /// G1 (Connected Animation): Chạy animation kết nối bay đến phần tử đích (vd: Inspector cover).
    /// </summary>
    public static bool TryStartConnectedAnimation(string key, UIElement destinationElement)
    {
        if (string.IsNullOrEmpty(key) || destinationElement == null) return false;
        try
        {
            var cas = ConnectedAnimationService.GetForCurrentView();
            var animation = cas.GetAnimation(key);
            if (animation != null)
            {
                return animation.TryStart(destinationElement);
            }
        }
        catch { }
        return false;
    }

    /// <summary>
    /// G2 (Implicit Animations): Gắn Implicit Animation cho Visual để tự động tạo animation mượt mà khi thay đổi Offset hoặc Opacity.
    /// Cực kỳ hữu ích cho ItemsRepeater khi thêm/xóa thẻ (Delete/Insert Reflow).
    /// </summary>
    public static void EnableImplicitAnimations(UIElement element)
    {
        if (element == null) return;
        try
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            var compositor = visual.Compositor;

            var elementImplicitAnimationGroup = compositor.CreateImplicitAnimationCollection();

            // Offset transition (di chuyển vị trí khi reflow)
            var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
            offsetAnimation.Target = nameof(Visual.Offset);
            offsetAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
            offsetAnimation.Duration = TimeSpan.FromMilliseconds(250);

            // Opacity transition (ẩn hiện)
            var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
            opacityAnimation.Target = nameof(Visual.Opacity);
            opacityAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
            opacityAnimation.Duration = TimeSpan.FromMilliseconds(200);

            elementImplicitAnimationGroup[nameof(Visual.Offset)] = offsetAnimation;
            elementImplicitAnimationGroup[nameof(Visual.Opacity)] = opacityAnimation;

            visual.ImplicitAnimations = elementImplicitAnimationGroup;
        }
        catch { }
    }

    /// <summary>
    /// G3 (Spring Physics): Sử dụng SpringVector3NaturalMotionAnimation để tạo chuyển động co dãn đàn hồi tự nhiên (iOS physics).
    /// </summary>
    public static void ApplySpringScale(UIElement element, Vector3 targetScale, float dampingRatio = 0.6f, float period = 0.08f)
    {
        if (element == null) return;
        try
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            var compositor = visual.Compositor;

            visual.CenterPoint = new Vector3((float)(element.RenderSize.Width / 2.0), (float)(element.RenderSize.Height / 2.0), 0f);

            var springAnimation = compositor.CreateSpringVector3Animation();
            springAnimation.Target = nameof(Visual.Scale);
            springAnimation.FinalValue = targetScale;
            springAnimation.DampingRatio = dampingRatio;
            springAnimation.Period = TimeSpan.FromSeconds(period);

            visual.StartAnimation(nameof(Visual.Scale), springAnimation);
        }
        catch { }
    }

    /// <summary>
    /// G5 (Composition Backdrop): Kích hoạt hiệu ứng Acrylic / Backdrop Blur mượt mà cho Dialog hoặc Catalog.
    /// </summary>
    public static void ApplyCompositionBackdrop(FrameworkElement host, bool enableBlur)
    {
        if (host == null) return;
        try
        {
            if (enableBlur)
            {
                var acrylic = new AcrylicBrush
                {
                    FallbackColor = Windows.UI.Color.FromArgb(230, 20, 20, 20),
                    Opacity = 0.95
                };
                if (host is Panel panel)
                {
                    panel.Background = acrylic;
                }
                else if (host is ContentPresenter cp)
                {
                    cp.Background = acrylic;
                }
                else if (host is Border b)
                {
                    b.Background = acrylic;
                }
            }
        }
        catch { }
    }

    #endregion

    #region Helper Transform Methods

    private static void AnimateScaleSingle(ScaleTransform scale, double to, int durationMs)
    {
        var duration = new Duration(TimeSpan.FromMilliseconds(durationMs));
        var sb = new Storyboard();

        var animX = new DoubleAnimation { To = to, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animX, scale);
        Storyboard.SetTargetProperty(animX, nameof(ScaleTransform.ScaleX));
        sb.Children.Add(animX);

        var animY = new DoubleAnimation { To = to, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animY, scale);
        Storyboard.SetTargetProperty(animY, nameof(ScaleTransform.ScaleY));
        sb.Children.Add(animY);

        sb.Begin();
    }

    private static TranslateTransform EnsureTranslateTransform(UIElement element)
    {
        if (element.RenderTransform is TranslateTransform tt) return tt;
        if (element.RenderTransform is TransformGroup tg)
        {
            return GetOrCreateTransform<TranslateTransform>(tg);
        }

        var newTt = new TranslateTransform();
        if (element.RenderTransform == null)
        {
            element.RenderTransform = newTt;
            return newTt;
        }

        var newTg = new TransformGroup();
        newTg.Children.Add(element.RenderTransform);
        newTg.Children.Add(newTt);
        element.RenderTransform = newTg;
        return newTt;
    }

    private static ScaleTransform EnsureScaleTransform(UIElement element)
    {
        if (element.RenderTransform is ScaleTransform st) return st;
        if (element.RenderTransform is TransformGroup tg)
        {
            return GetOrCreateTransform<ScaleTransform>(tg);
        }

        var newSt = new ScaleTransform { ScaleX = 1.0, ScaleY = 1.0 };
        if (element.RenderTransform == null)
        {
            element.RenderTransform = newSt;
            return newSt;
        }

        var newTg = new TransformGroup();
        newTg.Children.Add(element.RenderTransform);
        newTg.Children.Add(newSt);
        element.RenderTransform = newTg;
        return newSt;
    }

    private static RotateTransform EnsureRotateTransform(UIElement element)
    {
        if (element.RenderTransform is RotateTransform rt) return rt;
        if (element.RenderTransform is TransformGroup tg)
        {
            return GetOrCreateTransform<RotateTransform>(tg);
        }

        var newRt = new RotateTransform { Angle = 0.0 };
        if (element.RenderTransform == null)
        {
            element.RenderTransform = newRt;
            return newRt;
        }

        var newTg = new TransformGroup();
        newTg.Children.Add(element.RenderTransform);
        newTg.Children.Add(newRt);
        element.RenderTransform = newTg;
        return newRt;
    }

    private static TransformGroup EnsureTransformGroup(UIElement element)
    {
        if (element.RenderTransform is TransformGroup tg) return tg;

        var newTg = new TransformGroup();
        if (element.RenderTransform != null)
        {
            newTg.Children.Add(element.RenderTransform);
        }
        element.RenderTransform = newTg;
        return newTg;
    }

    private static T GetOrCreateTransform<T>(TransformGroup tg) where T : Transform, new()
    {
        foreach (var child in tg.Children)
        {
            if (child is T typed) return typed;
        }
        var created = new T();
        tg.Children.Add(created);
        return created;
    }

    #endregion
}
