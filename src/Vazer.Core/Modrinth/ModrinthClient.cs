using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vazer.Core.Json;
using Vazer.Core.Mods;

namespace Vazer.Core.Modrinth;

public sealed class ModrinthClient : IModrinthClient
{
    private const string BaseUrl = "https://api.modrinth.com/v2";
    private const string ApiClientName = "modrinth-api";
    private const string DownloadClientName = "modrinth-download";
    private const int MaxRetries = 3;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ModrinthClient> _logger;
    private readonly MinecraftOptions _options;
    private readonly ConcurrentDictionary<string, (DateTimeOffset expiry, object value)> _cache = new();

    public ModrinthClient(
        IHttpClientFactory httpClientFactory,
        ILogger<ModrinthClient> logger,
        IOptions<MinecraftOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken ct = default)
    {
        var facets = BuildFacets(query);
        var limit = Math.Clamp(query.Limit, 1, 100);
        var url = $"{BaseUrl}/search?query={Uri.EscapeDataString(query.Query ?? "")}" +
                  $"&limit={limit}&offset={query.Offset}" +
                  (query.SortIndex != null ? $"&index={query.SortIndex}" : "") +
                  $"&facets={Uri.EscapeDataString(facets)}";

        return await GetCachedAsync(url, VazJsonContext.Plain.SearchResult, ct);
    }

    public async Task<ModrinthProject> GetProjectAsync(string slugOrId, CancellationToken ct = default)
    {
        var url = $"{BaseUrl}/project/{Uri.EscapeDataString(slugOrId)}";
        return await GetCachedAsync(url, VazJsonContext.Plain.ModrinthProject, ct);
    }

    public async Task<IReadOnlyList<ProjectVersion>> GetVersionsAsync(
        string slugOrId, string? loader = null, string? gameVersion = null, CancellationToken ct = default)
    {
        var url = $"{BaseUrl}/project/{Uri.EscapeDataString(slugOrId)}/version";
        var queryParts = new List<string>();
        if (loader != null) queryParts.Add($"loaders=[%22{Uri.EscapeDataString(loader)}%22]");
        if (gameVersion != null) queryParts.Add($"game_versions=[%22{Uri.EscapeDataString(gameVersion)}%22]");
        if (queryParts.Count > 0) url += "?" + string.Join("&", queryParts);

        return await GetCachedAsync(url, VazJsonContext.Plain.ProjectVersionArray, ct);
    }

    public async Task<ProjectVersion?> GetVersionAsync(string versionId, CancellationToken ct = default)
    {
        var url = $"{BaseUrl}/version/{Uri.EscapeDataString(versionId)}";
        return await GetCachedAsync(url, VazJsonContext.Plain.ProjectVersion, ct);
    }

