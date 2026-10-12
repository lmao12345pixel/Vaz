using Vazer.Core.Abstractions;

namespace Vazer.Core.Loaders.Optifine;

public interface IOptifineInstallerService
{
    Task<OptifineCatalog> GetCatalogAsync(string mcVersion, CancellationToken ct = default);
    Task<InstallOptifineResult> InstallFromCatalogAsync(MinecraftInstance instance, string optifineVersion, IProgress<InstallProgress>? progress = null, CancellationToken ct = default);
    Task<InstallOptifineResult> InstallFromUrlAsync(MinecraftInstance instance, string manualUrl, IProgress<InstallProgress>? progress = null, CancellationToken ct = default);
}
