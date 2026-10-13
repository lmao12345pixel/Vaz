using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vazer.Core.Abstractions;
using Vazer.Core.Http;

namespace Vazer.Core.Java;

/// <summary>
/// Production <see cref="IAdoptiumClient"/> over <see cref="IHttpClientFactory"/>.
/// Logic moved verbatim from <see cref="JavaResolver"/> (behavior unchanged).
/// </summary>
public sealed class AdoptiumClient : IAdoptiumClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AdoptiumClient> _logger;
    private readonly MinecraftOptions _options;
    private readonly IFileDownloader _fileDownloader;

    public AdoptiumClient(
        IHttpClientFactory httpClientFactory,
        ILogger<AdoptiumClient> logger,
        IOptions<MinecraftOptions> options,
        IFileDownloader? fileDownloader = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
        _fileDownloader = fileDownloader ?? new FileDownloader(httpClientFactory);
    }

    /// <inheritdoc />
    public async Task<AdoptiumAssetInfo> GetLatestJdkAsync(int major, CancellationToken ct = default)
    {
        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(_options.HttpTimeoutSeconds);

        var url = $"https://api.adoptium.net/v3/assets/latest/{major}/hotspot?architecture=x64&image_type=jdk&os=windows&vendor=eclipse";

        for (int attempt = 0; attempt <= _options.HttpMaxRetries; attempt++)
        {
            try
            {
                var response = await client.GetAsync(url, ct);
                var body = await response.Content.ReadAsStringAsync(ct);

                if ((int)response.StatusCode >= 500 && attempt < _options.HttpMaxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
                    continue;
                }

                response.EnsureSuccessStatusCode();

                var array = JsonNode.Parse(body)?.AsArray();
                if (array == null || array.Count == 0)
                {
                    throw new InvalidOperationException($"No assets found for Java {major}");
                }

                var asset = array[0];
                var binary = asset["binary"]?["package"];
                if (binary == null)
                {
                    throw new InvalidOperationException("Invalid Adoptium response format");
                }

                return new AdoptiumAssetInfo
                {
                    DownloadUrl = binary["link"]?.GetValue<string>() ?? throw new InvalidOperationException("Link missing"),
                    Sha256Checksum = binary["checksum"]?.GetValue<string>() ?? throw new InvalidOperationException("Checksum missing"),
                    SizeBytes = binary["size"]?.GetValue<long>() ?? 0
                };
            }
            catch (HttpRequestException) when (attempt < _options.HttpMaxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }
        }

        throw new HttpRequestException($"Failed to query Adoptium API after {_options.HttpMaxRetries + 1} attempts");
    }

    /// <inheritdoc />
    public async Task DownloadFileAsync(string url, string destination, IProgress<InstallProgress>? progress, CancellationToken ct = default)
    {
        await _fileDownloader.DownloadFileAsync(
            url,
            destination,
            new FileDownloadOptions
            {
                Timeout = TimeSpan.FromMinutes(10),
                StatusMessage = "downloading",
                CompletionStatus = "verifying"
            },
            progress,
            ct);
    }
}
