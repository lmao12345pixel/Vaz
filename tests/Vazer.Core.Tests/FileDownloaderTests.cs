using System.Net;
using FluentAssertions;
using Vazer.Core.Abstractions;
using Vazer.Core.Http;
using Vazer.Core.Tests.TestHelpers;

namespace Vazer.Core.Tests;

/// <summary>
/// Item 5: FileDownloader (item 4) - transport + progress. Khong network that.
/// </summary>
public sealed class FileDownloaderTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly FakeHttpMessageHandler _http;

    public FileDownloaderTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "vaz-downloader-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _http = new FakeHttpMessageHandler();
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, true); } catch { }
    }

    private FileDownloader CreateDownloader() => new(new FakeHttpClientFactory(_http));

    [Fact]
    public async Task DownloadFileAsync_WritesBytesAndCompletesAt100()
    {
        var payload = new byte[200_000];
        new Random(7).NextBytes(payload);
        _http.Map(HttpMethod.Get, "/file.bin", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(payload)
        });

        var dest = Path.Combine(_tempRoot, "file.bin");
        var reports = new List<InstallProgress>();

        await CreateDownloader().DownloadFileAsync(
            "https://example.com/file.bin", dest, null, new Progress<InstallProgress>(p => reports.Add(p)));

        (await File.ReadAllBytesAsync(dest)).Should().Equal(payload);
        reports.Should().NotBeEmpty();
        reports.Last().DownloadedBytes.Should().Be(payload.Length);
        reports.Last().PercentComplete.Should().Be(100);
    }

    [Fact]
    public async Task DownloadFileAsync_KnownTotalBytes_UsedWhenNoContentLength()
    {
        var payload = new byte[10_000];
        new Random(9).NextBytes(payload);
        _http.Map(HttpMethod.Get, "/stream.bin", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            // StreamContent khong tu set Content-Length -> downloader phai dung KnownTotalBytes.
            Content = new StreamContent(new MemoryStream(payload))
        });

        var dest = Path.Combine(_tempRoot, "stream.bin");
        var reports = new List<InstallProgress>();

        await CreateDownloader().DownloadFileAsync(
            "https://example.com/stream.bin", dest,
            new FileDownloadOptions { KnownTotalBytes = payload.Length },
            new Progress<InstallProgress>(p => reports.Add(p)));

        (await File.ReadAllBytesAsync(dest)).Should().Equal(payload);
        reports.Should().NotBeEmpty();
        reports.All(p => p.TotalBytes == payload.Length).Should().BeTrue();
    }

    [Fact]
    public async Task DownloadFileAsync_CompletionStatus_ReportedLast()
    {
        var payload = new byte[] { 1, 2, 3 };
        _http.Map(HttpMethod.Get, "/tiny.bin", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(payload)
        });

        var reports = new List<InstallProgress>();
        await CreateDownloader().DownloadFileAsync(
            "https://example.com/tiny.bin", Path.Combine(_tempRoot, "tiny.bin"),
            new FileDownloadOptions { CompletionStatus = "verifying" },
            new Progress<InstallProgress>(p => reports.Add(p)));

        reports.Should().NotBeEmpty();
        reports.Last().Status.Should().Be("verifying");
    }

    [Fact]
    public async Task DownloadFileAsync_ConfigureRequest_Applied()
    {
        _http.Map(HttpMethod.Get, "/hdr.bin", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[] { 9 })
        });

        await CreateDownloader().DownloadFileAsync(
            "https://example.com/hdr.bin", Path.Combine(_tempRoot, "hdr.bin"),
            new FileDownloadOptions
            {
                ConfigureRequest = req => req.Headers.Referrer = new Uri("https://example.com/downloads")
            });

        _http.Requests.Should().ContainSingle();
        _http.Requests[0].Headers.Referrer.Should().Be(new Uri("https://example.com/downloads"));
    }

    [Fact]
    public async Task DownloadFileAsync_NotFound_ThrowsAndCreatesNoFile()
    {
        _http.MapJson(HttpMethod.Get, "/missing.bin", HttpStatusCode.NotFound, "nope");
        var dest = Path.Combine(_tempRoot, "missing.bin");

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            CreateDownloader().DownloadFileAsync("https://example.com/missing.bin", dest));

        File.Exists(dest).Should().BeFalse();
    }

    [Fact]
    public async Task DownloadFileAsync_NoFactory_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new FileDownloader().DownloadFileAsync("https://example.com/x.bin", Path.Combine(_tempRoot, "x.bin")));
    }

    [Fact]
    public async Task CopyStreamAsync_CopiesAndReportsProgress()
    {
        var payload = new byte[200_000];
        new Random(11).NextBytes(payload);
        var dest = Path.Combine(_tempRoot, "copy.bin");
        var reports = new List<InstallProgress>();

        await CreateDownloader().CopyStreamAsync(
            new MemoryStream(payload), dest, payload.Length, "Downloading",
            new Progress<InstallProgress>(p => reports.Add(p)));

        (await File.ReadAllBytesAsync(dest)).Should().Equal(payload);
        reports.Should().NotBeEmpty();
        reports.Last().PercentComplete.Should().Be(100);
        reports.Last().Status.Should().Be("Downloading");
    }
}
