using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Vaz.WinUI.Services;

/// <summary>
/// Tải + cache "cover art" của Minecraft version vào %AppData%/Vazer/cache/version-covers/{versionId}.png.
/// Endpoint: https://minecraft.wiki/images/{version}_cover.png (nếu fail/hết mạng → trả về null,
/// UI tự hiển thị gradient fallback — không crash).
/// </summary>
public sealed class CoverImageCacheService
{
    private static readonly string CoverUrlTemplate = "https://minecraft.wiki/images/{0}_cover.png";

    private readonly HttpClient _httpClient;
    private readonly string _cacheDir;

    public CoverImageCacheService(IHttpClientFactory? httpClientFactory = null)
    {
        _httpClient = httpClientFactory?.CreateClient("cover-cache") ?? new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Vazer", "cache", "version-covers");
        Directory.CreateDirectory(_cacheDir);
    }

    /// <summary>
    /// Trả về đường dẫn file PNG đã cache (hoặc mới tải), hoặc null nếu tải thất bại.
    /// Không bao giờ throw.
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
            var url = string.Format(CoverUrlTemplate, Uri.EscapeDataString(versionId));
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
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
            }
            catch
            {
                try { File.Delete(tmpPath); } catch { /* ignore */ }
                return null;
            }

            return cachePath;
        }
        catch
        {
            return null;
        }
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
