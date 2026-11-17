using Vazer.Core.Java;

namespace Vaz.WinUI.Services;

/// <summary>
/// Bridge giữa cảnh báo Java của Core và notification service của WinUI.
/// Core không tham chiếu UI nên host cài đặt <see cref="IJavaCompatibilityNotifier"/>
/// bằng adapter mỏng này.
/// </summary>
public sealed class JavaCompatibilityNotifier : IJavaCompatibilityNotifier
{
    private readonly INotificationService _notifications;

    public JavaCompatibilityNotifier(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public void ShowWarning(string title, string message) =>
        _notifications.ShowWarning(title, message);
}
