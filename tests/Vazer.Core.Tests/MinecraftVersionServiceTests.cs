using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vazer.Core.Versions;
using Vazer.Core.Tests.TestHelpers;

namespace Vazer.Core.Tests;

public sealed class MinecraftVersionServiceTests : IDisposable
{
    private readonly FakeHttpMessageHandler _handler;
    private readonly MinecraftVersionService _service;
    private readonly string _cacheRoot;

    private static readonly MinecraftOptions TestOptions = new();

    public MinecraftVersionServiceTests()
    {
        _handler = new FakeHttpMessageHandler();
        var factory = new FakeHttpClientFactory(_handler);
        _cacheRoot = Path.Combine(Path.GetTempPath(), $"vaz-test-cache-{Guid.NewGuid()}");
        Directory.CreateDirectory(_cacheRoot);

        var options = new TestOptionsAccessor(_cacheRoot);
        _service = new MinecraftVersionService(
            factory,
            NullLogger<MinecraftVersionService>.Instance,
            options);
    }

    public void Dispose()
    {
        try { Directory.Delete(_cacheRoot, recursive: true); }
        catch { }
    }

    private class TestOptionsAccessor : IOptions<MinecraftOptions>
    {
        private readonly string _cacheRoot;
        public TestOptionsAccessor(string cacheRoot) => _cacheRoot = cacheRoot;
        public MinecraftOptions Value => new() { RootPath = _cacheRoot };
    }

    // ── 1. GetVanillaVersionsAsync_FetchesFromMojang ──

    [Fact]
    public async Task GetVanillaVersionsAsync_FetchesFromMojang()
    {
        var manifestJson = """{"versions":[{"id":"1.21.1","type":"release","releaseTime":"2024-06-13T09:45:00+00:00","url":"https://example.com/v1.21.1"},{"id":"1.21","type":"release","releaseTime":"2024-06-12T09:45:00+00:00","url":"https://example.com/v1.21"},{"id":"24w36a","type":"snapshot","releaseTime":"2024-09-04T09:45:00+00:00","url":"https://example.com/24w36a"}]}""";
        _handler.MapJson(HttpMethod.Get, "/v2/versions/loader/1.21.1", HttpStatusCode.OK, "[]");
        _handler.MapJson(HttpMethod.Get, "/mc/game/version_manifest_v2.json", HttpStatusCode.OK, manifestJson);

        var versions = await _service.GetVanillaVersionsAsync();

        versions.Should().HaveCount(3);
        versions[0].Id.Should().Be("1.21.1");
        versions[0].Type.Should().Be("release");
        versions[1].Id.Should().Be("1.21");
        versions[1].Type.Should().Be("release");
        versions[2].Id.Should().Be("24w36a");
        versions[2].Type.Should().Be("snapshot");
    }

    // ── 2. GetLoaderVersionsAsync_Fabric ──

    [Fact]
    public async Task GetLoaderVersionsAsync_Fabric()
    {
        var fabricJson = """[{"version":"0.16.9","loader":"fabric","minecraft":"1.21.1","release":"stable","url":"https://example.com"},{"version":"0.16.8","loader":"fabric","minecraft":"1.21.1","release":"stable","url":"https://example.com"}]""";
        _handler.MapJson(HttpMethod.Get, "https://meta.fabricmc.net/v2/versions/loader/1.21.1", HttpStatusCode.OK, fabricJson);
        _handler.MapJson(HttpMethod.Get, "/v2/versions/loader/1.21.1", HttpStatusCode.OK, fabricJson);

        var versions = await _service.GetLoaderVersionsAsync("fabric", "1.21.1");

        versions.Should().HaveCount(2);
        versions[0].Version.Should().Be("0.16.9");
        versions[0].Loader.Should().Be("fabric");
        versions[0].MinecraftVersion.Should().Be("1.21.1");
        versions[0].Stable.Should().BeTrue();
    }

    // ── 3. GetLoaderVersionsAsync_Quilt ──

    [Fact]
    public async Task GetLoaderVersionsAsync_Quilt()
    {
        var quiltJson = """[{"version":"5.2.0","loader":"quilt","minecraft":"1.21.1","release":"stable","url":"https://example.com"},{"version":"5.1.0","loader":"quilt","minecraft":"1.21.1","release":"beta","url":"https://example.com"}]""";
        _handler.MapJson(HttpMethod.Get, "https://meta.quiltmc.org/v3/versions/loader/1.21.1", HttpStatusCode.OK, quiltJson);
        _handler.MapJson(HttpMethod.Get, "/v3/versions/loader/1.21.1", HttpStatusCode.OK, quiltJson);

        var versions = await _service.GetLoaderVersionsAsync("quilt", "1.21.1");

        versions.Should().HaveCount(2);
        versions[0].Version.Should().Be("5.2.0");
        versions[0].Stable.Should().BeTrue();
        versions[1].Version.Should().Be("5.1.0");
        versions[1].Stable.Should().BeFalse();
    }

    // ── 4. GetLoaderVersionsAsync_UnsupportedLoader_EmptyList ──

    [Fact]
    public async Task GetLoaderVersionsAsync_UnsupportedLoader_EmptyList()
    {
        var versions = await _service.GetLoaderVersionsAsync("unsupported-loader", "1.21.1");

        versions.Should().BeEmpty();
    }

    // ── 5. ClearCacheAsync ──

    [Fact]
    public async Task ClearCacheAsync_ClearsAllFiles()
    {
        // Service lưu cache trong thư mục con `cache` của RootPath.
        var dummyDir = Path.Combine(_cacheRoot, "cache");
        Directory.CreateDirectory(dummyDir);
        var dummyFile = Path.Combine(dummyDir, "versions-manifest.json");
        await File.WriteAllTextAsync(dummyFile, "{}");

        File.Exists(dummyFile).Should().BeTrue();

        var result = await _service.ClearCacheAsync();

        result.Should().BeTrue();
        File.Exists(dummyFile).Should().BeFalse();
        Directory.Exists(_cacheRoot).Should().BeTrue(); // Still exists but empty
    }

    // ── 6. GetCacheTimestampAsync_Exists ──

    [Fact]
    public async Task GetCacheTimestampAsync_Exists()
    {
        // Service lưu cache trong thư mục con `cache` của RootPath.
        var cacheDir = Path.Combine(_cacheRoot, "cache");
        Directory.CreateDirectory(cacheDir);
        var cacheFile = Path.Combine(cacheDir, "test-cache.json");
        await File.WriteAllTextAsync(cacheFile, "{}");

        var timestamp = await _service.GetCacheTimestampAsync("test-cache.json");
        timestamp.Should().NotBe(null);
    }

    // ── 7. GetCacheTimestampAsync_Missing ──

    [Fact]
    public async Task GetCacheTimestampAsync_Missing()
    {
        var timestamp = await _service.GetCacheTimestampAsync("nonexistent.json");
        timestamp.Should().Be(null);
    }
}
