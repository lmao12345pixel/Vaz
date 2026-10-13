using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

        return await GetCachedAsync<SearchResult>(url, ct);
    }

    public async Task<ModrinthProject> GetProjectAsync(string slugOrId, CancellationToken ct = default)
    {
        var url = $"{BaseUrl}/project/{Uri.EscapeDataString(slugOrId)}";
        return await GetCachedAsync<ModrinthProject>(url, ct);
    }

    public async Task<IReadOnlyList<ProjectVersion>> GetVersionsAsync(
        string slugOrId, string? loader = null, string? gameVersion = null, CancellationToken ct = default)
    {
        var url = $"{BaseUrl}/project/{Uri.EscapeDataString(slugOrId)}/version";
        var queryParts = new List<string>();
        if (loader != null) queryParts.Add($"loaders=[%22{Uri.EscapeDataString(loader)}%22]");
        if (gameVersion != null) queryParts.Add($"game_versions=[%22{Uri.EscapeDataString(gameVersion)}%22]");
        if (queryParts.Count > 0) url += "?" + string.Join("&", queryParts);

        return await GetCachedAsync<ProjectVersion[]>(url, ct);
    }

    public async Task<ProjectVersion?> GetVersionAsync(string versionId, CancellationToken ct = default)
    {
        var url = $"{BaseUrl}/version/{Uri.EscapeDataString(versionId)}";
        return await GetCachedAsync<ProjectVersion>(url, ct);
    }

    public async Task<Stream> DownloadAsync(string fileUrl, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(DownloadClientName);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(_options.ModrinthUserAgent);
        // Buffer vào MemoryStream để response được dispose ngay tại đây.
        // Stream mạng trả thẳng ra ngoài sẽ giữ HttpResponseMessage sống theo,
        // caller không có cách nào dispose response đó.
        using var response = await client.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        var buffered = new MemoryStream();
        await response.Content.CopyToAsync(buffered, ct);
        buffered.Position = 0;
        return buffered;
    }

    private async Task<T> GetCachedAsync<T>(string url, CancellationToken ct)
    {
        if (_cache.TryGetValue(url, out var cached) && cached.expiry > DateTimeOffset.UtcNow)
        {
            return (T)cached.value;
        }

        var result = await SendWithRetryAsync<T>(url, ct);
        _cache[url] = (DateTimeOffset.UtcNow + CacheTtl, result!);
        return result;
    }

    private async Task<T> SendWithRetryAsync<T>(string url, CancellationToken ct)
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
            var result = JsonSerializer.Deserialize<T>(json);

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
