using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using CmlLib.Core;
using CmlLib.Core.ModLoaders.FabricMC;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vazer.Core.Abstractions;

namespace Vazer.Core.Instances;

/// <summary>
/// Instance manager using CmlLib.Core for vanilla/Fabric installation and
/// <see cref="Loaders.INeoForgeInstallerService"/> for NeoForge
/// (CmlLib.Core 4.0.6 is the latest release and lacks NeoForge support).
/// </summary>
public sealed class InstanceManager : IInstanceManager
{
    private readonly ILogger<InstanceManager> _logger;
    private readonly MinecraftOptions _options;
    private readonly IOptions<MinecraftOptions> _optionsAccessor;
    private readonly MinecraftLauncher _launcher;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Loaders.INeoForgeInstallerService? _neoForgeInstaller;

    public InstanceManager(
        ILogger<InstanceManager> logger,
        IOptions<MinecraftOptions> options,
        IHttpClientFactory httpClientFactory,
        Loaders.INeoForgeInstallerService? neoForgeInstaller = null)
    {
        _logger = logger;
        _options = options.Value;
        _optionsAccessor = options;
        _httpClientFactory = httpClientFactory;
        _neoForgeInstaller = neoForgeInstaller;
        _launcher = new MinecraftLauncher(new MinecraftPath(Path.Combine(_options.RootPath, "instances")));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MinecraftInstance>> ListAsync(CancellationToken ct = default)
    {
        var instancesDir = Path.Combine(_options.RootPath, "instances");
        if (!Directory.Exists(instancesDir))
        {
            return Array.Empty<MinecraftInstance>();
        }

        var results = new List<MinecraftInstance>();
        foreach (var dir in Directory.GetDirectories(instancesDir))
        {
            var metaPath = Path.Combine(dir, "instance.json");
            if (File.Exists(metaPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(metaPath, ct);
                    var instance = JsonSerializer.Deserialize<MinecraftInstance>(json, JsonOptions);
                    if (instance != null)
                    {
                        results.Add(instance);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse instance meta: {Path}", metaPath);
                }
            }
        }

        return results.OrderByDescending(i => i.LastPlayedAt ?? i.CreatedAt).ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<MinecraftInstance?> GetAsync(string id, CancellationToken ct = default)
    {
        var metaPath = Path.Combine(_options.RootPath, "instances", id, "instance.json");
        if (!File.Exists(metaPath)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(metaPath, ct);
            return JsonSerializer.Deserialize<MinecraftInstance>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read instance {Id}", id);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<MinecraftInstance> CreateVanillaAsync(string name, string minecraftVersion, CancellationToken ct = default)
    {
        var id = SanitizeId(name);
        var instancePath = Path.Combine(_options.RootPath, "instances", id);
        Directory.CreateDirectory(instancePath);

        _logger.LogInformation("Installing vanilla {Version} for instance {Id}", minecraftVersion, id);

        // Install vanilla version via CmlLib
        await _launcher.InstallAsync(minecraftVersion, ct);

        var instance = new MinecraftInstance
        {
            Id = id,
            Name = name,
            MinecraftVersion = minecraftVersion,
            LoaderType = "vanilla",
            LoaderVersion = null,
            Path = instancePath,
            IconPath = null,
            CreatedAt = DateTimeOffset.UtcNow,
            LastPlayedAt = null
        };

        await SaveMetaAsync(instance, ct);
        _logger.LogInformation("Created vanilla instance {Id}", id);
        return instance;
    }

    /// <inheritdoc />
    public async Task<MinecraftInstance> CreateFabricAsync(string name, string minecraftVersion, string? fabricVersion = null, CancellationToken ct = default)
    {
        var id = SanitizeId(name);
        var instancePath = Path.Combine(_options.RootPath, "instances", id);
        Directory.CreateDirectory(instancePath);

        _logger.LogInformation("Installing Fabric {FabricVersion} for MC {MCVersion} on instance {Id}", fabricVersion ?? "latest", minecraftVersion, id);

        // Get Fabric loaders for this version
        var httpClient = _httpClientFactory.CreateClient();
        var fabricInstaller = new FabricInstaller(httpClient);
        var loaders = await fabricInstaller.GetLoaders(minecraftVersion);

        var targetLoader = fabricVersion != null
            ? loaders.FirstOrDefault(l => l.Version == fabricVersion)
            : loaders.FirstOrDefault(l => l.Stable) ?? loaders.FirstOrDefault();

        if (targetLoader == null)
        {
            throw new InvalidOperationException($"No Fabric loader found for Minecraft {minecraftVersion}");
        }

        // Install Fabric
        var versionName = await fabricInstaller.Install(minecraftVersion, targetLoader.Version, new MinecraftPath(instancePath));

        var instance = new MinecraftInstance
        {
            Id = id,
            Name = name,
            MinecraftVersion = minecraftVersion,
            LoaderType = "fabric",
            LoaderVersion = targetLoader.Version,
            Path = instancePath,
            IconPath = null,
            CreatedAt = DateTimeOffset.UtcNow,
            LastPlayedAt = null
        };

        await SaveMetaAsync(instance, ct);
        _logger.LogInformation("Created Fabric instance {Id} with version {VersionName}", id, versionName);
        return instance;
    }

    /// <inheritdoc />
    public async Task<MinecraftInstance> CreateNeoForgeAsync(string name, string minecraftVersion, string? neoforgeVersion = null, CancellationToken ct = default)
    {
        var svc = _neoForgeInstaller ?? new Loaders.NeoForgeInstallerService(
            _httpClientFactory,
            NullLogger<Loaders.NeoForgeInstallerService>.Instance,
            _optionsAccessor);

        var id = SanitizeId(name);
        var instancePath = Path.Combine(_options.RootPath, "instances", id);
        Directory.CreateDirectory(instancePath);

        var version = neoforgeVersion ?? await svc.ResolveLatestVersionAsync(minecraftVersion, ct);
        _logger.LogInformation("Installing NeoForge {Version} for MC {MC} on instance {Id}", version, minecraftVersion, id);

        var instance = new MinecraftInstance
        {
            Id = id,
            Name = name,
            MinecraftVersion = minecraftVersion,
            LoaderType = "neoforge",
            LoaderVersion = version,
            Path = instancePath,
            IconPath = null,
            CreatedAt = DateTimeOffset.UtcNow,
            LastPlayedAt = null
        };

        await SaveMetaAsync(instance, ct);
        await svc.InstallAsync(instance, null, ct);

        _logger.LogInformation("Created NeoForge instance {Id} ({Version})", id, version);
        return instance;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string id, bool deleteFiles = false, CancellationToken ct = default)
    {
        var instance = await GetAsync(id, ct);
        if (instance == null) return;

        if (deleteFiles && Directory.Exists(instance.Path))
        {
            Directory.Delete(instance.Path, true);
        }

        var metaPath = Path.Combine(_options.RootPath, "instances", id, "instance.json");
        if (File.Exists(metaPath))
        {
            File.Delete(metaPath);
        }

        _logger.LogInformation("Deleted instance {Id}", id);
    }

    /// <inheritdoc />
    public async Task UpdateLastPlayedAsync(string id, CancellationToken ct = default)
    {
        var instance = await GetAsync(id, ct);
        if (instance == null) return;

        var updated = instance with { LastPlayedAt = DateTimeOffset.UtcNow };
        await SaveMetaAsync(updated, ct);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    private async Task SaveMetaAsync(MinecraftInstance instance, CancellationToken ct)
    {
        var metaPath = Path.Combine(_options.RootPath, "instances", instance.Id, "instance.json");
        var json = JsonSerializer.Serialize(instance, JsonOptions);
        await File.WriteAllTextAsync(metaPath, json, ct);
    }

    private static string SanitizeId(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var id = string.Join("", name.Select(c => invalid.Contains(c) ? '_' : c));
        return id.ToLowerInvariant();
    }
}

