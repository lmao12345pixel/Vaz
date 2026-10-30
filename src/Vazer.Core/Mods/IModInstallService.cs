using Vazer.Core.Abstractions;

namespace Vazer.Core.Mods;

public interface IModInstallService
{
    Task<InstallModResult> InstallFromModrinthAsync(
        string instanceId, 
        string projectSlug, 
        string? versionId = null, 
        bool resolveDependencies = true, 
        IProgress<InstallProgress>? progress = null, 
        CancellationToken ct = default);

    Task<IReadOnlyList<InstalledMod>> ListAsync(string instanceId, CancellationToken ct = default);

    Task SetEnabledAsync(string instanceId, string fileName, bool enabled, CancellationToken ct = default);

    Task UninstallAsync(string instanceId, string fileName, CancellationToken ct = default);

    Task<UpdateCheckResult> CheckUpdatesAsync(string instanceId, CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra cập nhật hàng loạt theo sha512 (POST /version_files của Modrinth),
    /// không gọi API từng mod. Chỉ áp dụng cho mod có Source="modrinth".
    /// </summary>
    Task<UpdateCheckResult> CheckUpdatesByHashAsync(
        string instanceId, int chunkSize = 100, CancellationToken ct = default);

    Task<int> UpdateAllAsync(string instanceId, IProgress<InstallProgress>? progress = null, CancellationToken ct = default);

    Task<InstallModResult> InstallPerformancePackAsync(string instanceId, IProgress<InstallProgress>? progress = null, CancellationToken ct = default);
}
