using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Vaz.WinUI.Models;

namespace Vaz.WinUI.Services;

public interface INotificationService
{
    ObservableCollection<NotificationItem> ActiveNotifications { get; }
    ObservableCollection<NotificationItem> History { get; }
    int UnreadCount { get; }

    event EventHandler<NotificationItem>? NotificationAdded;
    event EventHandler<NotificationItem>? NotificationRemoved;
    event EventHandler<int>? UnreadCountChanged;

    void Show(NotificationItem notification);

    void ShowSuccess(string title, string message, string? imageSource = null, string? actionLabel = null, Func<Task>? actionCallback = null);
    void ShowSuccess(string title, string message, string? imageSource, string? actionLabel, Action actionCallback);

    void ShowWarning(string title, string message, string? actionLabel = null, Func<Task>? actionCallback = null);
    void ShowWarning(string title, string message, string? actionLabel, Action actionCallback);

    void ShowError(string title, string message, string? actionLabel = null, Func<Task>? actionCallback = null, NotificationPriority priority = NotificationPriority.High);
    void ShowError(string title, string message, string? actionLabel, Action actionCallback, NotificationPriority priority = NotificationPriority.High);

    void ShowInfo(string title, string message, string? actionLabel = null, Func<Task>? actionCallback = null, NotificationPriority priority = NotificationPriority.Normal);
    void ShowInfo(string title, string message, string? actionLabel, Action actionCallback, NotificationPriority priority = NotificationPriority.Normal);

    void ShowUpdate(string title, string message, string? action1 = null, Func<Task>? cb1 = null, string? action2 = null, Func<Task>? cb2 = null);
    void ShowUpdate(string title, string message, string? action1, Action cb1, string? action2 = null, Action? cb2 = null);

    Guid ShowProgress(string title, string message, Guid? id = null);
    void UpdateProgress(Guid id, double value, string? message = null);
    void CompleteProgress(Guid id, bool success, string? message = null);

    // Overloads for string ID convenience
    void UpdateProgress(string id, double value, string? message = null);
    void CompleteProgress(string id, bool success, string? message = null);

    void Dismiss(Guid id);
    void Dismiss(string id);
    void Dismiss(NotificationItem notification);
    void DismissAll();

    void MarkAllRead();
    void ResetUnreadCount();
    void RemoveFromHistory(Guid id);
    void RemoveFromHistory(string id);
    void ClearHistory();
}
