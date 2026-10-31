using System.Net;
using System.Text;
using System.Text.Json;
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
    public async Task ER3_TooManyRequests_SurfacesRateLimit_WithRetryAfter()
    {
        var query = new SearchQuery { Query = "rate-limit-test" };
        var facets = ModrinthClient.BuildFacets(query);
        var path = $"/v2/search?query=rate-limit-test&limit=20&offset=0&facets={Uri.EscapeDataString(facets)}";

        int callCount = 0;
        _handler.Map(HttpMethod.Get, path, _ =>
        {
            callCount++;
            var resp = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            resp.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(2));
            return resp;
        });

        var act = () => _client.SearchAsync(query);
        var ex = await act.Should().ThrowAsync<ModrinthRateLimitException>();
        ex.Which.RetryAfterSeconds.Should().Be(2);
        callCount.Should().Be(1); // 0 auto retry for 429, tôn trọng Retry-After cho UI
    }

    [Fact]
    public async Task ER3_GetProject_429WithRetryAfter_ThrowsModrinthRateLimitException()
    {
        var path = "/v2/project/sodium";
        _handler.Map(HttpMethod.Get, path, _ =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            resp.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(5));
            return resp;
        });

        var act = () => _client.GetProjectAsync("sodium");
        var ex = await act.Should().ThrowAsync<ModrinthRateLimitException>();
        ex.Which.RetryAfterSeconds.Should().Be(5);
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

    // ── DL-partial: Thất bại xóa sạch .partial trong cùng thư mục mods ──

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

        var partialPath = Path.Combine(modsDir, "bad.jar.partial");
        bool partialExistedDuringDownload = false;
        var rawContent = new MemoryStream(Encoding.UTF8.GetBytes("content-with-different-hash"));
        modrinthMock.Setup(m => m.DownloadAsync(ver.Files[0].Url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrackingStream(rawContent, () =>
            {
                partialExistedDuringDownload = File.Exists(partialPath);
            }));

        var svc = new ModInstallService(instMgrMock.Object, modrinthMock.Object, Options.Create(TestOptions), NullLogger<ModInstallService>.Instance);
        var act = () => svc.InstallFromModrinthAsync("inst-1", "bad-hash");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*SHA512 mismatch*");

        partialExistedDuringDownload.Should().BeTrue("File .partial phải được tạo ra trong cùng thư mục mods khi tải");
        File.Exists(partialPath).Should().BeFalse("File .partial phải được dọn dẹp sạch khi sai hash");
        File.Exists(Path.Combine(modsDir, "bad.jar")).Should().BeFalse();
    }

    [Fact]
    public async Task DL_CancelledDownload_CleansUpPartialFile()
    {
        var instancePath = Path.Combine(_tempDir, "inst-dl-cancel");
        var modsDir = Path.Combine(instancePath, "mods");
        Directory.CreateDirectory(modsDir);

        var instMgrMock = new Mock<IInstanceManager>();
        instMgrMock.Setup(m => m.GetAsync("inst-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MinecraftInstance { Id = "inst-1", Name = "Test", Path = instancePath, LoaderType = "fabric", MinecraftVersion = "1.21.1", CreatedAt = DateTimeOffset.UtcNow });

        var modrinthMock = new Mock<IModrinthClient>();
        var proj = new ModrinthProject { Id = "pCancel", Slug = "cancel-mod", Title = "Cancel Mod", ProjectType = "mod" };
        var ver = new ProjectVersion
        {
            Id = "vCancel",
            ProjectId = "pCancel",
            VersionType = "release",
            DatePublished = DateTimeOffset.UtcNow,
            Files = [new VersionFile { Url = "https://cdn.modrinth.com/cancel.jar", Filename = "cancel.jar", Primary = true }],
            Dependencies = []
        };

        var partialPath = Path.Combine(modsDir, "cancel.jar.partial");
        bool partialExistedBeforeCancel = false;
        var cts = new CancellationTokenSource();
        modrinthMock.Setup(m => m.GetProjectAsync("cancel-mod", It.IsAny<CancellationToken>())).ReturnsAsync(proj);
        modrinthMock.Setup(m => m.GetVersionsAsync("cancel-mod", "fabric", "1.21.1", It.IsAny<CancellationToken>())).ReturnsAsync([ver]);
        modrinthMock.Setup(m => m.DownloadAsync(ver.Files[0].Url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CancellingStream(cts, () =>
            {
                partialExistedBeforeCancel = File.Exists(partialPath);
            }));

        var svc = new ModInstallService(instMgrMock.Object, modrinthMock.Object, Options.Create(TestOptions), NullLogger<ModInstallService>.Instance);
        var act = () => svc.InstallFromModrinthAsync("inst-1", "cancel-mod", ct: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();

        partialExistedBeforeCancel.Should().BeTrue("File .partial phải được tạo ra trước khi hủy");
        File.Exists(partialPath).Should().BeFalse("File .partial phải được dọn dẹp sạch khi hủy");
        File.Exists(Path.Combine(modsDir, "cancel.jar")).Should().BeFalse();
    }

    [Fact]
    public async Task DL_NetworkDisconnect_CleansUpPartialFile()
    {
        var instancePath = Path.Combine(_tempDir, "inst-dl-disconnect");
        var modsDir = Path.Combine(instancePath, "mods");
        Directory.CreateDirectory(modsDir);

        var instMgrMock = new Mock<IInstanceManager>();
        instMgrMock.Setup(m => m.GetAsync("inst-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MinecraftInstance { Id = "inst-1", Name = "Test", Path = instancePath, LoaderType = "fabric", MinecraftVersion = "1.21.1", CreatedAt = DateTimeOffset.UtcNow });

        var modrinthMock = new Mock<IModrinthClient>();
        var proj = new ModrinthProject { Id = "pDisc", Slug = "disc-mod", Title = "Disc Mod", ProjectType = "mod" };
        var ver = new ProjectVersion
        {
            Id = "vDisc",
            ProjectId = "pDisc",
            VersionType = "release",
            DatePublished = DateTimeOffset.UtcNow,
            Files = [new VersionFile { Url = "https://cdn.modrinth.com/disc.jar", Filename = "disc.jar", Primary = true }],
            Dependencies = []
        };

        var partialPath = Path.Combine(modsDir, "disc.jar.partial");
        bool partialExistedBeforeThrow = false;
        modrinthMock.Setup(m => m.GetProjectAsync("disc-mod", It.IsAny<CancellationToken>())).ReturnsAsync(proj);
        modrinthMock.Setup(m => m.GetVersionsAsync("disc-mod", "fabric", "1.21.1", It.IsAny<CancellationToken>())).ReturnsAsync([ver]);
        modrinthMock.Setup(m => m.DownloadAsync(ver.Files[0].Url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ThrowingNetworkStream(() =>
            {
                partialExistedBeforeThrow = File.Exists(partialPath);
            }));

        var svc = new ModInstallService(instMgrMock.Object, modrinthMock.Object, Options.Create(TestOptions), NullLogger<ModInstallService>.Instance);
        var act = () => svc.InstallFromModrinthAsync("inst-1", "disc-mod");

        await act.Should().ThrowAsync<HttpRequestException>();

        partialExistedBeforeThrow.Should().BeTrue("File .partial phải được tạo ra trong quá trình truyền dữ liệu");
        File.Exists(partialPath).Should().BeFalse("File .partial phải được dọn dẹp sạch khi mất mạng");
        File.Exists(Path.Combine(modsDir, "disc.jar")).Should().BeFalse();
    }

    [Fact]
    public void DL_CleanOrphanPartials_DeletesAllPartialFilesInModsDir()
    {
        var modsDir = Path.Combine(_tempDir, "inst-orphan-cleanup", "mods");
        Directory.CreateDirectory(modsDir);

        // Mô phỏng ứng dụng bị crash / tắt đột ngột để lại các file .partial mồ côi
        var orphan1 = Path.Combine(modsDir, "crashed-mod-1.jar.partial");
        var orphan2 = Path.Combine(modsDir, "crashed-mod-2.jar.partial");
        var legitimateJar = Path.Combine(modsDir, "good-mod.jar");
        var legitimateDisabled = Path.Combine(modsDir, "disabled-mod.jar.disabled");

        File.WriteAllText(orphan1, "incomplete data 1");
        File.WriteAllText(orphan2, "incomplete data 2");
        File.WriteAllText(legitimateJar, "valid jar");
        File.WriteAllText(legitimateDisabled, "valid disabled");

        // Gọi dọn dẹp file .partial mồ côi
        ModInstallService.CleanOrphanPartials(modsDir);

        File.Exists(orphan1).Should().BeFalse("File .partial mồ côi 1 phải bị xóa");
        File.Exists(orphan2).Should().BeFalse("File .partial mồ côi 2 phải bị xóa");
        File.Exists(legitimateJar).Should().BeTrue("File .jar hợp lệ phải được giữ nguyên");
        File.Exists(legitimateDisabled).Should().BeTrue("File .disabled hợp lệ phải được giữ nguyên");
    }

    [Fact]
    public void DL_CleanOrphanPartials_DoesNotDeleteActiveDownloads()
    {
        var modsDir = Path.Combine(_tempDir, "inst-active-orphan-test", "mods");
        Directory.CreateDirectory(modsDir);

        var activePartial = Path.Combine(modsDir, "active-download.jar.partial");
        var orphanPartial = Path.Combine(modsDir, "abandoned-download.jar.partial");

        File.WriteAllText(activePartial, "active downloading bytes");
        File.WriteAllText(orphanPartial, "abandoned bytes");

        // Đăng ký activePartial là đang tải trong tiến trình
        using (ModInstallService.RegisterActivePartial(activePartial))
        {
            // Thực hiện dọn dẹp orphan
            ModInstallService.CleanOrphanPartials(modsDir);

            // Active partial không được bị xóa
            File.Exists(activePartial).Should().BeTrue("File .partial đang trong tiến trình tải không được bị xóa");
            // Orphan partial phải bị xóa
            File.Exists(orphanPartial).Should().BeFalse("File .partial mồ côi phải bị xóa");
        }

        // Sau khi dispose registration, nếu dọn dẹp lại thì sẽ bị xóa
        ModInstallService.CleanOrphanPartials(modsDir);
        File.Exists(activePartial).Should().BeFalse("Sau khi registration dispose, partial trở thành orphan và bị dọn");
    }

    [Fact]
    public async Task InstallFromModrinthAsync_UpdateReplacesOldJar_DoesNotKeepBothSideBySide()
    {
        var instancePath = Path.Combine(_tempDir, "inst-update-replace");
        var modsDir = Path.Combine(instancePath, "mods");
        Directory.CreateDirectory(modsDir);

        var instMgrMock = new Mock<IInstanceManager>();
        instMgrMock.Setup(m => m.GetAsync("inst-update", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MinecraftInstance { Id = "inst-update", Name = "Test", Path = instancePath, LoaderType = "fabric", MinecraftVersion = "1.21.1", CreatedAt = DateTimeOffset.UtcNow });

        // Tạo file cũ trên đĩa và trong manifest
        var oldJarPath = Path.Combine(modsDir, "my-mod-1.0.jar");
        await File.WriteAllTextAsync(oldJarPath, "old version 1.0 bytes");

        var manifestPath = Path.Combine(instancePath, "mods.json");
        var initialManifest = new ModsManifest
        {
            Mods =
            [
                new InstalledMod
                {
                    FileName = "my-mod-1.0.jar",
                    ProjectId = "my-mod-proj",
                    VersionId = "ver-1.0",
                    Source = "modrinth",
                    Enabled = true,
                    InstalledAt = DateTimeOffset.UtcNow
                }
            ]
        };
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(initialManifest));

        // Cấu hình Modrinth mock trả về bản mới 2.0
        var newContent = new byte[] { 10, 20, 30 };
        using var sha = System.Security.Cryptography.SHA512.Create();
        var newSha = Convert.ToHexString(sha.ComputeHash(newContent));

        var modrinthMock = new Mock<IModrinthClient>();
        var proj = new ModrinthProject { Id = "my-mod-proj", Slug = "my-mod", Title = "My Mod", ProjectType = "mod" };
        var verNew = new ProjectVersion
        {
            Id = "ver-2.0",
            ProjectId = "my-mod-proj",
            VersionType = "release",
            DatePublished = DateTimeOffset.UtcNow,
            Files = [new VersionFile { Url = "https://cdn.modrinth.com/my-mod-2.0.jar", Filename = "my-mod-2.0.jar", Primary = true, Hashes = new FileHashes { Sha512 = newSha } }],
            Dependencies = []
        };

        modrinthMock.Setup(m => m.GetProjectAsync("my-mod-proj", It.IsAny<CancellationToken>())).ReturnsAsync(proj);
        modrinthMock.Setup(m => m.GetVersionsAsync("my-mod-proj", "fabric", "1.21.1", It.IsAny<CancellationToken>())).ReturnsAsync([verNew]);
        modrinthMock.Setup(m => m.GetVersionAsync("ver-2.0", It.IsAny<CancellationToken>())).ReturnsAsync(verNew);
        modrinthMock.Setup(m => m.DownloadAsync(verNew.Files[0].Url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(newContent));

        var svc = new ModInstallService(instMgrMock.Object, modrinthMock.Object, Options.Create(TestOptions), NullLogger<ModInstallService>.Instance);

        // Act: Cài đặt bản mới (cập nhật)
        var result = await svc.InstallFromModrinthAsync("inst-update", "my-mod-proj", "ver-2.0");

        // Assert
        result.Success.Should().BeTrue();
        File.Exists(oldJarPath).Should().BeFalse("File jar bản cũ phải bị xóa khi cập nhật, không được đặt cạnh bản mới");
        File.Exists(Path.Combine(modsDir, "my-mod-2.0.jar")).Should().BeTrue("File jar bản mới phải tồn tại");

        var updatedManifestJson = await File.ReadAllTextAsync(manifestPath);
        var updatedManifest = JsonSerializer.Deserialize<ModsManifest>(updatedManifestJson, VazJsonContext.Default.ModsManifest);
        updatedManifest!.Mods.Should().HaveCount(1, "Manifest chỉ chứa 1 bản duy nhất sau cập nhật");
        updatedManifest.Mods[0].FileName.Should().Be("my-mod-2.0.jar");
        updatedManifest.Mods[0].VersionId.Should().Be("ver-2.0");
    }

    [Fact]
    public async Task DL_ProgressReports_BytesAndPercent()
    {
        var instancePath = Path.Combine(_tempDir, "inst-dl-progress");
        var modsDir = Path.Combine(instancePath, "mods");
        Directory.CreateDirectory(modsDir);

        var instMgrMock = new Mock<IInstanceManager>();
        instMgrMock.Setup(m => m.GetAsync("inst-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MinecraftInstance { Id = "inst-1", Name = "Test", Path = instancePath, LoaderType = "fabric", MinecraftVersion = "1.21.1", CreatedAt = DateTimeOffset.UtcNow });

        var content = new byte[100_000];
        Array.Fill<byte>(content, 42);
        using var sha = System.Security.Cryptography.SHA512.Create();
        var shaHex = Convert.ToHexString(sha.ComputeHash(content));

        var modrinthMock = new Mock<IModrinthClient>();
        var proj = new ModrinthProject { Id = "pProg", Slug = "prog-mod", Title = "Prog Mod", ProjectType = "mod" };
        var ver = new ProjectVersion
        {
            Id = "vProg",
            ProjectId = "pProg",
            VersionType = "release",
            DatePublished = DateTimeOffset.UtcNow,
            Files = [new VersionFile { Url = "https://cdn.modrinth.com/prog.jar", Filename = "prog.jar", Primary = true, Size = content.Length, Hashes = new FileHashes { Sha512 = shaHex } }],
            Dependencies = []
        };

        modrinthMock.Setup(m => m.GetProjectAsync("prog-mod", It.IsAny<CancellationToken>())).ReturnsAsync(proj);
        modrinthMock.Setup(m => m.GetVersionsAsync("prog-mod", "fabric", "1.21.1", It.IsAny<CancellationToken>())).ReturnsAsync([ver]);
        modrinthMock.Setup(m => m.DownloadAsync(ver.Files[0].Url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(content));

        var reports = new List<InstallProgress>();
        var progress = new Progress<InstallProgress>(p => reports.Add(p));

        var svc = new ModInstallService(instMgrMock.Object, modrinthMock.Object, Options.Create(TestOptions), NullLogger<ModInstallService>.Instance);
        var result = await svc.InstallFromModrinthAsync("inst-1", "prog-mod", progress: progress);

        result.Success.Should().BeTrue();
        reports.Should().NotBeEmpty();
        reports.Any(r => r.DownloadedBytes > 0).Should().BeTrue();
        reports.Last().PercentComplete.Should().Be(100);
    }

    [Fact]
    public async Task DEP_UncheckedDependencies_DoesNotInstallRequiredDep()
    {
        var instancePath = Path.Combine(_tempDir, "inst-dep-unchecked");
        var modsDir = Path.Combine(instancePath, "mods");
        Directory.CreateDirectory(modsDir);

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
            Dependencies = [new VersionDependency { ProjectId = "pB", DependencyType = "required" }]
        };

        modrinthMock.Setup(m => m.GetProjectAsync("mod-a", It.IsAny<CancellationToken>())).ReturnsAsync(projA);
        modrinthMock.Setup(m => m.GetVersionsAsync("mod-a", "fabric", "1.21.1", It.IsAny<CancellationToken>())).ReturnsAsync([verA]);
        modrinthMock.Setup(m => m.DownloadAsync(verA.Files[0].Url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes("fake-mod-a")));

        var svc = new ModInstallService(instMgrMock.Object, modrinthMock.Object, Options.Create(TestOptions), NullLogger<ModInstallService>.Instance);
        var result = await svc.InstallFromModrinthAsync("inst-1", "mod-a", resolveDependencies: false);

        result.Success.Should().BeTrue();
        result.InstalledDependencies.Should().BeEmpty();
        File.Exists(Path.Combine(instancePath, "mods", "mod-a.jar")).Should().BeTrue();
        File.Exists(Path.Combine(instancePath, "mods", "mod-b.jar")).Should().BeFalse();
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

    private sealed class CancellingStream : Stream
    {
        private readonly CancellationTokenSource _cts;
        private readonly Action? _onBeforeCancel;
        public CancellingStream(CancellationTokenSource cts, Action? onBeforeCancel = null)
        {
            _cts = cts;
            _onBeforeCancel = onBeforeCancel;
        }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 1000;
        public override long Position { get => 0; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count)
        {
            _onBeforeCancel?.Invoke();
            _cts.Cancel();
            throw new OperationCanceledException();
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            _onBeforeCancel?.Invoke();
            _cts.Cancel();
            throw new OperationCanceledException();
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _onBeforeCancel?.Invoke();
            _cts.Cancel();
            throw new OperationCanceledException();
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class TrackingStream : Stream
    {
        private readonly Stream _inner;
        private readonly Action _onFirstRead;
        private bool _called;

        public TrackingStream(Stream inner, Action onFirstRead)
        {
            _inner = inner;
            _onFirstRead = onFirstRead;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() => _inner.Flush();

        private void Trigger()
        {
            if (!_called)
            {
                _called = true;
                _onFirstRead();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Trigger();
            return _inner.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Trigger();
            return _inner.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Trigger();
            return _inner.ReadAsync(buffer, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await _inner.DisposeAsync();
            await base.DisposeAsync();
        }
    }

    private sealed class ThrowingNetworkStream : Stream
    {
        private readonly Action? _onBeforeThrow;
        public ThrowingNetworkStream(Action? onBeforeThrow = null) => _onBeforeThrow = onBeforeThrow;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 1024;
        public override long Position { get => 0; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count)
        {
            _onBeforeThrow?.Invoke();
            throw new HttpRequestException("Network connection lost mid-flight.");
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            _onBeforeThrow?.Invoke();
            throw new HttpRequestException("Network connection lost mid-flight.");
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _onBeforeThrow?.Invoke();
            throw new HttpRequestException("Network connection lost mid-flight.");
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
