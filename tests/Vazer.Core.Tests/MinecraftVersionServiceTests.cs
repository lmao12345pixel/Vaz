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
        // Hình dạng API thật của Fabric Meta: nested { loader: { version, stable, maven } }
        var fabricJson = """[{"loader":{"version":"0.16.9","stable":true,"maven":"net.fabricmc:fabric-loader:0.16.9"},"intermediary":{"version":"1.21.1","stable":true}},{"loader":{"version":"0.16.8","stable":true,"maven":"net.fabricmc:fabric-loader:0.16.8"},"intermediary":{"version":"1.21.1","stable":true}}]""";
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
        // Hình dạng API thật của Quilt Meta: nested { loader: { version, maven } }; không có cờ stable
        var quiltJson = """[{"loader":{"version":"5.2.0","maven":"org.quiltmc:quilt-loader:5.2.0"}},{"loader":{"version":"5.1.0-beta.1","maven":"org.quiltmc:quilt-loader:5.1.0-beta.1"}}]""";
        _handler.MapJson(HttpMethod.Get, "https://meta.quiltmc.org/v3/versions/loader/1.21.1", HttpStatusCode.OK, quiltJson);
        _handler.MapJson(HttpMethod.Get, "/v3/versions/loader/1.21.1", HttpStatusCode.OK, quiltJson);

        var versions = await _service.GetLoaderVersionsAsync("quilt", "1.21.1");

        versions.Should().HaveCount(2);
        versions[0].Version.Should().Be("5.2.0");
        versions[0].Stable.Should().BeTrue();
        versions[1].Version.Should().Be("5.1.0-beta.1");
        versions[1].Stable.Should().BeFalse();
    }

    // ── 3b. GetLoaderVersionsAsync_NeoForge ──

    [Fact]
    public async Task GetLoaderVersionsAsync_NeoForge()
    {
        var mavenJson = """{"isSnapshot":false,"versions":["21.1.0","21.1.206","21.1.10-beta","21.2.0-beta","20.2.1","26.3.0.101"]}""";
        // Fake handler match theo PathAndQuery (không host) — map cả 2 dạng như fabric/quilt test.
        _handler.MapJson(HttpMethod.Get, "https://maven.neoforged.net/api/maven/versions/releases/net/neoforged/neoforge", HttpStatusCode.OK, mavenJson);
        _handler.MapJson(HttpMethod.Get, "/api/maven/versions/releases/net/neoforged/neoforge", HttpStatusCode.OK, mavenJson);

        var versions = await _service.GetLoaderVersionsAsync("neoforge", "1.21.1");

        // Chỉ giữ các build khớp prefix "21.1." của MC 1.21.1, giữ nguyên thứ tự API.
        versions.Should().HaveCount(3);
        versions.Select(v => v.Version).Should().Equal("21.1.0", "21.1.206", "21.1.10-beta");
        versions.Count(v => v.Stable).Should().Be(2);
        versions.Count(v => v.Installable && !v.Stable).Should().Be(1);
        versions.Should().OnlyContain(v => v.Loader == "neoforge" && v.MinecraftVersion == "1.21.1");
    }

    [Fact]
    public async Task GetLoaderVersionsAsync_NeoForge_UnsupportedMc_ReturnsEmpty()
    {
        // MC 1.19.4 → prefix "19.4." < 20.20 → bị từ chối, không gọi HTTP.
        var versions = await _service.GetLoaderVersionsAsync("neoforge", "1.19.4");

        versions.Should().BeEmpty();
    }

    // ── 3c. GetLoaderVersionsAsync_Forge ──

    [Fact]
    public async Task GetLoaderVersionsAsync_Forge_FiltersAndMarksRecommended()
    {
        var promosJson = """
        {
          "homepage": "http://files.minecraftforge.net/maven/net/minecraftforge/forge/",
          "promos": {
            "1.20.1-recommended": "47.2.0",
            "1.20.1-latest": "47.2.1",
            "1.19.4-recommended": "45.4.0",
            "1.19.4-latest": "45.4.5"
          }
        }
        """;
        _handler.MapJson(HttpMethod.Get, "https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json", HttpStatusCode.OK, promosJson);
        _handler.MapJson(HttpMethod.Get, "/net/minecraftforge/forge/promotions_slim.json", HttpStatusCode.OK, promosJson);

        var versions = await _service.GetLoaderVersionsAsync("forge", "1.20.1");

        versions.Should().HaveCount(2);
        versions[0].Version.Should().Be("1.20.1-47.2.0");
        versions[0].IsRecommended.Should().BeTrue();
        versions[0].Stable.Should().BeTrue();
        versions[0].Loader.Should().Be("forge");
        versions[0].MinecraftVersion.Should().Be("1.20.1");
        versions[0].Url.Should().Be("https://maven.minecraftforge.net/net/minecraftforge/forge/1.20.1-47.2.0/forge-1.20.1-47.2.0-installer.jar");

        versions[1].Version.Should().Be("1.20.1-47.2.1");
        versions[1].IsRecommended.Should().BeFalse();
        versions[1].Stable.Should().BeFalse();
        versions[1].Installable.Should().BeTrue();
    }

    [Fact]
    public async Task GetLoaderVersionsAsync_Forge_WhenSameRecommendedAndLatest_ReturnsSingleItem()
    {
        var promosJson = """
        {
          "promos": {
            "1.20.1-recommended": "47.4.28",
            "1.20.1-latest": "47.4.28"
          }
        }
        """;
        _handler.MapJson(HttpMethod.Get, "https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json", HttpStatusCode.OK, promosJson);
        _handler.MapJson(HttpMethod.Get, "/net/minecraftforge/forge/promotions_slim.json", HttpStatusCode.OK, promosJson);

        var versions = await _service.GetForgeVersionsAsync("1.20.1");

        versions.Should().HaveCount(1);
        versions[0].Version.Should().Be("1.20.1-47.4.28");
        versions[0].IsRecommended.Should().BeTrue();
    }

    [Fact]
    public async Task GetLoaderVersionsAsync_Forge_UnsupportedMc_ReturnsEmpty()
    {
        var promosJson = """
        {
          "promos": {
            "1.20.1-recommended": "47.2.0"
          }
        }
        """;
        _handler.MapJson(HttpMethod.Get, "https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json", HttpStatusCode.OK, promosJson);
        _handler.MapJson(HttpMethod.Get, "/net/minecraftforge/forge/promotions_slim.json", HttpStatusCode.OK, promosJson);

        var versions = await _service.GetForgeVersionsAsync("26.3");

        versions.Should().BeEmpty();
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
