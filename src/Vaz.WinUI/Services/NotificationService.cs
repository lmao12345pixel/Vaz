using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Windows.Media.Core;
using Windows.Media.Playback;
using Vaz.WinUI.Models;

namespace Vaz.WinUI.Services;

public sealed class NotificationService : INotificationService
{
    private const int MaxActiveStack = 5;
    private const int MaxHistoryCount = 50;
    private const int CoalesceWindowSeconds = 3;
    private readonly object _lock = new();

    public ObservableCollection<NotificationItem> ActiveNotifications { get; } = new();
    public ObservableCollection<NotificationItem> History { get; } = new();

    private int _unreadCount;
    public int UnreadCount
    {
        get
        {
            lock (_lock) return _unreadCount;
        }
        private set
        {
            lock (_lock) _unreadCount = value;
            UnreadCountChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<NotificationItem>? NotificationAdded;
    public event EventHandler<NotificationItem>? NotificationRemoved;
    public event EventHandler<int>? UnreadCountChanged;

    public void Show(NotificationItem notification)
    {
        RunOnUIThread(() =>
        {
            lock (_lock)
            {
                var now = DateTime.Now;
                var key = notification.GroupKey ?? $"{notification.Type}:{notification.Title}";

                // Grouping check: same Type + GroupKey and CreatedAt < 3s
                var existing = ActiveNotifications.FirstOrDefault(n =>
                    n.Type == notification.Type &&
                    (n.GroupKey == key || $"{n.Type}:{n.Title}" == key) &&
                    (now - n.CreatedAt).TotalSeconds <= CoalesceWindowSeconds);

                if (existing != null)
                {
                    existing.GroupCount++;
                    existing.CreatedAt = now;
                    if (!string.IsNullOrEmpty(notification.Message))
                    {
                        existing.Message = notification.Message;
                    }
                    PlaySoundSafe(notification.Type);
                    return;
                }

                // Max 5 toast in active stack: remove oldest from stack
                while (ActiveNotifications.Count >= MaxActiveStack)
                {
                    var oldest = ActiveNotifications[0];
                    ActiveNotifications.RemoveAt(0);
                    NotificationRemoved?.Invoke(this, oldest);
                }

                notification.GroupKey = key;
                ActiveNotifications.Add(notification);
                NotificationAdded?.Invoke(this, notification);

                // Add to history (at index 0 for newest first)
                History.Insert(0, notification);
                while (History.Count > MaxHistoryCount)
                {
                    History.RemoveAt(History.Count - 1);
                }

                UnreadCount++;
            }

            PlaySoundSafe(notification.Type);
        });
    }

    public void ShowSuccess(string title, string message, string? imageSource = null, string? actionLabel = null, Func<Task>? actionCallback = null)
    {
        Show(new NotificationItem
        {
            Type = NotificationType.Success,
            Priority = NotificationPriority.Normal,
            Title = title,
            Message = message,
            ImageSource = imageSource,
            ActionLabel1 = actionLabel,
            ActionCallback1 = actionCallback,
            AutoDismissMs = 10000
        });
    }

    public void ShowSuccess(string title, string message, string? imageSource, string? actionLabel, Action actionCallback)
    {
        ShowSuccess(title, message, imageSource, actionLabel, () =>
        {
            actionCallback();
            return Task.CompletedTask;
        });
    }

    public void ShowWarning(string title, string message, string? actionLabel = null, Func<Task>? actionCallback = null)
    {
        Show(new NotificationItem
        {
            Type = NotificationType.Warning,
            Priority = NotificationPriority.Normal,
            Title = title,
            Message = message,
            ActionLabel1 = actionLabel,
            ActionCallback1 = actionCallback,
            AutoDismissMs = 10000
        });
    }

    public void ShowWarning(string title, string message, string? actionLabel, Action actionCallback)
    {
        ShowWarning(title, message, actionLabel, () =>
        {
            actionCallback();
            return Task.CompletedTask;
        });
    }

    public void ShowError(string title, string message, string? actionLabel = null, Func<Task>? actionCallback = null, NotificationPriority priority = NotificationPriority.High)
    {
        Show(new NotificationItem
        {
            Type = NotificationType.Error,
            Priority = priority,
            Title = title,
            Message = message,
            ActionLabel1 = actionLabel,
            ActionCallback1 = actionCallback,
            IsSticky = priority == NotificationPriority.Critical,
            AutoDismissMs = priority == NotificationPriority.Critical ? 0 : 15000
        });
    }

    public void ShowError(string title, string message, string? actionLabel, Action actionCallback, NotificationPriority priority = NotificationPriority.High)
    {
        ShowError(title, message, actionLabel, () =>
        {
            actionCallback();
            return Task.CompletedTask;
        });
    }

    public void ShowInfo(string title, string message, string? actionLabel = null, Func<Task>? actionCallback = null, NotificationPriority priority = NotificationPriority.Normal)
    {
        Show(new NotificationItem
        {
            Type = NotificationType.Info,
            Priority = priority,
            Title = title,
            Message = message,
            ActionLabel1 = actionLabel,
            ActionCallback1 = actionCallback,
            AutoDismissMs = priority == NotificationPriority.Low ? 5000 : 10000
        });
    }

    public void ShowInfo(string title, string message, string? actionLabel, Action actionCallback, NotificationPriority priority = NotificationPriority.Normal)
    {
        ShowInfo(title, message, actionLabel, () =>
        {
            actionCallback();
            return Task.CompletedTask;
        });
    }

    public void ShowUpdate(string title, string message, string? action1 = null, Func<Task>? cb1 = null, string? action2 = null, Func<Task>? cb2 = null)
    {
        Show(new NotificationItem
        {
            Type = NotificationType.Update,
            Priority = NotificationPriority.Normal,
            Title = title,
            Message = message,
            ActionLabel1 = action1,
            ActionCallback1 = cb1,
            ActionLabel2 = action2,
            ActionCallback2 = cb2,
            AutoDismissMs = 15000
        });
    }

    public void ShowUpdate(string title, string message, string? action1, Action cb1, string? action2 = null, Action? cb2 = null)
    {
        ShowUpdate(
            title,
            message,
            action1,
            cb1 != null ? () => { cb1(); return Task.CompletedTask; } : null,
            action2,
            cb2 != null ? () => { cb2(); return Task.CompletedTask; } : null);
    }

    public Guid ShowProgress(string title, string message, Guid? id = null)
    {
        var targetId = id ?? Guid.NewGuid();
        RunOnUIThread(() =>
        {
            lock (_lock)
            {
                var existing = ActiveNotifications.FirstOrDefault(n => n.Id == targetId);
                if (existing != null)
                {
                    existing.Title = title;
                    existing.Message = message;
                    existing.ProgressValue = 0;
                }
                else
                {
                    var item = new NotificationItem
                    {
                        Id = targetId,
                        Type = NotificationType.Progress,
                        Priority = NotificationPriority.High,
                        Title = title,
                        Message = message,
                        ProgressValue = 0,
                        IsSticky = true,
                        AutoDismissMs = 0
                    };
                    Show(item);
                }
            }
        });
        return targetId;
    }

    public void UpdateProgress(Guid id, double value, string? message = null)
    {
        RunOnUIThread(() =>
        {
            lock (_lock)
            {
                var existing = ActiveNotifications.FirstOrDefault(n => n.Id == id);
                if (existing != null)
                {
                    existing.ProgressValue = Math.Clamp(value, 0, 100);
                    if (!string.IsNullOrEmpty(message))
                    {
                        existing.Message = message;
                    }
                }
            }
        });
    }

    public void UpdateProgress(string id, double value, string? message = null)
    {
        if (Guid.TryParse(id, out var guid))
        {
            UpdateProgress(guid, value, message);
        }
    }

    public void CompleteProgress(Guid id, bool success, string? message = null)
    {
        RunOnUIThread(() =>
        {
            lock (_lock)
            {
                var existing = ActiveNotifications.FirstOrDefault(n => n.Id == id);
                if (existing != null)
                {
                    existing.Type = success ? NotificationType.Success : NotificationType.Error;
                    existing.ProgressValue = -1;
                    existing.IsSticky = false;
                    existing.AutoDismissMs = 5000;
                    if (!string.IsNullOrEmpty(message))
                    {
                        existing.Message = message;
                    }
                    PlaySoundSafe(existing.Type);
                }
            }
        });
    }

    public void CompleteProgress(string id, bool success, string? message = null)
    {
        if (Guid.TryParse(id, out var guid))
        {
            CompleteProgress(guid, success, message);
        }
    }

    public void Dismiss(Guid id)
    {
        RunOnUIThread(() =>
        {
            NotificationItem? item;
            lock (_lock)
            {
                item = ActiveNotifications.FirstOrDefault(n => n.Id == id);
                if (item != null)
                {
                    ActiveNotifications.Remove(item);
                }
            }
            if (item != null)
            {
                NotificationRemoved?.Invoke(this, item);
            }
        });
    }

    public void Dismiss(string id)
    {
        if (Guid.TryParse(id, out var guid))
        {
            Dismiss(guid);
        }
    }

    public void Dismiss(NotificationItem notification)
    {
        if (notification != null)
        {
            Dismiss(notification.Id);
        }
    }

    public void DismissAll()
    {
        RunOnUIThread(() =>
        {
            NotificationItem[] items;
            lock (_lock)
            {
                items = ActiveNotifications.ToArray();
                ActiveNotifications.Clear();
            }
            foreach (var item in items)
            {
                NotificationRemoved?.Invoke(this, item);
            }
        });
    }

    public void MarkAllRead()
    {
        ResetUnreadCount();
    }

    public void ResetUnreadCount()
    {
        RunOnUIThread(() =>
        {
            UnreadCount = 0;
        });
    }

    public void RemoveFromHistory(Guid id)
    {
        RunOnUIThread(() =>
        {
            lock (_lock)
            {
                var item = History.FirstOrDefault(h => h.Id == id);
                if (item != null)
                {
                    History.Remove(item);
                }
            }
        });
    }

    public void RemoveFromHistory(string id)
    {
        if (Guid.TryParse(id, out var guid))
        {
            RemoveFromHistory(guid);
        }
    }

    public void ClearHistory()
    {
        RunOnUIThread(() =>
        {
            lock (_lock)
            {
                History.Clear();
            }
            UnreadCount = 0;
        });
    }

    [DllImport("user32.dll")]
    private static extern bool MessageBeep(uint uType);

    private static void PlaySoundSafe(NotificationType type)
    {
        if (type == NotificationType.Progress) return;

        Task.Run(() =>
        {
            try
            {
                string soundEvent = (type == NotificationType.Error || type == NotificationType.Warning)
                    ? "ms-winsoundevent:Notification.Reminder"
                    : "ms-winsoundevent:Notification.Default";

                var player = new MediaPlayer();
                player.Source = MediaSource.CreateFromUri(new Uri(soundEvent));
                player.Play();

                _ = Task.Delay(2000).ContinueWith(_ =>
                {
                    try { player.Dispose(); } catch { }
                });
            }
            catch
            {
                // Fallback to Win32 MessageBeep if MediaPlayer is unavailable
                try
                {
                    switch (type)
                    {
                        case NotificationType.Error:
                            MessageBeep(0x00000010); // MB_ICONHAND
                            break;
                        case NotificationType.Warning:
                            MessageBeep(0x00000030); // MB_ICONEXCLAMATION
                            break;
                        default:
                            MessageBeep(0x00000040); // MB_ICONASTERISK
                            break;
                    }
                }
                catch { }
            }
        });
    }

    private static void RunOnUIThread(Action action)
    {
        if (App.Dispatcher == null || App.Dispatcher.HasThreadAccess)
        {
            action();
        }
        else
        {
            App.Dispatcher.TryEnqueue(DispatcherQueuePriority.Normal, () => action());
        }
    }
}
