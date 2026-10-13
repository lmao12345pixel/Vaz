using System.Net.Http;
using Vazer.Core.Abstractions;

namespace Vazer.Core.Http;

/// <summary>
/// Tùy chọn cho một lượt tải file (item 4: gom 4 chỗ download trùng lặp).
/// Chỉ lo transport + progress; hash/move/partial do caller giữ nguyên.
/// </summary>
public sealed record FileDownloadOptions
{
    /// <summary>
    /// Tên HttpClient trong factory ("" = default). Mỗi caller giữ client cũ của mình:
    /// "modrinth-download", "optifine" (AllowAutoRedirect=false), default (Adoptium).
    /// </summary>
    public string HttpClientName { get; init; } = "";

    /// <summary>
    /// Timeout áp lên client (nếu set). Mirror đúng behavior cũ của từng caller
    /// (Adoptium set 10 phút); không set thì giữ nguyên timeout của client.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Tổng bytes đã biết trước (khi server không trả Content-Length).
    /// </summary>
    public long? KnownTotalBytes { get; init; }

    /// <summary>
    /// Status text cho progress report từng chunk.
    /// </summary>
    public string StatusMessage { get; init; } = "Downloading";

    /// <summary>
    /// Report cuối khi xong (ví dụ Adoptium báo "verifying"). Null = không báo.
    /// </summary>
    public string? CompletionStatus { get; init; }

    /// <summary>
    /// Tùy biến request trước khi gửi (ví dụ OptiFine cần Referrer + User-Agent).
    /// </summary>
    public Action<HttpRequestMessage>? ConfigureRequest { get; init; }
}

/// <summary>
/// Downloader dùng chung: GET (ResponseHeadersRead) + copy stream + progress.
/// Không dispose HttpClient của factory (đúng guidance), chỉ dispose response/stream.
/// Thất bại giữa chừng thì để nguyên file dở (caller/test resume theo .partial).
/// </summary>
public interface IFileDownloader
{
    /// <summary>
    /// Tải URL ra file đích (ghi trực tiếp vào <paramref name="destinationPath"/>).
    /// </summary>
    Task DownloadFileAsync(
        string url,
        string destinationPath,
        FileDownloadOptions? options = null,
        IProgress<InstallProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Copy một stream có sẵn ra file đích (dùng cho Modrinth DownloadAsync).
    /// </summary>
    Task CopyStreamAsync(
        Stream source,
        string destinationPath,
        long? totalBytes,
        string statusMessage,
        IProgress<InstallProgress>? progress = null,
        CancellationToken ct = default);
}

/// <inheritdoc />
public sealed class FileDownloader : IFileDownloader
{
    private const int BufferSize = 81920;

    private readonly IHttpClientFactory? _httpClientFactory;

    public FileDownloader(IHttpClientFactory? httpClientFactory = null)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public async Task DownloadFileAsync(
        string url,
        string destinationPath,
        FileDownloadOptions? options = null,
        IProgress<InstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (_httpClientFactory == null)
        {
            throw new InvalidOperationException("FileDownloader cần IHttpClientFactory để tải từ URL.");
        }

        options ??= new FileDownloadOptions();
        var client = string.IsNullOrEmpty(options.HttpClientName)
            ? _httpClientFactory.CreateClient()
            : _httpClientFactory.CreateClient(options.HttpClientName);

        // Mirror behavior cũ: caller nào tự set Timeout thì vẫn set y vậy.
        if (options.Timeout.HasValue)
        {
            client.Timeout = options.Timeout.Value;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        options.ConfigureRequest?.Invoke(request);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? options.KnownTotalBytes ?? -1;
        await using var responseStream = await response.Content.ReadAsStreamAsync(ct);
        await CopyCoreAsync(responseStream, destinationPath, totalBytes, options.StatusMessage, progress, ct);

        if (options.CompletionStatus != null)
        {
            progress?.Report(new InstallProgress
            {
                TotalBytes = totalBytes,
                DownloadedBytes = totalBytes > 0 ? totalBytes : 0,
                Status = options.CompletionStatus
            });
        }
    }

    /// <inheritdoc />
    public async Task CopyStreamAsync(
        Stream source,
        string destinationPath,
        long? totalBytes,
        string statusMessage,
        IProgress<InstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        await CopyCoreAsync(source, destinationPath, totalBytes ?? -1, statusMessage, progress, ct);
    }

    private static async Task CopyCoreAsync(
        Stream source,
        string destinationPath,
        long totalBytes,
        string statusMessage,
        IProgress<InstallProgress>? progress,
        CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        var buffer = new byte[BufferSize];
        int bytesRead;
        long downloaded = 0;
        while ((bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            downloaded += bytesRead;
            progress?.Report(new InstallProgress
            {
                TotalBytes = totalBytes > 0 ? totalBytes : downloaded,
                DownloadedBytes = downloaded,
                Status = statusMessage
            });
        }
    }
}
