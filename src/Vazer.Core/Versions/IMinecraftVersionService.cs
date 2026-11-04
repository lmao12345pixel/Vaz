using System.Threading;

namespace Vazer.Core.Versions;

/// <summary>
/// Service fetch và cache version manifest từ Mojang và loader metadata.
/// </summary>
public interface IMinecraftVersionService
{
    /// <summary>Lấy danh sách Vanilla versions từ cache hoặc Mojang.</summary>
    Task<IReadOnlyList<MinecraftVersionInfo>> GetVanillaVersionsAsync(CancellationToken ct = default);

    /// <summary>Lấy danh sách loader versions (Fabric/Quilt) cho MC version cụ thể.</summary>
    Task<IReadOnlyList<LoaderVersionInfo>> GetLoaderVersionsAsync(string loader, string mcVersion, CancellationToken ct = default);

    /// <summary>Xóa toàn bộ cache versions.</summary>
    Task<bool> ClearCacheAsync(CancellationToken ct = default);

    /// <summary>Lấy thời gian cache cuối cùng (UTC).</summary>
    Task<DateTimeOffset?> GetCacheTimestampAsync(string cacheFile, CancellationToken ct = default);
}
