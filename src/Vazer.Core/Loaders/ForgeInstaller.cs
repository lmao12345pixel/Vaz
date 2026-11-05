using System.Threading;

namespace Vazer.Core.Loaders;

/// <summary>
/// Forge installer — placeholder. Chưa implement (xem Đợt 18.3).
/// Mọi thao tác cài đều throw NotSupportedException.
/// </summary>
public sealed class ForgeInstaller : ILoaderInstaller
{
    public string LoaderType => "forge";

    public bool SupportsVersion(string mcVersion) => false;

    public Task InstallAsync(
        string instancePath,
        string mcVersion,
        string? loaderVersion,
        IProgress<InstallerProgress>? progress,
        CancellationToken ct)
        => throw new NotSupportedException(
            "Forge chưa được hỗ trợ. Tính năng sẽ có trong phiên bản sau (Đợt 18.3).");
}
