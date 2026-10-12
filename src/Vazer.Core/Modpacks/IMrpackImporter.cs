using Vazer.Core.Abstractions;

namespace Vazer.Core.Modpacks;

public interface IMrpackImporter
{
    Task<MrpackPreview> InspectAsync(string mrpackPath, CancellationToken ct = default);
    Task ImportAsync(string mrpackPath, string instanceName, IProgress<InstallProgress>? progress = null, CancellationToken ct = default);
}
