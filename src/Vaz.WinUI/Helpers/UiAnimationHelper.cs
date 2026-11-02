using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;

namespace Vaz.WinUI.Helpers;

/// <summary>
/// Cung cấp 11 animation chuẩn mực cho WinUI 3 (Nhóm 1: Premium Feel, Nhóm 2: Interaction Feedback, Nhóm 3: UX Booster).
/// Sử dụng Storyboard và Composition mượt mà, tối ưu hiệu năng không gây drop frame.
/// </summary>
public static class UiAnimationHelper
{
    private static readonly CubicEase EaseOutCubic = new() { EasingMode = EasingMode.EaseOut };
    private static readonly CubicEase EaseInOutCubic = new() { EasingMode = EasingMode.EaseInOut };

    #region 1. Card Staggered Entrance
    /// <summary>
    /// Animation 1: Khi load xong danh sách, mỗi thẻ trượt từ dưới lên (TranslateY 20px -> 0) + Fade-in (Opacity 0 -> 1).
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

        // Slide Y: fromY -> 0
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

        // Fade In: 0.0 -> 1.0
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
    #endregion

    #region 2. Card Hover
    /// <summary>
    /// Animation 2: Rê chuột vào thẻ: Scale 1.02 + đổi BorderBrush sang Accent. Timing: 200ms CubicEase.
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
    #endregion

    #region 3. Thumbnail Fade-in
    /// <summary>
    /// Animation 3: Khi ảnh Thumbnail load xong, Fade-in từ Placeholder (Opacity 0 -> 1) trong 300ms.
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
    #endregion

    #region 4. Delete Animation
    /// <summary>
    /// Animation 4: Khi bấm nút Xóa, thẻ Scale 0.8 + Fade-out (Opacity -> 0) trong 200ms.
    /// Sau khi hoàn tất animation, trả về Task để caller gỡ bỏ item khỏi ObservableCollection (Smooth Reflow).
    /// </summary>
    public static Task AnimateCardDeleteAsync(FrameworkElement? card, int durationMs = 200)
    {
        if (card == null) return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();
        card.RenderTransformOrigin = new Point(0.5, 0.5);
        var scale = EnsureScaleTransform(card);

        var duration = new Duration(TimeSpan.FromMilliseconds(durationMs));
        var sb = new Storyboard();

        var animScaleX = new DoubleAnimation
        {
            To = 0.8,
            Duration = duration,
            EasingFunction = EaseOutCubic
        };
        Storyboard.SetTarget(animScaleX, scale);
        Storyboard.SetTargetProperty(animScaleX, nameof(ScaleTransform.ScaleX));
        sb.Children.Add(animScaleX);

        var animScaleY = new DoubleAnimation
        {
            To = 0.8,
            Duration = duration,
            EasingFunction = EaseOutCubic
        };
        Storyboard.SetTarget(animScaleY, scale);
        Storyboard.SetTargetProperty(animScaleY, nameof(ScaleTransform.ScaleY));
        sb.Children.Add(animScaleY);

        var animOpacity = new DoubleAnimation
        {
            To = 0.0,
            Duration = duration,
            EasingFunction = EaseOutCubic
        };
        Storyboard.SetTarget(animOpacity, card);
        Storyboard.SetTargetProperty(animOpacity, nameof(UIElement.Opacity));
        sb.Children.Add(animOpacity);

        sb.Completed += (_, _) =>
        {
            tcs.TrySetResult(true);
        };

        sb.Begin();
        return tcs.Task;
    }
    #endregion

