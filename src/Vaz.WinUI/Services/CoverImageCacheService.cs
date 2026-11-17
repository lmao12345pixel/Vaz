using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Vaz.WinUI.Services;

/// <summary>
/// Tải + cache "cover art" của Minecraft version từ Minecraft Wiki qua MediaWiki API.
/// Cache lưu tại: %AppData%/Vazer/cache/version-covers/{versionId}.png.
/// Nếu API hoặc ảnh lỗi hoặc offline → trả về null để UI tự hiển thị gradient fallback.
/// Không bao giờ ném ngoại lệ (fail-safe).
/// </summary>
public sealed class CoverImageCacheService
{
    private const string WikiApiEndpoint = "https://minecraft.wiki/api.php?action=query&titles={0}&prop=pageimages&format=json&pithumbsize=400";
    private const string UserAgent = "VazLauncher/1.0 (contact: info@vazlauncher.local)";

    private readonly HttpClient _httpClient;
    private readonly string _cacheDir;

    public CoverImageCacheService(IHttpClientFactory? httpClientFactory = null)
    {
        _httpClient = httpClientFactory?.CreateClient("cover-cache") ?? new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        }

        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Vazer", "cache", "version-covers");
        Directory.CreateDirectory(_cacheDir);
    }

    /// <summary>
    /// Trả về đường dẫn file PNG đã cache (hoặc mới tải về), hoặc null nếu tải thất bại.
    /// </summary>
    public async Task<string?> GetCoverPathAsync(string versionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(versionId))
        {
            return null;
        }

        var fileName = SanitizeFileName(versionId) + ".png";
        var cachePath = Path.Combine(_cacheDir, fileName);

        if (File.Exists(cachePath) && new FileInfo(cachePath).Length > 0)
        {
            return cachePath;
        }

        try
        {
            var thumbUrl = await QueryWikiThumbnailUrlAsync(versionId, ct);
            if (string.IsNullOrWhiteSpace(thumbUrl))
            {
                return null;
            }

            using var response = await _httpClient.GetAsync(thumbUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var tmpPath = cachePath + ".tmp";
            try
            {
                await using (var file = File.Create(tmpPath))
                {
                    await stream.CopyToAsync(file, ct);
                }

                File.Move(tmpPath, cachePath, true);
                return cachePath;
            }
            catch
            {
                try { File.Delete(tmpPath); } catch { /* ignore */ }
                return null;
            }
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> QueryWikiThumbnailUrlAsync(string versionId, CancellationToken ct)
    {
        // Thử tìm theo "Java_Edition_{versionId}" trước, nếu không được thử "{versionId}"
        var titles = new[] { $"Java_Edition_{versionId}", versionId };

        foreach (var title in titles)
        {
            try
            {
                var apiUrl = string.Format(WikiApiEndpoint, Uri.EscapeDataString(title));
                using var req = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseContentRead, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    continue;
                }

                var json = await resp.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("query", out var query) &&
                    query.TryGetProperty("pages", out var pages))
                {
                    foreach (var page in pages.EnumerateObject())
                    {
                        if (page.Value.TryGetProperty("thumbnail", out var thumb) &&
                            thumb.TryGetProperty("source", out var src))
                        {
                            var url = src.GetString();
                            if (!string.IsNullOrWhiteSpace(url))
                            {
                                return url;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Thử tiếp title kế tiếp hoặc trả về null
            }
        }

        return null;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            sb.Append(invalid.Contains(c) ? '_' : c);
        }
        return sb.ToString();
    }
}