    public async Task<Stream> DownloadAsync(string fileUrl, CancellationToken ct = default)
    {
        if (!ModInstallService.IsAllowedDownloadUrl(fileUrl))
        {
            throw new InvalidOperationException($"Blocked download from untrusted host: '{fileUrl}'. Only 'https://cdn.modrinth.com' is allowed.");
        }

        var client = _httpClientFactory.CreateClient(DownloadClientName);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(_options.ModrinthUserAgent);

        var response = await client.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        try
        {
            response.EnsureSuccessStatusCode();

            // Kiểm tra redirect không trỏ ra ngoài cdn.modrinth.com
            var finalUri = response.RequestMessage?.RequestUri?.ToString();
            if (!string.IsNullOrEmpty(finalUri) && !ModInstallService.IsAllowedDownloadUrl(finalUri))
            {
                throw new InvalidOperationException($"Redirected to unauthorized host: '{finalUri}'. Only 'https://cdn.modrinth.com' is allowed.");
            }

            var netStream = await response.Content.ReadAsStreamAsync(ct);
            return new Http.NetworkResponseStream(response, netStream);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    public async Task<IReadOnlyDictionary<string, ProjectVersion>> GetVersionsFromHashesAsync(
        IEnumerable<string> sha512Hashes, CancellationToken ct = default)
    {
        var hashes = (sha512Hashes ?? Enumerable.Empty<string>())
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Select(h => h.Trim().ToLowerInvariant())
            .Distinct()
            .ToArray();
        if (hashes.Length == 0)
        {
            return new Dictionary<string, ProjectVersion>();
        }

        var client = _httpClientFactory.CreateClient(ApiClientName);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(_options.ModrinthUserAgent);

        var request = new VersionFilesRequest { Hashes = hashes, Algorithm = "sha512" };
        var json = JsonSerializer.Serialize(request, VazJsonContext.Plain.VersionFilesRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync($"{BaseUrl}/version_files", content, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        var map = JsonSerializer.Deserialize(body, VazJsonContext.Plain.DictionaryStringProjectVersion)
                  ?? new Dictionary<string, ProjectVersion>();
        return map;
    }

    public async Task<IReadOnlyDictionary<string, ProjectVersion>> GetLatestVersionsFromHashesAsync(
        IEnumerable<string> sha512Hashes,
        IEnumerable<string>? loaders = null,
        IEnumerable<string>? gameVersions = null,
        CancellationToken ct = default)
    {
        var hashes = (sha512Hashes ?? Enumerable.Empty<string>())
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Select(h => h.Trim().ToLowerInvariant())
            .Distinct()
            .ToArray();
        if (hashes.Length == 0)
        {
            return new Dictionary<string, ProjectVersion>();
        }

        var client = _httpClientFactory.CreateClient(ApiClientName);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(_options.ModrinthUserAgent);

        var request = new VersionFilesUpdateRequest
        {
            Hashes = hashes,
            Algorithm = "sha512",
            Loaders = (loaders ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToArray(),
            GameVersions = (gameVersions ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToArray()
        };
        var json = JsonSerializer.Serialize(request, VazJsonContext.Plain.VersionFilesUpdateRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync($"{BaseUrl}/version_files/update", content, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        var map = JsonSerializer.Deserialize(body, VazJsonContext.Plain.DictionaryStringProjectVersion)
                  ?? new Dictionary<string, ProjectVersion>();
        return map;
    }

    private async Task<T> GetCachedAsync<T>(string url, JsonTypeInfo<T> typeInfo, CancellationToken ct)
    {
        if (_cache.TryGetValue(url, out var cached) && cached.expiry > DateTimeOffset.UtcNow)
        {
            return (T)cached.value;
        }

        var result = await SendWithRetryAsync(url, typeInfo, ct);
        _cache[url] = (DateTimeOffset.UtcNow + CacheTtl, result!);
        return result;
    }

    private async Task<T> SendWithRetryAsync<T>(string url, JsonTypeInfo<T> typeInfo, CancellationToken ct)
    {
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            var client = _httpClientFactory.CreateClient(ApiClientName);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(_options.ModrinthUserAgent);

            using var response = await client.GetAsync(url, ct);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(1);
                _logger.LogWarning("Modrinth 429 on attempt {Attempt}, waiting {Delay}", attempt + 1, retryAfter);
                await Task.Delay(retryAfter, ct);
                continue;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize(json, typeInfo);

            if (result is SearchHit or ModrinthProject or ProjectVersion)
            {
                // Check for extension data on types that support it
                LogExtensionData(result);
            }

            return result!;
        }

        throw new HttpRequestException($"Modrinth API request failed after {MaxRetries} retries: {url}");
    }

    private void LogExtensionData(object result)
    {
        Dictionary<string, JsonElement>? ext = result switch
        {
            SearchHit h => h.ExtensionData,
            ModrinthProject p => p.ExtensionData,
            ProjectVersion v => v.ExtensionData,
            _ => null
        };
        if (ext is { Count: > 0 })
        {
            _logger.LogDebug("Unknown fields in {Type}: {Fields}", result.GetType().Name, string.Join(", ", ext.Keys));
        }
    }

    internal static string BuildFacets(SearchQuery query)
    {
        var facets = new List<string> { "[\"project_type:mod\"]" };

        if (query.Loaders is { Length: > 0 })
            foreach (var l in query.Loaders)
                facets.Add($"[\"categories:{l}\"]");

        if (query.GameVersions is { Length: > 0 })
            foreach (var v in query.GameVersions)
                facets.Add($"[\"versions:{v}\"]");

        if (query.Categories is { Length: > 0 })
            foreach (var c in query.Categories)
                facets.Add($"[\"categories:{c}\"]");

        return $"[{string.Join(",", facets)}]";
    }
}
