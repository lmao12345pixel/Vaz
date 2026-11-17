using System;
using System.Collections.Generic;

namespace Vazer.Core.Models;

public sealed class NotificationRecordDto
{
    public int Version { get; set; } = 1;
    public int UnreadCount { get; set; }
    public List<NotificationRecordItem> Items { get; set; } = new();
}

public sealed class NotificationRecordItem
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public string Priority { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? ImageSource { get; set; }
    public string? ActionLabel1 { get; set; }
    public string? ActionLabel2 { get; set; }
    public int GroupCount { get; set; }
    public string? GroupKey { get; set; }
    public bool IsSoundEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
}
