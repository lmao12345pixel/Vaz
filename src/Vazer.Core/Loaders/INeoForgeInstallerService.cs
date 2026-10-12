using System.Threading;
using Vazer.Core.Abstractions;

namespace Vazer.Core.Loaders;

/// <summary>
/// Installs NeoForge mod loader without CmlLib support (CmlLib.Core 4.0.6
/// is the latest release and only handles Fabric/Quilt).
/// Uses the official NeoForge Maven + headless installer JAR.
/// </summary>
public interface INeoForgeInstallerService
{
    /// <summary>
    /// Resolves the latest NeoForge build for a Minecraft version by reading
    /// maven-metadata.xml and filtering on the NeoForge version prefix
    /// (scheme: "{mcMinor}.{mcPatch}.{build}", e.g. "21.1.169" for MC 1.21.1).
    /// </summary>
    /// <param name="minecraftVersion">Minecraft version (e.g. "1.21.1").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Full NeoForge version (e.g. "21.1.169").</returns>
    /// <exception cref="NotSupportedException">Snapshots or unparsable versions.</exception>
    /// <exception cref="InvalidOperationException">No build published for this MC version.</exception>
    Task<string> ResolveLatestVersionAsync(string minecraftVersion, CancellationToken ct = default);

    /// <summary>
    /// Downloads the NeoForge installer JAR and runs it headless
    /// (<c>java -jar ... --installClient</c>) into the instance directory.
    /// </summary>
    /// <param name="instance">Instance with <c>LoaderVersion</c> set.</param>
    /// <param name="progress">Progress reporter (optional).</param>
    /// <param name="ct">Cancellation token.</param>
    Task InstallAsync(MinecraftInstance instance, IProgress<InstallProgress>? progress = null, CancellationToken ct = default);
}
