using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Vaz.WinUI.Models;
using Windows.UI;

namespace Vaz.WinUI.Views.Controls;

public sealed partial class NotificationToast : UserControl
{
    private static readonly CubicEase EaseOutCubic = new() { EasingMode = EasingMode.EaseOut };
    private static readonly CubicEase EaseInCubic = new() { EasingMode = EasingMode.EaseIn };

    public NotificationItem? Notification { get; private set; }
    public event Action<NotificationToast, NotificationItem>? DismissRequested;

    private Storyboard? _progressStoryboard;
    private DispatcherTimer? _dismissTimer;
    private DateTime _dismissStartTime;
    private int _remainingMs;
    private bool _isHovered;
    private bool _isDismissing;

    public NotificationToast()
    {
        InitializeComponent();
        Unloaded += (s, e) => CleanUp();
    }

    public NotificationToast(NotificationItem model) : this()
    {
        Initialize(model);
    }

    public void Initialize(NotificationItem model)
    {
        if (Notification != null)
        {
            Notification.PropertyChanged -= OnNotificationPropertyChanged;
        }

        Notification = model;
        Notification.PropertyChanged += OnNotificationPropertyChanged;

        TitleTextBlock.Text = model.DisplayTitle;
        MessageTextBlock.Text = model.Message;

        UpdateGroupBadge();
        UpdateRichMedia();

        var color = ParseColor(model.TypeColorHex);
        var brush = new SolidColorBrush(color);

        TypeColorBar.Background = brush;
        TypeIcon.Glyph = model.IconGlyph;
        TypeIcon.Foreground = brush;
        ProgressBarFill.Fill = brush;

        // Action 1
        if (model.HasAction1)
        {
            ActionButton1.Content = model.ActionLabel1;
            ActionButton1.Foreground = brush;
            ActionButton1.Visibility = Visibility.Visible;
        }
        else
        {
            ActionButton1.Visibility = Visibility.Collapsed;
        }

        // Action 2
        if (model.HasAction2)
        {
            ActionButton2.Content = model.ActionLabel2;
            ActionButton2.Visibility = Visibility.Visible;
        }
        else
        {
            ActionButton2.Visibility = Visibility.Collapsed;
        }

        // Accessibility LiveSetting
        var liveSetting = model.Priority >= NotificationPriority.High
            ? AutomationLiveSetting.Assertive
            : AutomationLiveSetting.Polite;
        AutomationProperties.SetLiveSetting(this, liveSetting);

        _remainingMs = model.AutoDismissMs;

        // Progress bar mode check
        if (model.IsProgressMode)
        {
            ProgressBarFill.Visibility = Visibility.Visible;
            ProgressScale.ScaleX = Math.Clamp(model.ProgressValue / 100.0, 0, 1.0);
        }
    }

    private void OnNotificationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (Notification == null) return;

