using System.Threading;

namespace Vazer.Core.Loaders;

/// <summary>
/// Strategy pattern: mỗi mod loader (Vanilla/Fabric/Quilt/NeoForge/Forge) có một
/// implementation riêng. "Facade" <see cref="Vazer.Core.Instances.IInstanceCreationService"/>
/// chọn installer theo <see cref="LoaderType"/>.
/// </summary>
public interface ILoaderInstaller
{
    /// <summary>Loại loader (case-insensitive): "vanilla" | "fabric" | "quilt" | "neoforge" | "forge".</summary>
    string LoaderType { get; }

    /// <summary>Loader có hỗ trợ Minecraft version này không (VD: NeoForge cần 1.20.2+).</summary>
    bool SupportsVersion(string mcVersion);

    /// <summary>
    /// Cài loader vào instance.
    /// </summary>
    /// <param name="instancePath">Thư mục instance (được tạo nếu chưa có).</param>
    /// <param name="mcVersion">Minecraft version (VD: "1.21.1").</param>
    /// <param name="loaderVersion">Loader version, null = lấy bản mới nhất (nếu loader hỗ trợ).</param>
    /// <param name="progress">Progress reporter (có thể null).</param>
    /// <param name="ct">Cancellation token.</param>
    Task InstallAsync(string instancePath, string mcVersion, string? loaderVersion, IProgress<InstallerProgress>? progress, CancellationToken ct);
}
