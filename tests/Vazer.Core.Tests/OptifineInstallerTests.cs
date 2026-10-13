using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Vazer.Core;
using Vazer.Core.Abstractions;
using Vazer.Core.Loaders.Optifine;
using Vazer.Core.Modrinth;
using Vazer.Core.Mods;
using Vazer.Core.Tests.TestHelpers;

namespace Vazer.Core.Tests;

public sealed class OptifineInstallerTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly FakeHttpMessageHandler _http;
    private readonly Mock<IModrinthClient> _modrinthMock;

    private const string CatalogHtmlFixture = """
        <!DOCTYPE html>
        <html>
        <body>
        <h2>Minecraft 1.21.1</h2>
        <table>
          <tr class="downloadLine">
            <td class="colFile">OptiFine HD U J1</td>
            <td class="colDate">18.08.2024</td>
            <td class="colForge">Forge 52.0.2</td>
            <td class="colMirror"><a href="adloadx?f=OptiFine_1.21.1_HD_U_J1.jar">Mirror</a></td>
          </tr>
          <tr class="downloadLine downloadLinePreview">
            <td class="colFile">OptiFine HD U J4 pre2</td>
            <td class="colDate">20.08.2024</td>
            <td class="colForge"></td>
            <td class="colMirror"><a href="adloadx?f=preview_OptiFine_1.21.1_HD_U_J4_pre2.jar">Mirror</a></td>
          </tr>
        </table>
        <h2>Minecraft 1.20.4</h2>
        <table>
          <tr class="downloadLine">
            <td class="colFile">OptiFine HD U I7</td>
            <td class="colDate">15.01.2024</td>
            <td class="colForge">Forge 49.0.38</td>
            <td class="colMirror"><a href="adloadx?f=OptiFine_1.20.4_HD_U_I7.jar">Mirror</a></td>
          </tr>
        </table>
        </body>
        </html>
        """;

    public OptifineInstallerTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "upco-optifine-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _http = new FakeHttpMessageHandler();
        _modrinthMock = new Mock<IModrinthClient>();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, true);
            }
        }
        catch { }
    }

    private OptifineInstallerService CreateService(IModrinthClient? modrinthClient = null)
    {
        var options = Options.Create(new MinecraftOptions
        {
            RootPath = _tempRoot,
            OptifineUserAgent = "TestUserAgent/1.0"
        });

        return new OptifineInstallerService(
            new FakeHttpClientFactory(_http),
            options,
            NullLogger<OptifineInstallerService>.Instance,
            modrinthClient);
    }

    private MinecraftInstance CreateInstance(string loaderType = "vanilla", string mcVersion = "1.21.1")
    {
        var instanceDir = Path.Combine(_tempRoot, "instances", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(instanceDir);
        return new MinecraftInstance
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Test Instance",
            MinecraftVersion = mcVersion,
            LoaderType = loaderType,
            Path = instanceDir,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static byte[] CreateFakeJar(int size = 102401, bool validMagic = true)
    {
        var data = new byte[size];
        if (validMagic && size >= 4)
        {
            data[0] = 0x50; // P
            data[1] = 0x4B; // K
            data[2] = 0x03;
            data[3] = 0x04;
        }
        return data;
    }

    [Fact]
    public void ParseCatalog_ValidHtml_ReturnsCorrectReleases()
    {
        var catalog = OptifineInstallerService.ParseCatalog(CatalogHtmlFixture, "1.21.1");

        catalog.McVersion.Should().Be("1.21.1");
        catalog.Releases.Should().HaveCount(2);

        var first = catalog.Releases[0];
        first.Version.Should().Be("HD U J1");
        first.Edition.Should().Be("HD_U");
        first.Type.Should().Be("stable");
        first.DownloadPageUrl.Should().Be("https://optifine.net/adloadx?f=OptiFine_1.21.1_HD_U_J1.jar");
        first.Filename.Should().Be("OptiFine_1.21.1_HD_U_J1.jar");
        first.ForgeVersion.Should().Be("Forge 52.0.2");
        first.Date.Should().Be("18.08.2024");

        var second = catalog.Releases[1];
        second.Version.Should().Be("HD U J4 pre2");
        second.Edition.Should().Be("HD_U");
        second.Type.Should().Be("preview");
        second.DownloadPageUrl.Should().Be("https://optifine.net/adloadx?f=preview_OptiFine_1.21.1_HD_U_J4_pre2.jar");
        second.Filename.Should().Be("preview_OptiFine_1.21.1_HD_U_J4_pre2.jar");
        second.ForgeVersion.Should().BeNull();
        second.Date.Should().Be("20.08.2024");
    }

    [Fact]
    public async Task GetCatalog_SecondCallWithinTTL_UsesCache()
    {
        _http.Map(HttpMethod.Get, "/downloads", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CatalogHtmlFixture, Encoding.UTF8, "text/html")
            });

        var svc = CreateService();

        var first = await svc.GetCatalogAsync("1.21.1");
        var second = await svc.GetCatalogAsync("1.21.1");

        first.Should().BeSameAs(second);
        _http.CallCount(HttpMethod.Get, "/downloads").Should().Be(1);
    }

    [Fact]
    public async Task GetCatalog_NonexistentMcVersion_ThrowsOptifineVersionNotFoundException()
    {
        _http.Map(HttpMethod.Get, "/downloads", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CatalogHtmlFixture, Encoding.UTF8, "text/html")
            });

        var svc = CreateService();

        var act = () => svc.GetCatalogAsync("1.99.9");

        var ex = await act.Should().ThrowAsync<OptifineVersionNotFoundException>();
        ex.Which.McVersion.Should().Be("1.99.9");
    }

    [Fact]
    public async Task InstallFromCatalog_UnknownVersion_ThrowsOptifineVersionNotFoundException()
    {
        _http.Map(HttpMethod.Get, "/downloads", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CatalogHtmlFixture, Encoding.UTF8, "text/html")
            });

        var svc = CreateService();
        var instance = CreateInstance();

        var act = () => svc.InstallFromCatalogAsync(instance, "HD U Z99");

        await act.Should().ThrowAsync<OptifineVersionNotFoundException>();
    }

    [Fact]
    public async Task DownloadCatalog_ValidChain_SavesJarToMods()
    {
        _http.Map(HttpMethod.Get, "/downloads", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CatalogHtmlFixture, Encoding.UTF8, "text/html")
            });

        _http.Map(HttpMethod.Get, "/adloadx?f=OptiFine_1.21.1_HD_U_J1.jar", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<a href='downloadx?f=OptiFine_1.21.1_HD_U_J1.jar&x=abc123xyz'>Download</a>", Encoding.UTF8, "text/html")
            });

        _http.Map(HttpMethod.Get, "/downloadx?f=OptiFine_1.21.1_HD_U_J1.jar&x=abc123xyz", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(CreateFakeJar())
            });

        var svc = CreateService();
        var instance = CreateInstance();

        var result = await svc.InstallFromCatalogAsync(instance, "HD U J1");

        result.Success.Should().BeTrue();
        var jarPath = Path.Combine(instance.Path, "mods", "OptiFine_1.21.1_HD_U_J1.jar");
        File.Exists(jarPath).Should().BeTrue();
        result.InstalledFilePath.Should().Be(jarPath);

        var manifestPath = Path.Combine(instance.Path, "mods.json");
        File.Exists(manifestPath).Should().BeTrue();
        var manifest = JsonSerializer.Deserialize<ModsManifest>(await File.ReadAllTextAsync(manifestPath));
        manifest.Should().NotBeNull();
        manifest!.Mods.Should().ContainSingle(m =>
            m.FileName == "OptiFine_1.21.1_HD_U_J1.jar" &&
            m.ProjectId == "optifine" &&
            m.VersionId == "HD U J1" &&
            m.Source == "optifine" &&
            m.Enabled);
    }

    [Fact]
    public async Task DownloadCatalog_AntiHotlink_AssertsRefererHeader()
    {
        _http.Map(HttpMethod.Get, "/downloads", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CatalogHtmlFixture, Encoding.UTF8, "text/html")
            });

        _http.Map(HttpMethod.Get, "/adloadx?f=OptiFine_1.21.1_HD_U_J1.jar", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<a href='downloadx?f=OptiFine_1.21.1_HD_U_J1.jar&x=abc123xyz'>Download</a>", Encoding.UTF8, "text/html")
            });

        _http.Map(HttpMethod.Get, "/downloadx?f=OptiFine_1.21.1_HD_U_J1.jar&x=abc123xyz", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(CreateFakeJar())
            });

        var svc = CreateService();
        var instance = CreateInstance();

        await svc.InstallFromCatalogAsync(instance, "HD U J1");

        var adloadxReq = _http.Requests.Single(r => r.RequestUri!.PathAndQuery.StartsWith("/adloadx"));
        adloadxReq.Headers.Referrer.Should().NotBeNull();
        adloadxReq.Headers.Referrer!.ToString().Should().Be("https://optifine.net/downloads");

        var downloadxReq = _http.Requests.Single(r => r.RequestUri!.PathAndQuery.StartsWith("/downloadx"));
        downloadxReq.Headers.Referrer.Should().NotBeNull();
        downloadxReq.Headers.Referrer!.ToString().Should().Be("https://optifine.net/downloads");
    }

    [Fact]
    public async Task Download_SingleQuoteDownloadx_ParsedCorrectly()
    {
        _http.Map(HttpMethod.Get, "/adloadx?f=file.jar", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<a href='downloadx?f=file.jar&x=123'>Download</a>", Encoding.UTF8, "text/html")
            });

        _http.Map(HttpMethod.Get, "/downloadx?f=file.jar&x=123", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(CreateFakeJar())
            });

        var svc = CreateService();
        var instance = CreateInstance();

        var result = await svc.InstallFromUrlAsync(instance, "https://optifine.net/adloadx?f=file.jar");

        result.Success.Should().BeTrue();
        File.Exists(Path.Combine(instance.Path, "mods", "file.jar")).Should().BeTrue();
    }

    [Fact]
    public async Task Download_MagicBytesOrSizeMismatch_ThrowsAndDeletesPartial()
    {
        _http.Map(HttpMethod.Get, "/downloadx?f=badmagic.jar&x=1", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(CreateFakeJar(size: 110000, validMagic: false))
            });

        _http.Map(HttpMethod.Get, "/downloadx?f=toosmall.jar&x=2", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(CreateFakeJar(size: 500, validMagic: true))
            });

        var svc = CreateService();
        var instance1 = CreateInstance();
        var instance2 = CreateInstance();

        // Wrong magic bytes
        var act1 = () => svc.InstallFromUrlAsync(instance1, "https://optifine.net/downloadx?f=badmagic.jar&x=1");
        await act1.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*magic bytes*");
        Directory.GetFiles(Path.Combine(instance1.Path, "mods"), "*.partial").Should().BeEmpty();
        File.Exists(Path.Combine(instance1.Path, "mods", "badmagic.jar")).Should().BeFalse();

        // Size <= 100KB
        var act2 = () => svc.InstallFromUrlAsync(instance2, "https://optifine.net/downloadx?f=toosmall.jar&x=2");
        await act2.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*magic bytes*");
        Directory.GetFiles(Path.Combine(instance2.Path, "mods"), "*.partial").Should().BeEmpty();
        File.Exists(Path.Combine(instance2.Path, "mods", "toosmall.jar")).Should().BeFalse();
    }

    [Fact]
    public async Task ManualUrl_ValidAdloadxOrDownloadx_InstallsSuccessfully()
    {
        _http.Map(HttpMethod.Get, "/adloadx?f=OptiFine_1.21.1_HD_U_J1.jar", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<a href=\"downloadx?f=OptiFine_1.21.1_HD_U_J1.jar&x=789\">Download</a>", Encoding.UTF8, "text/html")
            });

        _http.Map(HttpMethod.Get, "/downloadx?f=OptiFine_1.21.1_HD_U_J1.jar&x=789", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(CreateFakeJar())
            });

        _http.Map(HttpMethod.Get, "/downloadx?f=OptiFine_1.21.1_HD_U_J1.jar&x=direct", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(CreateFakeJar())
            });

        var svc = CreateService();
        var instance1 = CreateInstance();
        var instance2 = CreateInstance();

        // Via adloadx
        var res1 = await svc.InstallFromUrlAsync(instance1, "https://optifine.net/adloadx?f=OptiFine_1.21.1_HD_U_J1.jar");
        res1.Success.Should().BeTrue();
        File.Exists(Path.Combine(instance1.Path, "mods", "OptiFine_1.21.1_HD_U_J1.jar")).Should().BeTrue();

        // Via direct downloadx
        var res2 = await svc.InstallFromUrlAsync(instance2, "https://optifine.net/downloadx?f=OptiFine_1.21.1_HD_U_J1.jar&x=direct");
        res2.Success.Should().BeTrue();
        File.Exists(Path.Combine(instance2.Path, "mods", "OptiFine_1.21.1_HD_U_J1.jar")).Should().BeTrue();
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://optifine.net/adloadx?f=OptiFine_1.21.1_HD_U_J1.jar")]
    [InlineData("https://evil.com/adloadx?f=OptiFine_1.21.1_HD_U_J1.jar")]
    [InlineData("https://optifine.net/adloadx")]
    [InlineData("https://optifine.net/adloadx?f=")]
    [InlineData("https://optifine.net/otherpage?f=OptiFine_1.21.1_HD_U_J1.jar")]
    public async Task ManualUrl_InvalidHostOrSchemeOrNoParamF_ThrowsArgumentException(string url)
    {
        var svc = CreateService();
        var instance = CreateInstance();

        var act = () => svc.InstallFromUrlAsync(instance, url);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Install_FabricInstance_SetsWarningWithSodiumIris()
    {
        _http.Map(HttpMethod.Get, "/downloadx?f=OptiFine_1.21.1_HD_U_J1.jar&x=1", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(CreateFakeJar())
            });

        var svc = CreateService();
        var instance = CreateInstance(loaderType: "fabric");

        var result = await svc.InstallFromUrlAsync(instance, "https://optifine.net/downloadx?f=OptiFine_1.21.1_HD_U_J1.jar&x=1");

        result.Success.Should().BeTrue();
        result.Warning.Should().NotBeNull();
        result.Warning.Should().Contain("Sodium + Iris");
    }

    [Fact]
    public async Task Install_FabricInstance_OptiFabricHit_SetsOptifabricFork()
    {
        _http.Map(HttpMethod.Get, "/downloadx?f=OptiFine_1.21.1_HD_U_J1.jar&x=1", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(CreateFakeJar())
            });

        _modrinthMock.Setup(m => m.SearchAsync(
            It.Is<SearchQuery>(q => q.Query == "optifabric"),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResult
            {
                Hits = [new SearchHit { Slug = "optifabric-fork-hit" }]
            });

        var svc = CreateService(_modrinthMock.Object);
        var instance = CreateInstance(loaderType: "fabric");

        var result = await svc.InstallFromUrlAsync(instance, "https://optifine.net/downloadx?f=OptiFine_1.21.1_HD_U_J1.jar&x=1");

        result.Success.Should().BeTrue();
        result.OptifabricFork.Should().Be("optifabric-fork-hit");
    }

    [Fact]
    public async Task Install_FabricInstance_OptiFabricMiss_DoesNotThrow()
    {
        _http.Map(HttpMethod.Get, "/downloadx?f=OptiFine_1.21.1_HD_U_J1.jar&x=1", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(CreateFakeJar())
            });

        // 1. Returns empty hits
        _modrinthMock.Setup(m => m.SearchAsync(
            It.IsAny<SearchQuery>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResult { Hits = [] });

        var svc = CreateService(_modrinthMock.Object);
        var instance = CreateInstance(loaderType: "fabric");

        var result1 = await svc.InstallFromUrlAsync(instance, "https://optifine.net/downloadx?f=OptiFine_1.21.1_HD_U_J1.jar&x=1");
        result1.Success.Should().BeTrue();
        result1.OptifabricFork.Should().BeNull();

        // 2. Modrinth search throws exception
        _modrinthMock.Setup(m => m.SearchAsync(
            It.IsAny<SearchQuery>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("network down"));

        var result2 = await svc.InstallFromUrlAsync(instance, "https://optifine.net/downloadx?f=OptiFine_1.21.1_HD_U_J1.jar&x=1");
        result2.Success.Should().BeTrue();
        result2.OptifabricFork.Should().BeNull();
    }

    [Fact]
    public async Task Install_NeoForgeInstance_SetsWarningUntested()
    {
        _http.Map(HttpMethod.Get, "/downloadx?f=OptiFine_1.21.1_HD_U_J1.jar&x=1", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(CreateFakeJar())
            });

        var svc = CreateService();
        var instance = CreateInstance(loaderType: "neoforge");

        var result = await svc.InstallFromUrlAsync(instance, "https://optifine.net/downloadx?f=OptiFine_1.21.1_HD_U_J1.jar&x=1");

        result.Success.Should().BeTrue();
        result.Warning.Should().NotBeNull();
        result.Warning.Should().Contain("chưa test");
    }

    [Fact]
    public async Task Install_VanillaInstance_WarningIsNull()
    {
        _http.Map(HttpMethod.Get, "/downloadx?f=OptiFine_1.21.1_HD_U_J1.jar&x=1", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(CreateFakeJar())
            });

        var svc = CreateService();
        var instance = CreateInstance(loaderType: "vanilla");

        var result = await svc.InstallFromUrlAsync(instance, "https://optifine.net/downloadx?f=OptiFine_1.21.1_HD_U_J1.jar&x=1");

        result.Success.Should().BeTrue();
        result.Warning.Should().BeNull();
    }
}
