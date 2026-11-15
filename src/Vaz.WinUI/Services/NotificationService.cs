using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Windows.Media.Core;
using Windows.Media.Playback;
using Vaz.WinUI.Models;
using Vazer.Core.Models;
using Vazer.Core.Services;
using Vazer.Core.Settings;

namespace Vaz.WinUI.Services;

public sealed class NotificationService : INotificationService
{
    private const int MaxActiveStack = 5;
    private const int MaxHistoryCount = 50;
    private const int CoalesceWindowSeconds = 3;
    private readonly object _lock = new();
    private readonly NotificationHistoryStore _store;
    private readonly ISettingsService? _settingsService;

    public NotificationService(NotificationHistoryStore? store = null, ISettingsService? settingsService = null)
    {
        _store = store ?? new NotificationHistoryStore();
        _settingsService = settingsService;
    }

    private GlobalSettings Settings => _settingsService?.Current ?? new GlobalSettings();

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

    private bool ShouldShowToast(NotificationType type, NotificationPriority priority)
    {
        // Critical LUÔN hiện toast bất kể cài đặt loại thông báo (N3.3 sẽ handle DND exception)
        if (priority == NotificationPriority.Critical)
        {
            return true;
        }

        var s = Settings;
        return type switch
        {
            NotificationType.Success => s.NotificationShowSuccess,
            NotificationType.Error => s.NotificationShowError,
            NotificationType.Warning => s.NotificationShowWarning,
            NotificationType.Progress => s.NotificationShowProgress,
            _ => true
        };
    }

    public void Show(NotificationItem notification)
    {
        // Đồng bộ IsSoundEnabled theo master toggle nếu notification chưa bị tắt riêng
        notification.IsSoundEnabled = notification.IsSoundEnabled && Settings.NotificationSoundEnabled;

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
                    PlaySoundSafe(notification);
                    return;
                }

                // QUYẾT ĐỊNH (N3.2): Kiểm tra per-category toggle.
                // Nếu bị tắt, skip toast trên màn hình nhưng VẪN LƯU VÀO HISTORY để an toàn, không mất data.
                bool showToast = ShouldShowToast(notification.Type, notification.Priority);

                if (showToast)
                {
                    // Max 5 toast in active stack: remove oldest from stack
                    while (ActiveNotifications.Count >= MaxActiveStack)
                    {
                        var oldest = ActiveNotifications[0];
                        ActiveNotifications.RemoveAt(0);
                        NotificationRemoved?.Invoke(this, oldest);
                    }

                    notification.GroupKey = key;
                    ActiveNotifications.Add(notification);

                    var subs = NotificationAdded?.GetInvocationList().Length ?? 0;
                    try
                    {
                        System.IO.File.AppendAllText(
                            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vazer", "notif-debug.log"),
                            DateTime.Now.ToString("HH:mm:ss.fff") + " [SVC] Show hash=" + GetHashCode() + " title=" + notification.Title + " subs=" + subs + "\n");
                    }
                    catch { }

                    NotificationAdded?.Invoke(this, notification);
                }

                // Add to history (at index 0 for newest first) - luôn lưu để user xem lại sau
                History.Insert(0, notification);
                while (History.Count > MaxHistoryCount)
                {
                    History.RemoveAt(History.Count - 1);
                }

                UnreadCount++;
                PersistHistoryLocked();
            }

            PlaySoundSafe(notification);
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
            AutoDismissMs = 5000
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
            AutoDismissMs = 5000
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
            AutoDismissMs = priority == NotificationPriority.Critical ? 0 : 8000
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
            AutoDismissMs = priority == NotificationPriority.Low ? 5000 : 5000
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
            AutoDismissMs = 8000
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
                    PlaySoundSafe(existing);
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
            lock (_lock)
            {
                UnreadCount = 0;
                PersistHistoryLocked();
            }
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
                    PersistHistoryLocked();
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
                UnreadCount = 0;
                PersistHistoryLocked();
            }
        });
    }

    public async Task InitializeAsync()
    {
        var dto = await _store.LoadAsync().ConfigureAwait(false);
        RunOnUIThread(() =>
        {
            lock (_lock)
            {
                History.Clear();
                foreach (var item in dto.Items)
                {
                    History.Add(MapToNotificationItem(item));
                }
                UnreadCount = dto.UnreadCount;
            }
        });
    }

    private void PersistHistoryLocked()
    {
        var dto = BuildDtoFromHistory();
        _store.RequestSave(dto);
    }

    private NotificationRecordDto BuildDtoFromHistory()
    {
        var dto = new NotificationRecordDto
        {
            Version = 1,
            UnreadCount = _unreadCount,
            Items = new List<NotificationRecordItem>(History.Count)
        };

        foreach (var item in History)
        {
            dto.Items.Add(new NotificationRecordItem
            {
                Id = item.Id,
                Type = item.Type.ToString(),
                Priority = item.Priority.ToString(),
                Title = item.Title ?? string.Empty,
                Message = item.Message ?? string.Empty,
                ImageSource = item.ImageSource,
                ActionLabel1 = item.ActionLabel1,
                ActionLabel2 = item.ActionLabel2,
                GroupCount = item.GroupCount,
                GroupKey = item.GroupKey,
                IsSoundEnabled = item.IsSoundEnabled,
                CreatedAt = item.CreatedAt
            });
        }

        return dto;
    }

    private static NotificationItem MapToNotificationItem(NotificationRecordItem record)
    {
        var type = Enum.TryParse<NotificationType>(record.Type, ignoreCase: true, out var parsedType)
            ? parsedType
            : NotificationType.Info;

        var priority = Enum.TryParse<NotificationPriority>(record.Priority, ignoreCase: true, out var parsedPriority)
            ? parsedPriority
            : NotificationPriority.Normal;

        return new NotificationItem
        {
            Id = record.Id,
            Type = type,
            Priority = priority,
            Title = record.Title,
            Message = record.Message,
            ImageSource = record.ImageSource,
            ActionLabel1 = record.ActionLabel1,
            ActionLabel2 = record.ActionLabel2,
            GroupCount = record.GroupCount > 0 ? record.GroupCount : 1,
            GroupKey = record.GroupKey,
            IsSoundEnabled = record.IsSoundEnabled,
            CreatedAt = record.CreatedAt != default ? record.CreatedAt : DateTime.Now,
            // Callbacks are null after restore from disk
            ActionCallback1 = null,
            ActionCallback2 = null,
            // Transient fields reset to defaults
            ProgressValue = -1,
            IsSticky = false,
            AutoDismissMs = 5000
        };
    }

    [DllImport("user32.dll")]
    private static extern bool MessageBeep(uint uType);

    private void PlaySoundSafe(NotificationItem notification)
    {
        if (notification.Type == NotificationType.Progress) return;

        // Critical LUÔN phát âm thanh bất kể cài đặt (N3.3 sẽ handle DND exception chi tiết)
        if (notification.Priority != NotificationPriority.Critical)
        {
            if (!Settings.NotificationSoundEnabled) return;
            if (!notification.IsSoundEnabled) return;
            if (!ShouldShowToast(notification.Type, notification.Priority)) return;
        }

        Task.Run(() =>
        {
            try
            {
                string soundEvent = (notification.Type == NotificationType.Error || notification.Type == NotificationType.Warning)
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
                    switch (notification.Type)
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

