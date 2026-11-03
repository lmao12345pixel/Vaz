using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Vaz.WinUI.Models;
using Vaz.WinUI.Services;

namespace Vaz.WinUI.Views.Controls;

public sealed partial class NotificationCenterPanel : UserControl
{
    private INotificationService? _service;
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    public event EventHandler? Closed;

    public NotificationCenterPanel()
    {
        InitializeComponent();
    }

    public void Initialize(INotificationService service)
    {
        _service = service;
        HistoryItemsControl.ItemsSource = _service.History;
        _service.History.CollectionChanged += OnHistoryCollectionChanged;
        UpdateEmptyState();
    }

    private void OnHistoryCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateEmptyState();
    }

    private void UpdateEmptyState()
    {
        int count = _service?.History?.Count ?? 0;
        EmptyStatePanel.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ClearAllButton.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public async Task OpenAsync()
    {
        if (_isOpen) return;
        _isOpen = true;
        _service?.MarkAllRead();
        UpdateEmptyState();

        this.Visibility = Visibility.Visible;

        var sb = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(250));
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        // Backdrop fade in
        var animBackdrop = new DoubleAnimation { From = 0.0, To = 1.0, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(animBackdrop, Backdrop);
        Storyboard.SetTargetProperty(animBackdrop, nameof(UIElement.Opacity));
        sb.Children.Add(animBackdrop);

        // SidePanel slide in
        var animSlide = new DoubleAnimation { From = 400.0, To = 0.0, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(animSlide, PanelTransform);
        Storyboard.SetTargetProperty(animSlide, nameof(TranslateTransform.X));
        sb.Children.Add(animSlide);

        var tcs = new TaskCompletionSource<bool>();
        sb.Completed += (_, _) => tcs.TrySetResult(true);
        sb.Begin();
        await tcs.Task;
    }

    public async Task CloseAsync()
    {
        if (!_isOpen) return;
        _isOpen = false;

        var sb = new Storyboard();
        var duration = new Duration(TimeSpan.FromMilliseconds(200));
        var ease = new CubicEase { EasingMode = EasingMode.EaseIn };

        // Backdrop fade out
        var animBackdrop = new DoubleAnimation { From = 1.0, To = 0.0, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(animBackdrop, Backdrop);
        Storyboard.SetTargetProperty(animBackdrop, nameof(UIElement.Opacity));
        sb.Children.Add(animBackdrop);

        // SidePanel slide out
        var animSlide = new DoubleAnimation { From = 0.0, To = 400.0, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(animSlide, PanelTransform);
        Storyboard.SetTargetProperty(animSlide, nameof(TranslateTransform.X));
        sb.Children.Add(animSlide);

        var tcs = new TaskCompletionSource<bool>();
        sb.Completed += (_, _) => tcs.TrySetResult(true);
        sb.Begin();
        await tcs.Task;

        this.Visibility = Visibility.Collapsed;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private async void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        await CloseAsync();
    }

    private async void Backdrop_Tapped(object sender, TappedRoutedEventArgs e)
    {
        await CloseAsync();
    }

    private void ClearAllButton_Click(object sender, RoutedEventArgs e)
    {
        _service?.ClearHistory();
    }

    private void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            if (btn.Tag is Guid id)
            {
                _service?.RemoveFromHistory(id);
            }
            else if (btn.Tag is string idStr && Guid.TryParse(idStr, out var parsedId))
            {
                _service?.RemoveFromHistory(parsedId);
            }
        }
    }

    private async void ItemAction_Click(object sender, RoutedEventArgs e)
    {
        NotificationItem? model = null;
        if (sender is FrameworkElement elem)
        {
            model = elem.DataContext as NotificationItem;
            if (model == null && elem.Tag != null && _service != null)
            {
                Guid targetId = elem.Tag switch
                {
                    Guid g => g,
                    string s when Guid.TryParse(s, out var parsed) => parsed,
                    _ => Guid.Empty
                };

                if (targetId != Guid.Empty)
                {
                    model = System.Linq.Enumerable.FirstOrDefault(_service.History, m => m.Id == targetId);
                }
            }
        }

        if (model != null)
        {
            if (model.ActionCallback1 != null)
            {
                await model.ActionCallback1();
            }
            await CloseAsync();
        }
    }
}