    #region 5. Install Success
    /// <summary>
    /// Animation 5: Khi cài thành công, thẻ mới Slide Down + Fade-in.
    /// Badge "Đã cài" (nếu có) Scale từ 0 -> 1.2 -> 1 (Bounce) trong 400ms.
    /// </summary>
    public static void AnimateInstallSuccess(FrameworkElement badge, FrameworkElement? card = null)
    {
        if (badge != null)
        {
            badge.RenderTransformOrigin = new Point(0.5, 0.5);
            var scale = EnsureScaleTransform(badge);

            var sb = new Storyboard();

            // Bounce KeyFrames: 0 -> 1.2 -> 1.0 (400ms)
            var kfX = new DoubleAnimationUsingKeyFrames();
            kfX.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.0 });
            kfX.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250)), Value = 1.2, EasingFunction = EaseOutCubic });
            kfX.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400)), Value = 1.0, EasingFunction = EaseOutCubic });
            Storyboard.SetTarget(kfX, scale);
            Storyboard.SetTargetProperty(kfX, nameof(ScaleTransform.ScaleX));
            sb.Children.Add(kfX);

            var kfY = new DoubleAnimationUsingKeyFrames();
            kfY.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.0 });
            kfY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250)), Value = 1.2, EasingFunction = EaseOutCubic });
            kfY.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400)), Value = 1.0, EasingFunction = EaseOutCubic });
            Storyboard.SetTarget(kfY, scale);
            Storyboard.SetTargetProperty(kfY, nameof(ScaleTransform.ScaleY));
            sb.Children.Add(kfY);

            badge.Visibility = Visibility.Visible;
            sb.Begin();
        }

        if (card != null)
        {
            // Slide down nhẹ (-15 -> 0) + Fade in
            var transform = EnsureTranslateTransform(card);
            transform.Y = -15.0;
            card.Opacity = 0.0;

            var sbCard = new Storyboard();
            var duration = new Duration(TimeSpan.FromMilliseconds(300));

            var animY = new DoubleAnimation
            {
                From = -15.0,
                To = 0.0,
                Duration = duration,
                EasingFunction = EaseOutCubic
            };
            Storyboard.SetTarget(animY, transform);
            Storyboard.SetTargetProperty(animY, nameof(TranslateTransform.Y));
            sbCard.Children.Add(animY);

            var animOp = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = duration,
                EasingFunction = EaseOutCubic
            };
            Storyboard.SetTarget(animOp, card);
            Storyboard.SetTargetProperty(animOp, nameof(UIElement.Opacity));
            sbCard.Children.Add(animOp);

            sbCard.Begin();
        }
    }
    #endregion

    #region 6. Delete Confirmation Dialog
    /// <summary>
    /// Animation 6: Dialog xác nhận xóa: Slide Up (30px) + Scale 0.95 -> 1 + Fade-in trong 200ms.
    /// </summary>
    public static void AnimateDialogEntrance(FrameworkElement dialogContent, int durationMs = 200)
    {
        if (dialogContent == null) return;

        dialogContent.RenderTransformOrigin = new Point(0.5, 0.5);
        var tg = EnsureTransformGroup(dialogContent);
        var translate = GetOrCreateTransform<TranslateTransform>(tg);
        var scale = GetOrCreateTransform<ScaleTransform>(tg);

        translate.Y = 30.0;
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
    #endregion

    #region 7. Empty State Animation
    /// <summary>
    /// Animation 7: Khi thư viện trống, icon (File/Folder) lơ lửng lên xuống 5px liên tục (Animation Loop 2s).
    /// </summary>
    public static Storyboard StartEmptyStateFloatingAnimation(UIElement icon, double distance = 5.0)
    {
        var transform = EnsureTranslateTransform(icon);
        var sb = new Storyboard
        {
            RepeatBehavior = RepeatBehavior.Forever
        };

        // 1s lên + 1s xuống = 2s chu kỳ loop
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

    #region 8. Tab Switch Transition
    /// <summary>
    /// Animation 8: Chuyển tab Resource Packs &lt;-&gt; Shaders:
    /// Danh sách cũ Fade-out (150ms), danh sách mới Fade-in + Slide nhẹ từ trái sang phải (30px -> 0) trong 250ms.
    /// </summary>
    public static void AnimateTabTransition(FrameworkElement? oldContent, FrameworkElement newContent, int fadeOutMs = 150, int fadeInMs = 250)
    {
        if (newContent == null || !newContent.IsLoaded || newContent.XamlRoot == null)
        {
            return;
        }

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

        if (newContent != null)
        {
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
    }
    #endregion

    #region 9. Filter/Search Update
    /// <summary>
    /// Animation 9: Khi đổi bộ lọc hoặc gõ tìm kiếm: Danh sách cũ Fade-out nhanh (100ms).
    /// Khi có kết quả mới, chạy lại hiệu ứng Card Staggered Entrance (#1).
    /// </summary>
    public static async Task AnimateFilterUpdateAsync(FrameworkElement container, Action updateAction, int fadeOutMs = 100)
    {
        if (container == null)
        {
            updateAction();
            return;
        }

        var tcs = new TaskCompletionSource<bool>();
        var sb = new Storyboard();
        var anim = new DoubleAnimation
        {
            To = 0.2,
            Duration = new Duration(TimeSpan.FromMilliseconds(fadeOutMs)),
            EasingFunction = EaseOutCubic
        };
        Storyboard.SetTarget(anim, container);
        Storyboard.SetTargetProperty(anim, nameof(UIElement.Opacity));
        sb.Children.Add(anim);
        sb.Completed += (_, _) => tcs.TrySetResult(true);
        sb.Begin();

        await tcs.Task;
        updateAction();

        // Fade in lại nhanh chóng
        var sbIn = new Storyboard();
        var animIn = new DoubleAnimation
        {
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(150)),
            EasingFunction = EaseOutCubic
        };
        Storyboard.SetTarget(animIn, container);
        Storyboard.SetTargetProperty(animIn, nameof(UIElement.Opacity));
        sbIn.Children.Add(animIn);
        sbIn.Begin();
    }
    #endregion

    #region 10. Button Press Feedback
    /// <summary>
    /// Animation 10: Khi bấm bất kỳ nút chính nào (Xóa, Mở thư mục, Cài đặt...),
    /// nút Scale 0.95 trong 100ms, nhả ra Scale 1.0 trong 100ms.
    /// </summary>
    public static void AttachButtonPressFeedback(Button button, double targetScale = 0.95, int durationMs = 100)
    {
        if (button == null) return;

        button.RenderTransformOrigin = new Point(0.5, 0.5);
        var scale = EnsureScaleTransform(button);

        button.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((_, _) =>
        {
            AnimateScaleSingle(scale, targetScale, durationMs);
        }), true);

        button.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler((_, _) =>
        {
            AnimateScaleSingle(scale, 1.0, durationMs);
        }), true);

        button.AddHandler(UIElement.PointerCaptureLostEvent, new PointerEventHandler((_, _) =>
        {
            AnimateScaleSingle(scale, 1.0, durationMs);
        }), true);

        button.AddHandler(UIElement.PointerCanceledEvent, new PointerEventHandler((_, _) =>
        {
            AnimateScaleSingle(scale, 1.0, durationMs);
        }), true);
    }

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
    #endregion

    #region 11. Folder Icon Spin
    /// <summary>
    /// Animation 11: Khi bấm "Mở thư mục", icon Folder xoay nhẹ 15 độ rồi trả về (hoặc Scale 1.1) trong 150ms.
    /// </summary>
    public static void AnimateFolderIconSpin(FontIcon? icon, double angle = 15.0, int durationMs = 150)
    {
        if (icon == null) return;

        icon.RenderTransformOrigin = new Point(0.5, 0.5);
        var rotate = EnsureRotateTransform(icon);

        var sb = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(durationMs / 2.0));

        var kf = new DoubleAnimationUsingKeyFrames();
        kf.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0.0 });
        kf.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs * 0.4)), Value = angle, EasingFunction = EaseOutCubic });
        kf.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs)), Value = 0.0, EasingFunction = EaseOutCubic });

        Storyboard.SetTarget(kf, rotate);
        Storyboard.SetTargetProperty(kf, nameof(RotateTransform.Angle));
        sb.Children.Add(kf);

        sb.Begin();
    }
    #endregion

    #region Helper Transform Methods
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
