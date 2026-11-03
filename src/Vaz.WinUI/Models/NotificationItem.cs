using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Vaz.WinUI.Models;

public class NotificationItem : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private string _message = string.Empty;
    private string? _imageSource;
    private string? _actionLabel1;
    private Func<Task>? _actionCallback1;
    private string? _actionLabel2;
    private Func<Task>? _actionCallback2;
    private double _progressValue = -1.0;
    private int _groupCount = 1;
    private bool _isSticky;
    private int _autoDismissMs = 10000;
    private NotificationType _type = NotificationType.Info;
    private NotificationPriority _priority = NotificationPriority.Normal;

    public Guid Id { get; set; } = Guid.NewGuid();

    public NotificationType Type
    {
        get => _type;
        set
        {
            if (_type != value)
            {
                _type = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IconGlyph));
                OnPropertyChanged(nameof(TypeColorHex));
                OnPropertyChanged(nameof(IsProgressMode));
            }
        }
    }

    public NotificationPriority Priority
    {
        get => _priority;
        set
        {
            if (_priority != value)
            {
                _priority = value;
                OnPropertyChanged();
            }
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayTitle));
            }
        }
    }

    public string Message
    {
        get => _message;
        set
        {
            if (_message != value)
            {
                _message = value;
                OnPropertyChanged();
            }
        }
    }

    public string? ImageSource
    {
        get => _imageSource;
        set
        {
            if (_imageSource != value)
            {
                _imageSource = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasImage));
            }
        }
    }

    public string? ActionLabel1
    {
        get => _actionLabel1;
        set
        {
            if (_actionLabel1 != value)
            {
                _actionLabel1 = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasAction1));
            }
        }
    }

    public Func<Task>? ActionCallback1
    {
        get => _actionCallback1;
        set
        {
            if (_actionCallback1 != value)
            {
                _actionCallback1 = value;
                OnPropertyChanged();
            }
        }
    }

    public string? ActionLabel2
    {
        get => _actionLabel2;
        set
        {
            if (_actionLabel2 != value)
            {
                _actionLabel2 = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasAction2));
            }
        }
    }

    public Func<Task>? ActionCallback2
    {
        get => _actionCallback2;
        set
        {
            if (_actionCallback2 != value)
            {
                _actionCallback2 = value;
                OnPropertyChanged();
            }
        }
    }

    public double ProgressValue
    {
        get => _progressValue;
        set
        {
            if (Math.Abs(_progressValue - value) > 0.001)
            {
                _progressValue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProgressPercentText));
                OnPropertyChanged(nameof(IsProgressMode));
            }
        }
    }

    public int GroupCount
    {
        get => _groupCount;
        set
        {
            if (_groupCount != value)
            {
                _groupCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GroupBadgeText));
                OnPropertyChanged(nameof(HasGroupBadge));
            }
        }
    }

    public string? GroupKey { get; set; }

    public bool IsSticky
    {
        get => _isSticky;
        set
        {
            if (_isSticky != value)
            {
                _isSticky = value;
                OnPropertyChanged();
            }
        }
    }

    public int AutoDismissMs
    {
        get => _autoDismissMs;
        set
        {
            if (_autoDismissMs != value)
            {
                _autoDismissMs = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsSoundEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool HasImage => !string.IsNullOrWhiteSpace(ImageSource);
    public bool HasAction1 => !string.IsNullOrWhiteSpace(ActionLabel1);
    public bool HasAction2 => !string.IsNullOrWhiteSpace(ActionLabel2);
    public bool IsProgressMode => Type == NotificationType.Progress || ProgressValue >= 0;
    public string ProgressPercentText => ProgressValue >= 0 ? $"{(int)Math.Clamp(ProgressValue, 0, 100)}%" : string.Empty;

    public string DisplayTitle => GroupCount > 1 ? $"{Title} (x{GroupCount})" : Title;
    public string GroupBadgeText => $"x{GroupCount}";
    public bool HasGroupBadge => GroupCount > 1;

    public string CreatedAtFormatted => CreatedAt.ToString("HH:mm:ss");

    public string IconGlyph => Type switch
    {
        NotificationType.Success => "\uE73E",  // CheckMark
        NotificationType.Warning => "\uE7BA",  // Warning
        NotificationType.Error   => "\uEA39",  // ErrorBadge
        NotificationType.Info    => "\uE946",  // Info
        NotificationType.Update  => "\uE895",  // Sync
        NotificationType.Progress=> "\uE896",  // Download
        _                        => "\uE946"
    };

    public string TypeColorHex => Type switch
    {
        NotificationType.Success => "#10B981", // Emerald Green
        NotificationType.Warning => "#F59E0B", // Amber
        NotificationType.Error   => "#EF4444", // Red
        NotificationType.Info    => "#3B82F6", // Blue
        NotificationType.Update  => "#8B5CF6", // Purple
        NotificationType.Progress=> "#06B6D4", // Cyan
        _                        => "#3B82F6"
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
