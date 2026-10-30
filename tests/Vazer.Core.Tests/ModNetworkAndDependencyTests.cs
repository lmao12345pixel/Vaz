using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Vazer.Core;
using Vazer.Core.Abstractions;
using Vazer.Core.Json;
using Vazer.Core.Modpacks;
using Vazer.Core.Modrinth;
using Vazer.Core.Mods;
using Vazer.Core.Tests.TestHelpers;

namespace Vazer.Core.Tests;

public sealed class ModNetworkAndDependencyTests : IDisposable
{
    private readonly FakeHttpMessageHandler _handler;
    private readonly ModrinthClient _client;
    private readonly string _tempDir;

    private static readonly MinecraftOptions TestOptions = new()
    {
        ModrinthUserAgent = "TestAgent/1.0 (test@test.com)"
    };

    public ModNetworkAndDependencyTests()
    {
        _handler = new FakeHttpMessageHandler();
        var factory = new FakeHttpClientFactory(_handler);
        _client = new ModrinthClient(
            factory,
            NullLogger<ModrinthClient>.Instance,
            Options.Create(TestOptions));

        _tempDir = Path.Combine(Path.GetTempPath(), "vaz-net-dep-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Ignore
        }
    }

    // ── ER-1: Mất mạng (Network failure) ──

