using System.Net.Http;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Vazer.Core.Versions;

/// <summary>
/// Service fetch và cache version manifest từ Mojang và loader metadata (Fabric/Quilt).
/// Caches: %AppData%/Vazer/cache/versions-manifest.json, loader-versions/{loader}/{mcVersion}.json (TTL 24h).
/// </summary>
public sealed class MinecraftVersionService : IMinecraftVersionService
{
    private readonly ILogger<MinecraftVersionService> _logger;
    private readonly HttpClient _httpClient;
    private readonly MinecraftOptions _options;
    private readonly string _cacheRoot;

    private const int CacheTtlHours = 24;

    public string VanillaManifestCachePath => Path.Combine(_cacheRoot, "versions-manifest.json");
    public string GetLoaderCachePath(string loader, string mcVersion) =>
        Path.Combine(_cacheRoot, "loader-versions", loader.ToLowerInvariant(), $"{mcVersion}.json");

    public MinecraftVersionService(
        IHttpClientFactory httpClientFactory,
        ILogger<MinecraftVersionService> logger,
        IOptions<MinecraftOptions> options)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _options = options.Value;
        _cacheRoot = Path.Combine(_options.RootPath, "cache");
        Directory.CreateDirectory(_cacheRoot);
    }

    public async Task<IReadOnlyList<MinecraftVersionInfo>> GetVanillaVersionsAsync(CancellationToken ct = default)
    {
        var cached = await GetCachedVanillaVersionsAsync(ct);
        if (cached.HasValue)
        {
            var versions = cached.Value.Item2.ToList();
            SetInstalledStatus(versions);
            _logger.LogInformation("Using cached Vanilla versions manifest ({Count} versions)", versions.Count);
            return versions.AsReadOnly();
        }

        _logger.LogInformation("Fetching Vanilla version manifest from Mojang...");
        var manifest = await FetchVanillaManifestAsync(ct);
        var versions2 = manifest.Versions.ToList();
        SetInstalledStatus(versions2);
        await SaveCacheAsync(VanillaManifestCachePath, manifest, ct);
        _logger.LogInformation("Cached Vanilla version manifest ({Count} versions)", versions2.Count);

        return versions2.AsReadOnly();
    }

    private void SetInstalledStatus(List<MinecraftVersionInfo> versions)
    {
        var versionsDir = Path.Combine(_options.RootPath, "versions");
        foreach (var v in versions)
        {
            v.IsInstalled = Directory.Exists(Path.Combine(versionsDir, v.Id));
        }
    }

    public async Task<IReadOnlyList<LoaderVersionInfo>> GetLoaderVersionsAsync(string loader, string mcVersion, CancellationToken ct = default)
    {
        var cachePath = GetLoaderCachePath(loader, mcVersion);
        var cached = await GetCachedLoaderVersionsAsync(loader, mcVersion, ct);
        if (cached.HasValue)
        {
            _logger.LogInformation("Using cached {Loader} versions for MC {Mc} ({Count} versions)", loader, mcVersion, cached.Value.Item2.Count);
            return cached.Value.Item2;
        }

        _logger.LogInformation("Fetching {Loader} versions for MC {Mc}...", loader, mcVersion);
        IReadOnlyList<LoaderVersionInfo>? versions = null;

        try
        {
            if (string.Equals(loader, "fabric", StringComparison.OrdinalIgnoreCase))
                versions = await FetchFabricVersionsAsync(mcVersion, ct);
            else if (string.Equals(loader, "quilt", StringComparison.OrdinalIgnoreCase))
                versions = await FetchQuiltVersionsAsync(mcVersion, ct);
            else
                _logger.LogWarning("Loader {Loader} không hỗ trợ fetch metadata", loader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch {Loader} versions for MC {Mc}", loader, mcVersion);
        }

        if (versions?.Count > 0)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
            await SaveCacheAsync(cachePath, versions, ct);
            _logger.LogInformation("Cached {Loader} versions for MC {Mc} ({Count} versions)", loader, mcVersion, versions.Count);
            return versions;
        }

        return Array.Empty<LoaderVersionInfo>();
    }

    public async Task<bool> ClearCacheAsync(CancellationToken ct = default)
    {
        if (Directory.Exists(_cacheRoot))
        {
            try
            {
                Directory.Delete(_cacheRoot, recursive: true);
                Directory.CreateDirectory(_cacheRoot);
                _logger.LogInformation("Cleared version cache folder: {Path}", _cacheRoot);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear cache folder {Path}", _cacheRoot);
                return false;
            }
        }
        return true;
    }

    public Task<DateTimeOffset?> GetCacheTimestampAsync(string cacheFile, CancellationToken ct = default)
    {
        var path = Path.Combine(_cacheRoot, cacheFile);
        return File.Exists(path) ? Task.FromResult((DateTimeOffset?)File.GetLastWriteTimeUtc(path)) : Task.FromResult<DateTimeOffset?>(null);
    }

    private async Task<(DateTimeOffset, IReadOnlyList<MinecraftVersionInfo>)?> GetCachedVanillaVersionsAsync(CancellationToken ct)
    {
        if (!File.Exists(VanillaManifestCachePath))
            return null;

        var lastWrite = File.GetLastWriteTimeUtc(VanillaManifestCachePath);
        if (DateTime.UtcNow - lastWrite > TimeSpan.FromHours(CacheTtlHours))
        {
            _logger.LogInformation("Vanilla manifest cache expired ({Age}h)", (DateTime.UtcNow - lastWrite).TotalHours);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(VanillaManifestCachePath, ct);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var manifest = JsonSerializer.Deserialize<VanillaVersionManifest>(json, options);
            return (lastWrite, manifest?.Versions?.Count > 0 ? (IReadOnlyList<MinecraftVersionInfo>)manifest.Versions : Array.Empty<MinecraftVersionInfo>());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse cached Vanilla manifest");
            return null;
        }
    }

    private async Task<(DateTimeOffset, IReadOnlyList<LoaderVersionInfo>)?> GetCachedLoaderVersionsAsync(string loader, string mcVersion, CancellationToken ct)
    {
        var path = GetLoaderCachePath(loader, mcVersion);
        if (!File.Exists(path))
            return null;

        var lastWrite = File.GetLastWriteTimeUtc(path);
        if (DateTime.UtcNow - lastWrite > TimeSpan.FromHours(CacheTtlHours))
        {
            _logger.LogInformation("Loader cache expired for {Loader}/{Mc} ({Age}h)", loader, mcVersion, (DateTime.UtcNow - lastWrite).TotalHours);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(path, ct);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var versions = JsonSerializer.Deserialize<List<LoaderVersionInfo>>(json, options);
            return (lastWrite, versions?.Count > 0 ? (IReadOnlyList<LoaderVersionInfo>)versions : Array.Empty<LoaderVersionInfo>());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse cached loader versions: {Path}", path);
            return null;
        }
    }

    private async Task<VanillaVersionManifest> FetchVanillaManifestAsync(CancellationToken ct)
    {
        var url = "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json";
        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var manifest = JsonSerializer.Deserialize<VanillaVersionManifest>(json, options)
            ?? throw new InvalidOperationException("Failed to parse Vanilla version manifest");

        return manifest;
    }

    private async Task<IReadOnlyList<LoaderVersionInfo>> FetchFabricVersionsAsync(string mcVersion, CancellationToken ct)
    {
        var url = $"https://meta.fabricmc.net/v2/versions/loader/{mcVersion}";
        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var loaderVersions = JsonSerializer.Deserialize<List<LoaderVersionInfo>>(json, options);

        return loaderVersions?.Count > 0 ? loaderVersions : Array.Empty<LoaderVersionInfo>();
    }

    private async Task<IReadOnlyList<LoaderVersionInfo>> FetchQuiltVersionsAsync(string mcVersion, CancellationToken ct)
    {
        var url = $"https://meta.quiltmc.org/v3/versions/loader/{mcVersion}";
        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var loaderVersions = JsonSerializer.Deserialize<List<LoaderVersionInfo>>(json, options);

        return loaderVersions?.Count > 0 ? loaderVersions : Array.Empty<LoaderVersionInfo>();
    }

    private async Task SaveCacheAsync<T>(string path, T data, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false });
        await File.WriteAllTextAsync(path, json, ct);
    }

    private class VanillaVersionManifest
    {
        public List<MinecraftVersionInfo> Versions { get; set; } = new();
    }
}
