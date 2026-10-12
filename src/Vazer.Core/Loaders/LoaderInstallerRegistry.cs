namespace Vazer.Core.Loaders;

/// <summary>
/// Minimal registry mapping loader ids ("fabric", "neoforge", ...) to their
/// installer services. Fabric/Quilt stay inside CmlLib; NeoForge (and future
/// loaders CmlLib lacks) register here.
/// </summary>
public sealed class LoaderInstallerRegistry
{
    private readonly Dictionary<string, object> _services = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Registers an installer service under a loader id.</summary>
    public void Register<T>(string loaderType, T service) where T : notnull
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(loaderType);
        ArgumentNullException.ThrowIfNull(service);
        _services[loaderType] = service;
    }

    /// <summary>Tries to resolve an installer service for a loader id.</summary>
    public bool TryGet<T>(string loaderType, out T? service)
    {
        if (_services.TryGetValue(loaderType, out var obj) && obj is T typed)
        {
            service = typed;
            return true;
        }

        service = default;
        return false;
    }
}
