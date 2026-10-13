using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vazer.Core;
using Vazer.Core.Abstractions;
using Vazer.Core.Http;
using Vazer.Core.Java;
using Vazer.Core.Tests.TestHelpers;

namespace Vazer.Core.Tests;

/// <summary>
/// Item 5: AdoptiumClient - query metadata JDK + download file. Khong network that.
/// </summary>
public sealed class AdoptiumClientTests : IDisposable
{
    private const string ApiPath = "/v3/assets/latest/21/hotspot?architecture=x64&image_type=jdk&os=windows&vendor=eclipse";

    private readonly string _tempRoot;
    private readonly FakeHttpMessageHandler _http;

    public AdoptiumClientTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "vaz-adoptium-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _http = new FakeHttpMessageHandler();
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, true); } catch { }
    }

    private AdoptiumClient CreateClient(int maxRetries = 0) => new(
        new FakeHttpClientFactory(_http),
        NullLogger<AdoptiumClient>.Instance,
        Options.Create(new MinecraftOptions
        {
            RootPath = _tempRoot,
            HttpTimeoutSeconds = 30,
            HttpMaxRetries = maxRetries
        }),
        new FileDownloader(new FakeHttpClientFactory(_http)));

    private static string AssetJson(string link = "https://example.com/jdk-21.zip", string checksum = "abc123", long size = 12345)
        => $"[{{\"binary\":{{\"package\":{{\"link\":\"{link}\",\"checksum\":\"{checksum}\",\"size\":{size}}}}}}}]";

    [Fact]
    public async Task GetLatestJdkAsync_ReturnsAsset()
    {
        _http.MapJson(HttpMethod.Get, ApiPath, HttpStatusCode.OK, AssetJson());

        var asset = await CreateClient().GetLatestJdkAsync(21);

        asset.DownloadUrl.Should().Be("https://example.com/jdk-21.zip");
        asset.Sha256Checksum.Should().Be("abc123");
        asset.SizeBytes.Should().Be(12345);
    }

    [Fact]
    public async Task GetLatestJdkAsync_EmptyArray_Throws()
    {
        _http.MapJson(HttpMethod.Get, ApiPath, HttpStatusCode.OK, "[]");

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateClient().GetLatestJdkAsync(21));
    }

    [Fact]
    public async Task GetLatestJdkAsync_MissingLink_Throws()
    {
        _http.MapJson(HttpMethod.Get, ApiPath, HttpStatusCode.OK,
            "[{\"binary\":{\"package\":{\"checksum\":\"abc\",\"size\":10}}}]");

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateClient().GetLatestJdkAsync(21));
    }

    [Fact]
    public async Task GetLatestJdkAsync_RetriesOn500_ThenSucceeds()
    {
        var calls = 0;
        _http.Map(HttpMethod.Get, ApiPath, _ =>
        {
            calls++;
            return calls == 1
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(AssetJson(), Encoding.UTF8, "application/json")
                };
        });

        var asset = await CreateClient(maxRetries: 1).GetLatestJdkAsync(21);

        asset.DownloadUrl.Should().Be("https://example.com/jdk-21.zip");
        _http.CallCount(HttpMethod.Get, ApiPath).Should().Be(2);
    }

    [Fact]
    public async Task GetLatestJdkAsync_RetryExhausted_Throws()
    {
        _http.MapJson(HttpMethod.Get, ApiPath, HttpStatusCode.InternalServerError, "boom");

        await Assert.ThrowsAsync<HttpRequestException>(() => CreateClient(maxRetries: 1).GetLatestJdkAsync(21));
        _http.CallCount(HttpMethod.Get, ApiPath).Should().Be(2);
    }

    [Fact]
    public async Task DownloadFileAsync_WritesBytesAndReportsProgress()
    {
        var payload = new byte[200_000];
        new Random(42).NextBytes(payload);
        _http.Map(HttpMethod.Get, "/jdk-21.zip", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(payload)
        });

        var dest = Path.Combine(_tempRoot, "jdk.zip");
        var reports = new List<InstallProgress>();
        var progress = new Progress<InstallProgress>(p => reports.Add(p));

        await CreateClient().DownloadFileAsync("https://example.com/jdk-21.zip", dest, progress);

        (await File.ReadAllBytesAsync(dest)).Should().Equal(payload);
        reports.Should().NotBeEmpty();
        reports.Last().PercentComplete.Should().Be(100);
        reports.Last().Status.Should().Be("verifying");
    }
}
