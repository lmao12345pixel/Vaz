using Vazer.Core.Abstractions;

namespace Vaz.WinUI.Services;

/// <summary>
/// Singleton that tracks which Minecraft instance is currently active
/// across pages (Mod Browser filters, Instance Detail target, etc.).
/// </summary>
public sealed class ActiveInstanceService
{
    private readonly IInstanceManager _instances;

    public ActiveInstanceService(IInstanceManager instances)
    {
        _instances = instances;
    }

    /// <summary>ID của instance đang active (null = chưa chọn).</summary>
    public string? CurrentInstanceId { get; set; }

    /// <summary>Fire khi instance active đổi.</summary>
    public event EventHandler? CurrentChanged;

    /// <summary>Lấy instance hiện tại (null nếu chưa chọn hoặc không tìm thấy).</summary>
    public async Task<MinecraftInstance?> GetCurrentAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(CurrentInstanceId))
        {
            return null;
        }
        return await _instances.GetAsync(CurrentInstanceId, ct);
    }

    /// <summary>Set instance active và báo cho các page đang nghe.</summary>
    public void SetCurrent(string? instanceId)
    {
        if (CurrentInstanceId == instanceId)
        {
            return;
        }
        CurrentInstanceId = instanceId;
        CurrentChanged?.Invoke(this, EventArgs.Empty);
    }
}