    [Fact]
    public async Task ER1_NetworkFailure_ThrowsHttpRequestException()
    {
        var query = new SearchQuery { Query = "network-fail" };
        var facets = ModrinthClient.BuildFacets(query);
        var path = $"/v2/search?query=network-fail&limit=20&offset=0&facets={Uri.EscapeDataString(facets)}";

        _handler.Map(HttpMethod.Get, path, _ =>
            throw new HttpRequestException("Network unreachable", null, HttpStatusCode.ServiceUnavailable));

        var act = () => _client.SearchAsync(query);
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ── ER-2: Timeout ──

    [Fact]
    public async Task ER2_Timeout_ThrowsTaskCanceledException()
    {
        var query = new SearchQuery { Query = "timeout-test" };
        var facets = ModrinthClient.BuildFacets(query);
        var path = $"/v2/search?query=timeout-test&limit=20&offset=0&facets={Uri.EscapeDataString(facets)}";

        _handler.Map(HttpMethod.Get, path, _ =>
            throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout"));

        var act = () => _client.SearchAsync(query);
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    // ── ER-3: 429 có Retry-After ──

    [Fact]
    public async Task ER3_TooManyRequests_RetriesAndSucceedsWhenNextCallOk()
    {
        var query = new SearchQuery { Query = "rate-limit-test" };
        var facets = ModrinthClient.BuildFacets(query);
        var path = $"/v2/search?query=rate-limit-test&limit=20&offset=0&facets={Uri.EscapeDataString(facets)}";

        int callCount = 0;
        _handler.Map(HttpMethod.Get, path, _ =>
        {
            callCount++;
            if (callCount == 1)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                resp.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero);
                return resp;
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"hits":[],"total_hits":0,"offset":0,"limit":20}""", Encoding.UTF8, "application/json")
            };
        });

        var result = await _client.SearchAsync(query);
        result.Should().NotBeNull();
        result.TotalHits.Should().Be(0);
        callCount.Should().Be(2);
    }

    // ── ER-4: 5xx server error ──

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ER4_ServerError_ThrowsHttpRequestException(HttpStatusCode statusCode)
    {
        var query = new SearchQuery { Query = $"server-error-{(int)statusCode}" };
        var facets = ModrinthClient.BuildFacets(query);
        var path = $"/v2/search?query={Uri.EscapeDataString(query.Query)}&limit=20&offset=0&facets={Uri.EscapeDataString(facets)}";

        _handler.Map(HttpMethod.Get, path, _ => new HttpResponseMessage(statusCode));

        var act = () => _client.SearchAsync(query);
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ── DP-1: Gõ nhanh hủy request cũ (Cancellation token cancellation) ──

    [Fact]
    public async Task DP1_RapidTyping_CancelsPreviousRequestWithoutCompleting()
    {
        var ctsOld = new CancellationTokenSource();
        var ctsNew = new CancellationTokenSource();

        var queryOld = new SearchQuery { Query = "sodi" };
        var queryNew = new SearchQuery { Query = "sodium" };

        var pathOld = $"/v2/search?query=sodi&limit=20&offset=0&facets={Uri.EscapeDataString(ModrinthClient.BuildFacets(queryOld))}";
        var pathNew = $"/v2/search?query=sodium&limit=20&offset=0&facets={Uri.EscapeDataString(ModrinthClient.BuildFacets(queryNew))}";

        _handler.Map(HttpMethod.Get, pathOld, _ =>
        {
            ctsOld.Cancel();
            ctsOld.Token.ThrowIfCancellationRequested();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        _handler.MapJson(HttpMethod.Get, pathNew, HttpStatusCode.OK,
            """{"hits":[{"project_id":"p1","slug":"sodium","title":"Sodium"}],"total_hits":1,"offset":0,"limit":20}""");

        // Request 1 khởi động với token bị cancel giữa chừng
        var actOld = () => _client.SearchAsync(queryOld, ctsOld.Token);
        await actOld.Should().ThrowAsync<OperationCanceledException>();

        // Request 2 gửi đi và hoàn thành bình thường
        var resultNew = await _client.SearchAsync(queryNew, ctsNew.Token);
        resultNew.Hits.Should().HaveCount(1);
        resultNew.Hits[0].Slug.Should().Be("sodium");
    }

    // ── PH-2: Phân trang / load more không trùng lặp ──

    [Fact]
    public async Task PH2_Pagination_ConsecutiveOffsets_ReturnDistinctPages()
    {
        var facets = ModrinthClient.BuildFacets(new SearchQuery { Query = "pagination" });
        var path1 = $"/v2/search?query=pagination&limit=2&offset=0&facets={Uri.EscapeDataString(facets)}";
        var path2 = $"/v2/search?query=pagination&limit=2&offset=2&facets={Uri.EscapeDataString(facets)}";

        _handler.MapJson(HttpMethod.Get, path1, HttpStatusCode.OK,
            """{"hits":[{"project_id":"p1","slug":"mod-1"},{"project_id":"p2","slug":"mod-2"}],"total_hits":4,"offset":0,"limit":2}""");
        _handler.MapJson(HttpMethod.Get, path2, HttpStatusCode.OK,
            """{"hits":[{"project_id":"p3","slug":"mod-3"},{"project_id":"p4","slug":"mod-4"}],"total_hits":4,"offset":2,"limit":2}""");

        var page1 = await _client.SearchAsync(new SearchQuery { Query = "pagination", Limit = 2, Offset = 0 });
        var page2 = await _client.SearchAsync(new SearchQuery { Query = "pagination", Limit = 2, Offset = 2 });

        page1.Hits.Select(h => h.ProjectId).Should().Equal("p1", "p2");
        page2.Hits.Select(h => h.ProjectId).Should().Equal("p3", "p4");
        page1.Hits.Select(h => h.ProjectId).Intersect(page2.Hits.Select(h => h.ProjectId)).Should().BeEmpty();
    }

    // ── PH-3: Sắp xếp (SortIndex downloads, updated, newest) ──

    [Theory]
    [InlineData("downloads", "index=downloads")]
    [InlineData("updated", "index=updated")]
    [InlineData("newest", "index=newest")]
    public async Task PH3_Sorting_IncludesCorrectIndexParameter(string sortIndex, string expectedQueryFragment)
    {
        var query = new SearchQuery { Query = "sort-test", SortIndex = sortIndex, Limit = 10 };
        var facets = ModrinthClient.BuildFacets(query);
        var path = $"/v2/search?query=sort-test&limit=10&offset=0&index={sortIndex}&facets={Uri.EscapeDataString(facets)}";

        _handler.MapJson(HttpMethod.Get, path, HttpStatusCode.OK,
            """{"hits":[],"total_hits":0,"offset":0,"limit":10}""");

        await _client.SearchAsync(query);

        var req = _handler.Requests.Last();
        req.RequestUri!.Query.Should().Contain(expectedQueryFragment);
    }

    // ── DEP: Dependency resolution (required, incompatible, optional) ──

    [Fact]
    public async Task DEP_RequiredDependency_InstallsRecursively()
    {
        var instancePath = Path.Combine(_tempDir, "inst-dep-req");
        Directory.CreateDirectory(Path.Combine(instancePath, "mods"));

        var instMgrMock = new Mock<IInstanceManager>();
        instMgrMock.Setup(m => m.GetAsync("inst-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MinecraftInstance { Id = "inst-1", Name = "Test", Path = instancePath, LoaderType = "fabric", MinecraftVersion = "1.21.1", CreatedAt = DateTimeOffset.UtcNow });

        var modrinthMock = new Mock<IModrinthClient>();

        // Mod A depends on Mod B (required)
        var projA = new ModrinthProject { Id = "pA", Slug = "mod-a", Title = "Mod A", ProjectType = "mod" };
        var verA = new ProjectVersion
        {
            Id = "vA",
            ProjectId = "pA",
            VersionType = "release",
            DatePublished = DateTimeOffset.UtcNow,
            Files = [new VersionFile { Url = "https://cdn.modrinth.com/mod-a.jar", Filename = "mod-a.jar", Primary = true, Hashes = new FileHashes() }],
            Dependencies = [new VersionDependency { ProjectId = "pB", DependencyType = "required" }]
        };

        var projB = new ModrinthProject { Id = "pB", Slug = "mod-b", Title = "Mod B", ProjectType = "mod" };
        var verB = new ProjectVersion
        {
            Id = "vB",
            ProjectId = "pB",
            VersionType = "release",
            DatePublished = DateTimeOffset.UtcNow,
            Files = [new VersionFile { Url = "https://cdn.modrinth.com/mod-b.jar", Filename = "mod-b.jar", Primary = true, Hashes = new FileHashes() }],
            Dependencies = []
        };

        modrinthMock.Setup(m => m.GetProjectAsync("mod-a", It.IsAny<CancellationToken>())).ReturnsAsync(projA);
        modrinthMock.Setup(m => m.GetVersionsAsync("mod-a", "fabric", "1.21.1", It.IsAny<CancellationToken>())).ReturnsAsync([verA]);
        modrinthMock.Setup(m => m.DownloadAsync(verA.Files[0].Url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("fake-mod-a")));

        modrinthMock.Setup(m => m.GetProjectAsync("pB", It.IsAny<CancellationToken>())).ReturnsAsync(projB);
        modrinthMock.Setup(m => m.GetVersionsAsync("pB", "fabric", "1.21.1", It.IsAny<CancellationToken>())).ReturnsAsync([verB]);
        modrinthMock.Setup(m => m.DownloadAsync(verB.Files[0].Url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("fake-mod-b")));

        var svc = new ModInstallService(instMgrMock.Object, modrinthMock.Object, Options.Create(TestOptions), NullLogger<ModInstallService>.Instance);
        var result = await svc.InstallFromModrinthAsync("inst-1", "mod-a", resolveDependencies: true);

        result.Success.Should().BeTrue();
        result.InstalledDependencies.Should().Contain("pB");
        File.Exists(Path.Combine(instancePath, "mods", "mod-a.jar")).Should().BeTrue();
        File.Exists(Path.Combine(instancePath, "mods", "mod-b.jar")).Should().BeTrue();
    }

    [Fact]
    public async Task DEP_IncompatibleDependency_ThrowsConflictException()
    {
        var instancePath = Path.Combine(_tempDir, "inst-dep-incompat");
        var modsDir = Path.Combine(instancePath, "mods");
        Directory.CreateDirectory(modsDir);

        // Mod C already installed
        var manifest = new ModsManifest
        {
            Mods = [new InstalledMod { FileName = "mod-c.jar", ProjectId = "pC", VersionId = "vC", Enabled = true }]
        };
        await File.WriteAllTextAsync(Path.Combine(instancePath, "mods.json"), System.Text.Json.JsonSerializer.Serialize(manifest, VazJsonContext.CamelCase.ModsManifest));

        var instMgrMock = new Mock<IInstanceManager>();
        instMgrMock.Setup(m => m.GetAsync("inst-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MinecraftInstance { Id = "inst-1", Name = "Test", Path = instancePath, LoaderType = "fabric", MinecraftVersion = "1.21.1", CreatedAt = DateTimeOffset.UtcNow });

        var modrinthMock = new Mock<IModrinthClient>();
        var projA = new ModrinthProject { Id = "pA", Slug = "mod-a", Title = "Mod A", ProjectType = "mod" };
        var verA = new ProjectVersion
        {
            Id = "vA",
            ProjectId = "pA",
            VersionType = "release",
            DatePublished = DateTimeOffset.UtcNow,
            Files = [new VersionFile { Url = "https://cdn.modrinth.com/mod-a.jar", Filename = "mod-a.jar", Primary = true, Hashes = new FileHashes() }],
            Dependencies = [new VersionDependency { ProjectId = "pC", DependencyType = "incompatible" }]
        };

        modrinthMock.Setup(m => m.GetProjectAsync("mod-a", It.IsAny<CancellationToken>())).ReturnsAsync(projA);
        modrinthMock.Setup(m => m.GetVersionsAsync("mod-a", "fabric", "1.21.1", It.IsAny<CancellationToken>())).ReturnsAsync([verA]);
        modrinthMock.Setup(m => m.DownloadAsync(verA.Files[0].Url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("fake-mod-a")));

        var svc = new ModInstallService(instMgrMock.Object, modrinthMock.Object, Options.Create(TestOptions), NullLogger<ModInstallService>.Instance);
        var act = () => svc.InstallFromModrinthAsync("inst-1", "mod-a", resolveDependencies: true);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Incompatible mod conflict*");
    }

    [Fact]
    public async Task DEP_OptionalDependency_SkippedWithoutError()
    {
        var instancePath = Path.Combine(_tempDir, "inst-dep-opt");
        Directory.CreateDirectory(Path.Combine(instancePath, "mods"));

        var instMgrMock = new Mock<IInstanceManager>();
        instMgrMock.Setup(m => m.GetAsync("inst-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MinecraftInstance { Id = "inst-1", Name = "Test", Path = instancePath, LoaderType = "fabric", MinecraftVersion = "1.21.1", CreatedAt = DateTimeOffset.UtcNow });

        var modrinthMock = new Mock<IModrinthClient>();
        var projA = new ModrinthProject { Id = "pA", Slug = "mod-a", Title = "Mod A", ProjectType = "mod" };
        var verA = new ProjectVersion
        {
            Id = "vA",
            ProjectId = "pA",
            VersionType = "release",
            DatePublished = DateTimeOffset.UtcNow,
            Files = [new VersionFile { Url = "https://cdn.modrinth.com/mod-a.jar", Filename = "mod-a.jar", Primary = true, Hashes = new FileHashes() }],
            Dependencies = [new VersionDependency { ProjectId = "pOpt", DependencyType = "optional" }]
        };

        modrinthMock.Setup(m => m.GetProjectAsync("mod-a", It.IsAny<CancellationToken>())).ReturnsAsync(projA);
        modrinthMock.Setup(m => m.GetVersionsAsync("mod-a", "fabric", "1.21.1", It.IsAny<CancellationToken>())).ReturnsAsync([verA]);
        modrinthMock.Setup(m => m.DownloadAsync(verA.Files[0].Url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("fake-mod-a")));

        var svc = new ModInstallService(instMgrMock.Object, modrinthMock.Object, Options.Create(TestOptions), NullLogger<ModInstallService>.Instance);
        var result = await svc.InstallFromModrinthAsync("inst-1", "mod-a", resolveDependencies: true);

        result.Success.Should().BeTrue();
        result.InstalledDependencies.Should().BeEmpty();
    }

    // ── DL-redirect: Chống redirect sang host ngoài cdn.modrinth.com ──

    [Fact]
    public async Task DL_RedirectToUnauthorizedHost_ThrowsInvalidOperationException()
    {
        var path = "/cdn-redirect-test.jar";
        _handler.Map(HttpMethod.Get, path, req =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            resp.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://malicious.evil.com/payload.jar");
            resp.Content = new StringContent("malicious");
            return resp;
        });

        var act = () => _client.DownloadAsync($"https://cdn.modrinth.com{path}");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Redirected to unauthorized host*");
    }

    // ── DL-partial: Thất bại xóa sạch .partial ──

    [Fact]
    public async Task DL_FailedSha512_CleansUpPartialFile()
    {
        var instancePath = Path.Combine(_tempDir, "inst-partial-cleanup");
        var modsDir = Path.Combine(instancePath, "mods");
        Directory.CreateDirectory(modsDir);

        var instMgrMock = new Mock<IInstanceManager>();
        instMgrMock.Setup(m => m.GetAsync("inst-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MinecraftInstance { Id = "inst-1", Name = "Test", Path = instancePath, LoaderType = "fabric", MinecraftVersion = "1.21.1", CreatedAt = DateTimeOffset.UtcNow });

        var modrinthMock = new Mock<IModrinthClient>();
        var proj = new ModrinthProject { Id = "pBad", Slug = "bad-hash", Title = "Bad Hash Mod", ProjectType = "mod" };
        var ver = new ProjectVersion
        {
            Id = "vBad",
            ProjectId = "pBad",
            VersionType = "release",
            DatePublished = DateTimeOffset.UtcNow,
            Files = [new VersionFile
            {
                Url = "https://cdn.modrinth.com/bad.jar",
                Filename = "bad.jar",
                Primary = true,
                Hashes = new FileHashes { Sha512 = "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" }
            }],
            Dependencies = []
        };

        modrinthMock.Setup(m => m.GetProjectAsync("bad-hash", It.IsAny<CancellationToken>())).ReturnsAsync(proj);
        modrinthMock.Setup(m => m.GetVersionsAsync("bad-hash", "fabric", "1.21.1", It.IsAny<CancellationToken>())).ReturnsAsync([ver]);
        modrinthMock.Setup(m => m.DownloadAsync(ver.Files[0].Url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("content-with-different-hash")));

        var svc = new ModInstallService(instMgrMock.Object, modrinthMock.Object, Options.Create(TestOptions), NullLogger<ModInstallService>.Instance);
        var act = () => svc.InstallFromModrinthAsync("inst-1", "bad-hash");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*SHA512 mismatch*");

        // Xác nhận .partial và file đích KHÔNG còn trên đĩa
        File.Exists(Path.Combine(modsDir, "bad.jar.partial")).Should().BeFalse();
        File.Exists(Path.Combine(modsDir, "bad.jar")).Should().BeFalse();
    }

    // ── Mrpack: Untrusted download host blocked ──

    [Fact]
    public async Task Mrpack_UntrustedDownloadHost_ThrowsAndCleansUp()
    {
        var instancePath = Path.Combine(_tempDir, "inst-mrpack-sec");
        Directory.CreateDirectory(instancePath);

        // Tạo fake mrpack zip với file download từ evil.com
        var mrpackZipPath = Path.Combine(_tempDir, "evil.mrpack");
        using (var archive = System.IO.Compression.ZipFile.Open(mrpackZipPath, System.IO.Compression.ZipArchiveMode.Create))
        {
            var indexEntry = archive.CreateEntry("modrinth.index.json");
            using var writer = new StreamWriter(indexEntry.Open(), Encoding.UTF8);
            writer.Write("""
            {
                "formatVersion": 1,
                "game": "minecraft",
                "versionId": "1.0",
                "name": "EvilPack",
                "files": [
                    {
                        "path": "mods/malicious.jar",
                        "hashes": { "sha1": "abc" },
                        "downloads": ["https://evil-untrusted.com/malicious.jar"]
                    }
                ],
                "dependencies": { "minecraft": "1.21.1", "fabric-loader": "0.16.0" }
            }
            """);
        }

        var instMgrMock = new Mock<IInstanceManager>();
        instMgrMock.Setup(m => m.CreateFabricAsync("EvilPack", "1.21.1", "0.16.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MinecraftInstance { Id = "mrpack-1", Name = "EvilPack", Path = instancePath, LoaderType = "fabric", MinecraftVersion = "1.21.1", CreatedAt = DateTimeOffset.UtcNow });

        var httpFactoryMock = new Mock<IHttpClientFactory>();
        var importer = new MrpackImporter(instMgrMock.Object, httpFactoryMock.Object, NullLogger<MrpackImporter>.Instance);
        var act = () => importer.ImportAsync(mrpackZipPath, "EvilPack");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Blocked download from untrusted host in mrpack*");
    }
}