        DispatcherQueue?.TryEnqueue(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(NotificationItem.ProgressValue):
                    if (Notification.IsProgressMode)
                    {
                        ProgressBarFill.Visibility = Visibility.Visible;
                        ProgressScale.ScaleX = Math.Clamp(Notification.ProgressValue / 100.0, 0, 1.0);
                    }
                    break;
                case nameof(NotificationItem.Message):
                    MessageTextBlock.Text = Notification.Message;
                    break;
                case nameof(NotificationItem.GroupCount):
                    UpdateGroupBadge();
                    break;
                case nameof(NotificationItem.ImageSource):
                    UpdateRichMedia();
                    break;
            }
        });
    }

    private void UpdateGroupBadge()
    {
        if (Notification != null && Notification.HasGroupBadge)
        {
            GroupBadgeTextBlock.Text = Notification.GroupBadgeText;
            GroupBadgeBorder.Visibility = Visibility.Visible;
        }
        else
        {
            GroupBadgeBorder.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateRichMedia()
    {
        if (Notification != null && Notification.HasImage)
        {
            try
            {
                RichMediaImage.Source = new BitmapImage(new Uri(Notification.ImageSource!));
                RichMediaContainer.Visibility = Visibility.Visible;
            }
            catch
            {
                RichMediaContainer.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            RichMediaContainer.Visibility = Visibility.Collapsed;
        }
    }

    public void AnimateIn()
    {
        Opacity = 0.0;
        ToastTranslate.X = 450.0;

        var sb = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(350));

        var animX = new DoubleAnimation
        {
            From = 450.0,
            To = 0.0,
            Duration = duration,
            EasingFunction = EaseOutCubic
        };
        Storyboard.SetTarget(animX, ToastTranslate);
        Storyboard.SetTargetProperty(animX, nameof(TranslateTransform.X));
        sb.Children.Add(animX);

        var animOpacity = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = duration,
            EasingFunction = EaseOutCubic
        };
        Storyboard.SetTarget(animOpacity, this);
        Storyboard.SetTargetProperty(animOpacity, nameof(UIElement.Opacity));
        sb.Children.Add(animOpacity);

        sb.Begin();

        if (Notification != null && !Notification.IsProgressMode)
        {
            StartAutoDismissCountdown();
        }
    }

    private void StartAutoDismissCountdown()
    {
        if (Notification == null || Notification.AutoDismissMs <= 0 || Notification.IsSticky)
        {
            // Sticky or manual-only
            if (Notification?.IsProgressMode != true)
            {
                ProgressBarFill.Visibility = Visibility.Collapsed;
            }
            return;
        }

        ProgressBarFill.Visibility = Visibility.Visible;
        _remainingMs = Notification.AutoDismissMs;
        _dismissStartTime = DateTime.UtcNow;

        _progressStoryboard = new Storyboard();
        var progressAnim = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(_remainingMs))
        };
        Storyboard.SetTarget(progressAnim, ProgressScale);
        Storyboard.SetTargetProperty(progressAnim, nameof(ScaleTransform.ScaleX));
        _progressStoryboard.Children.Add(progressAnim);
        _progressStoryboard.Begin();

        if (_isHovered)
        {
            _progressStoryboard.Pause();
            return;
        }

        _dismissTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(_remainingMs)
        };
        _dismissTimer.Tick += async (s, e) =>
        {
            _dismissTimer?.Stop();
            await RequestDismissAsync();
        };
        _dismissTimer.Start();
    }

    public async Task RequestDismissAsync()
    {
        if (_isDismissing) return;
        _isDismissing = true;

        CleanUp();

        await AnimateSlideOutAsync();
        DismissRequested?.Invoke(this, Notification ?? new NotificationItem());
    }

    public async Task AnimateSlideOutAsync()
    {
        var tcs = new TaskCompletionSource<bool>();
        var sb = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(250));

        var animX = new DoubleAnimation
        {
            To = 450.0,
            Duration = duration,
            EasingFunction = EaseInCubic
        };
        Storyboard.SetTarget(animX, ToastTranslate);
        Storyboard.SetTargetProperty(animX, nameof(TranslateTransform.X));
        sb.Children.Add(animX);

        var animOpacity = new DoubleAnimation
        {
            To = 0.0,
            Duration = duration,
            EasingFunction = EaseInCubic
        };
        Storyboard.SetTarget(animOpacity, this);
        Storyboard.SetTargetProperty(animOpacity, nameof(UIElement.Opacity));
        sb.Children.Add(animOpacity);

        sb.Completed += (s, e) => tcs.TrySetResult(true);
        sb.Begin();

        await tcs.Task;
    }

    private void ToastCard_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isHovered = true;
        AnimateScale(1.02);
        AnimateCloseButtonOpacity(1.0);

        // Pause countdown on hover
        if (_dismissTimer != null && _dismissTimer.IsEnabled)
        {
            _dismissTimer.Stop();
            _progressStoryboard?.Pause();
            var elapsedMs = (int)(DateTime.UtcNow - _dismissStartTime).TotalMilliseconds;
            _remainingMs = Math.Max(500, _remainingMs - elapsedMs);
        }
    }

    private void ToastCard_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isHovered = false;
        AnimateScale(1.0);
        AnimateCloseButtonOpacity(0.0);

        // Resume countdown on exit
        if (!_isDismissing && Notification != null && Notification.AutoDismissMs > 0 && !Notification.IsSticky && _remainingMs > 0)
        {
            _dismissStartTime = DateTime.UtcNow;
            _progressStoryboard?.Resume();

            _dismissTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_remainingMs)
            };
            _dismissTimer.Tick += async (s, args) =>
            {
                _dismissTimer?.Stop();
                await RequestDismissAsync();
            };
            _dismissTimer.Start();
        }
    }

    private async void ToastCard_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Ignore if clicked on CloseButton or ActionButtons
        if (e.OriginalSource is DependencyObject source)
        {
            var cur = source;
            while (cur != null)
            {
                if (cur == CloseButton || cur == ActionButton1 || cur == ActionButton2) return;
                cur = VisualTreeHelper.GetParent(cur);
            }
        }

        // Primary action callback on card tap
        if (Notification?.ActionCallback1 != null)
        {
            await Notification.ActionCallback1.Invoke();
            await RequestDismissAsync();
        }
    }

    private async void ActionButton1_Click(object sender, RoutedEventArgs e)
    {
        if (Notification?.ActionCallback1 != null)
        {
            await Notification.ActionCallback1.Invoke();
            await RequestDismissAsync();
        }
    }

    private async void ActionButton2_Click(object sender, RoutedEventArgs e)
    {
        if (Notification?.ActionCallback2 != null)
        {
            await Notification.ActionCallback2.Invoke();
            await RequestDismissAsync();
        }
    }

    private async void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        await RequestDismissAsync();
    }

    private void AnimateScale(double target)
    {
        var sb = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(200));

        var animX = new DoubleAnimation { To = target, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animX, ToastScale);
        Storyboard.SetTargetProperty(animX, nameof(ScaleTransform.ScaleX));
        sb.Children.Add(animX);

        var animY = new DoubleAnimation { To = target, Duration = duration, EasingFunction = EaseOutCubic };
        Storyboard.SetTarget(animY, ToastScale);
        Storyboard.SetTargetProperty(animY, nameof(ScaleTransform.ScaleY));
        sb.Children.Add(animY);

        sb.Begin();
    }

    private void AnimateCloseButtonOpacity(double target)
    {
        var sb = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(150));

        var anim = new DoubleAnimation
        {
            To = target,
            Duration = duration,
            EasingFunction = EaseOutCubic
        };
        Storyboard.SetTarget(anim, CloseButton);
        Storyboard.SetTargetProperty(anim, nameof(UIElement.Opacity));
        sb.Children.Add(anim);

        sb.Begin();
    }

    private void CleanUp()
    {
        _dismissTimer?.Stop();
        _dismissTimer = null;
        _progressStoryboard?.Stop();
        _progressStoryboard = null;

        if (Notification != null)
        {
            Notification.PropertyChanged -= OnNotificationPropertyChanged;
        }
    }

    private static Color ParseColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return Color.FromArgb(255, 16, 185, 129);
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            var r = Convert.ToByte(hex[..2], 16);
            var g = Convert.ToByte(hex.Substring(2, 2), 16);
            var b = Convert.ToByte(hex.Substring(4, 2), 16);
            return Color.FromArgb(255, r, g, b);
        }
        return Color.FromArgb(255, 16, 185, 129);
    }
}
