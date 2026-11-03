using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Vaz.WinUI.Models;
using Vaz.WinUI.Services;

namespace Vaz.WinUI.Views.Controls;

public sealed partial class NotificationHost : UserControl
{
    private INotificationService? _notificationService;
    private readonly Dictionary<Guid, NotificationToast> _activeToastMap = new();

    public NotificationHost()
    {
        InitializeComponent();
        IsHitTestVisible = false;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
        {
            Clip = new RectangleGeometry
            {
                Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height)
            };
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_notificationService == null && Application.Current is App app)
        {
            _notificationService = app.Services.GetService<INotificationService>();
            if (_notificationService != null)
            {
                _notificationService.NotificationAdded += OnNotificationAdded;
                _notificationService.NotificationRemoved += OnNotificationRemoved;

                // Sync existing active notifications
                foreach (var notif in _notificationService.ActiveNotifications)
                {
                    AddToast(notif);
                }
            }
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_notificationService != null)
        {
            _notificationService.NotificationAdded -= OnNotificationAdded;
            _notificationService.NotificationRemoved -= OnNotificationRemoved;
        }
    }

    private void OnNotificationAdded(object? sender, NotificationItem notification)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            AddToast(notification);
        });
    }

    private void OnNotificationRemoved(object? sender, NotificationItem notification)
    {
        DispatcherQueue.TryEnqueue(async () =>
        {
            await RemoveToastAsync(notification.Id);
        });
    }

    private void AddToast(NotificationItem notification)
    {
        if (_activeToastMap.ContainsKey(notification.Id))
        {
            return;
        }

        // Enforce max 5 toasts visible simultaneously: remove oldest if at capacity
        while (ToastStackPanel.Children.Count >= 5)
        {
            if (ToastStackPanel.Children[0] is NotificationToast oldestToast && oldestToast.Notification != null)
            {
                var oldestId = oldestToast.Notification.Id;
                _activeToastMap.Remove(oldestId);
                ToastStackPanel.Children.RemoveAt(0);
            }
            else
            {
                ToastStackPanel.Children.RemoveAt(0);
            }
        }

        var toast = new NotificationToast(notification);
        toast.DismissRequested += async (_, item) =>
        {
            _notificationService?.Dismiss(item.Id);
            await RemoveToastAsync(item.Id);
        };

        _activeToastMap[notification.Id] = toast;
        ToastStackPanel.Children.Add(toast);
        IsHitTestVisible = true;

        toast.AnimateIn();
    }

    private async Task RemoveToastAsync(Guid id)
    {
        if (_activeToastMap.TryGetValue(id, out var toast))
        {
            _activeToastMap.Remove(id);
            await toast.AnimateSlideOutAsync();
            ToastStackPanel.Children.Remove(toast);
        }

        IsHitTestVisible = ToastStackPanel.Children.Count > 0;
    }
}
