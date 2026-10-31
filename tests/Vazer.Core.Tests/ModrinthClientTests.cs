using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vazer.Core.Modrinth;
using Vazer.Core.Tests.TestHelpers;

namespace Vazer.Core.Tests;

public sealed class ModrinthClientTests : IDisposable
{
    private readonly FakeHttpMessageHandler _handler;
    private readonly ModrinthClient _client;

    private static readonly MinecraftOptions TestOptions = new()
    {
        ModrinthUserAgent = "TestAgent/1.0 (test@test.com)"
    };

    public ModrinthClientTests()
    {
        _handler = new FakeHttpMessageHandler();
        var factory = new FakeHttpClientFactory(_handler);
        _client = new ModrinthClient(
            factory,
            NullLogger<ModrinthClient>.Instance,
            Options.Create(TestOptions));
    }

    public void Dispose() { /* handler has no unmanaged resources */ }

    // ── 1. SearchAsync_BuildsFacetsCorrectly ──

    [Fact]
    public async Task SearchAsync_BuildsFacetsCorrectly()
    {
        var query = new SearchQuery
        {
            Query = "sodium",
            Loaders = ["fabric"],
            GameVersions = ["1.21.1"],
            Categories = ["optimization"],
            Limit = 10
        };

        // Build the exact path the client will request so we can map it
        var facets = ModrinthClient.BuildFacets(query);
        var pathAndQuery = $"/v2/search?query=sodium&limit=10&offset=0&facets={Uri.EscapeDataString(facets)}";

        _handler.MapJson(HttpMethod.Get, pathAndQuery, HttpStatusCode.OK,
            """{"hits":[],"total_hits":0,"offset":0,"limit":10}""");

        await _client.SearchAsync(query);

        var decoded = Uri.UnescapeDataString(pathAndQuery);
        decoded.Should().Contain("[\"project_type:mod\"]");
        decoded.Should().Contain("[\"categories:fabric\"]");
        decoded.Should().Contain("[\"versions:1.21.1\"]");
        decoded.Should().Contain("[\"categories:optimization\"]");
    }

    // ── 2. GetVersionsAsync_FiltersLoaderAndGameVersion ──

    [Fact]
    public async Task GetVersionsAsync_FiltersLoaderAndGameVersion()
    {
        var path = "/v2/project/sodium/version?loaders=[%22fabric%22]&game_versions=[%221.21.1%22]";
        _handler.MapJson(HttpMethod.Get, path, HttpStatusCode.OK, "[]");

        var result = await _client.GetVersionsAsync("sodium", "fabric", "1.21.1");
        result.Should().BeEmpty();
        _handler.CallCount(HttpMethod.Get, path).Should().Be(1);
    }

    // ── 3. SearchAsync_ParsesPagination ──

    [Fact]
    public async Task SearchAsync_ParsesPagination()
    {
        var query = new SearchQuery { Query = "test", Limit = 5, Offset = 10 };
        var facets = ModrinthClient.BuildFacets(query);
        var path = $"/v2/search?query=test&limit=5&offset=10&facets={Uri.EscapeDataString(facets)}";

        _handler.MapJson(HttpMethod.Get, path, HttpStatusCode.OK,
            """{"hits":[{"project_id":"abc","slug":"test","title":"Test","description":"d","author":"a","downloads":100,"follows":5,"categories":[],"versions":[],"project_type":"mod"}],"total_hits":42,"offset":10,"limit":5}""");

        var result = await _client.SearchAsync(query);
        result.TotalHits.Should().Be(42);
        result.Offset.Should().Be(10);
        result.Limit.Should().Be(5);
        result.Hits.Should().HaveCount(1);
        result.Hits[0].ProjectId.Should().Be("abc");
    }

    // ── 4. Retry429_ThrowsRateLimitException_WithoutAutoRetry ──

    [Fact]
    public async Task Retry429_ThrowsRateLimitException_WithoutAutoRetry()
    {
        var query = new SearchQuery { Query = "retry" };
        var facets = ModrinthClient.BuildFacets(query);
        var path = $"/v2/search?query=retry&limit=20&offset=0&facets={Uri.EscapeDataString(facets)}";

        _handler.Map(HttpMethod.Get, path, _ =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            resp.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(2));
            return resp;
        });

        var act = () => _client.SearchAsync(query);
        var ex = await act.Should().ThrowAsync<ModrinthRateLimitException>();
        ex.Which.RetryAfterSeconds.Should().Be(2);
        _handler.CallCount(HttpMethod.Get, path).Should().Be(1); // 0 auto retry for 429
    }

    // ── 5. Cache_SecondCallWithinTTL_HitsCache ──

    [Fact]
    public async Task Cache_SecondCallWithinTTL_HitsCache()
    {
        var query = new SearchQuery { Query = "cached", Limit = 5 };
        var facets = ModrinthClient.BuildFacets(query);
        var path = $"/v2/search?query=cached&limit=5&offset=0&facets={Uri.EscapeDataString(facets)}";

        _handler.MapJson(HttpMethod.Get, path, HttpStatusCode.OK,
            """{"hits":[],"total_hits":0,"offset":0,"limit":5}""");

        await _client.SearchAsync(query);
        await _client.SearchAsync(query);

        _handler.CallCount(HttpMethod.Get, path).Should().Be(1);
    }

    // ── 6. UserAgent_PresentOnEveryRequest ──

    [Fact]
    public async Task UserAgent_PresentOnEveryRequest()
    {
        var path = "/v2/project/sodium";
        _handler.MapJson(HttpMethod.Get, path, HttpStatusCode.OK,
            """{"id":"abc","slug":"sodium","title":"Sodium","description":"d","body":"b","downloads":0,"followers":0,"categories":[],"loaders":[],"game_versions":[],"versions":[],"project_type":"mod"}""");

        await _client.GetProjectAsync("sodium");

        var req = _handler.Requests.Last();
        var ua = req.Headers.UserAgent.ToString();
        ua.Should().Contain("TestAgent/1.0");
    }

    // ── 7. RoundTrip_SearchHit_SnakeCase ──

    [Fact]
    public void RoundTrip_SearchHit_SnakeCase()
    {
        var hit = new SearchHit
        {
            ProjectId = "p1",
            Slug = "sodium",
            Title = "Sodium",
            Description = "Fast rendering",
            Author = "CaffeineMC",
            IconUrl = "https://example.com/icon.png",
            Downloads = 5_000_000,
            Follows = 1234,
            Categories = ["optimization"],
            Versions = ["1.21.1"],
            ProjectType = "mod",
            LatestVersion = "v1",
            DateModified = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };

        var json = JsonSerializer.Serialize(hit);
        json.Should().Contain("project_id");
        json.Should().Contain("icon_url");
        json.Should().NotContain("total_hits"); // not present in SearchHit
        json.Should().Contain("project_type");

        var roundTripped = JsonSerializer.Deserialize<SearchHit>(json)!;
        roundTripped.ProjectId.Should().Be("p1");
        roundTripped.Slug.Should().Be("sodium");
        roundTripped.Downloads.Should().Be(5_000_000);
        roundTripped.Categories.Should().Contain("optimization");
        roundTripped.DateModified.Should().NotBeNull();
    }

    // ── 8. RoundTrip_License_Object ──

    [Fact]
    public void RoundTrip_License_Object()
    {
        var license = new ModrinthLicense
        {
            Id = "MIT",
            Name = "MIT License",
            Url = "https://opensource.org/licenses/MIT"
        };

        var json = JsonSerializer.Serialize(license);
        var roundTripped = JsonSerializer.Deserialize<ModrinthLicense>(json)!;

        roundTripped.Id.Should().Be("MIT");
        roundTripped.Name.Should().Be("MIT License");
        roundTripped.Url.Should().Be("https://opensource.org/licenses/MIT");
    }

    // ── 9. SM-08: EmptyQuery_ReturnsEmpty_WithoutHttp ──

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyQuery_ReturnsEmpty_WithoutHttp(string? emptyQuery)
    {
        var query = new SearchQuery { Query = emptyQuery, Limit = 20 };
        var result = await _client.SearchAsync(query);

        result.Hits.Should().BeEmpty();
        result.TotalHits.Should().Be(0);
        result.Limit.Should().Be(20);
        _handler.Requests.Should().BeEmpty(); // Khong goi HTTP request nao
    }

    // ── 9b. GetFeaturedModsAsync Tests ──

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetFeaturedModsAsync_WithEmptyQuery_CallsHttpWithDownloadsSort(string? emptyQuery)
    {
        var query = new SearchQuery { Query = emptyQuery, Limit = 20 };
        var facets = ModrinthClient.BuildFacets(query);
        var expectedPath = $"/v2/search?query=&limit=20&offset=0&index=downloads&facets={Uri.EscapeDataString(facets)}";

        _handler.MapJson(HttpMethod.Get, expectedPath, HttpStatusCode.OK,
            """{"hits":[{"project_id":"p1","slug":"sodium","title":"Sodium","description":"d","author":"a","downloads":1000000,"follows":100,"categories":[],"versions":[],"project_type":"mod"}],"total_hits":1,"offset":0,"limit":20}""");

        var result = await _client.GetFeaturedModsAsync(query);

        result.Hits.Should().HaveCount(1);
        result.Hits[0].Slug.Should().Be("sodium");
        result.TotalHits.Should().Be(1);
        _handler.CallCount(HttpMethod.Get, expectedPath).Should().Be(1);
    }

    [Fact]
    public async Task GetFeaturedModsAsync_WithCustomSort_PreservesSortIndex()
    {
        var query = new SearchQuery { Query = null, SortIndex = "updated", Limit = 10 };
        var facets = ModrinthClient.BuildFacets(query);
        var expectedPath = $"/v2/search?query=&limit=10&offset=0&index=updated&facets={Uri.EscapeDataString(facets)}";

        _handler.MapJson(HttpMethod.Get, expectedPath, HttpStatusCode.OK,
            """{"hits":[],"total_hits":0,"offset":0,"limit":10}""");

        var result = await _client.GetFeaturedModsAsync(query);

        result.Limit.Should().Be(10);
        _handler.CallCount(HttpMethod.Get, expectedPath).Should().Be(1);
    }

    [Fact]
    public async Task GetFeaturedModsAsync_WithFacetsAndPagination_BuildsCorrectUrl()
    {
        var query = new SearchQuery
        {
            Query = null,
            Loaders = ["fabric"],
            GameVersions = ["1.21.1"],
            Limit = 20,
            Offset = 40
        };
        var facets = ModrinthClient.BuildFacets(query);
        var expectedPath = $"/v2/search?query=&limit=20&offset=40&index=downloads&facets={Uri.EscapeDataString(facets)}";

        _handler.MapJson(HttpMethod.Get, expectedPath, HttpStatusCode.OK,
            """{"hits":[],"total_hits":150,"offset":40,"limit":20}""");

        var result = await _client.GetFeaturedModsAsync(query);

        result.Offset.Should().Be(40);
        result.TotalHits.Should().Be(150);
        _handler.CallCount(HttpMethod.Get, expectedPath).Should().Be(1);

        var decoded = Uri.UnescapeDataString(expectedPath);
        decoded.Should().Contain("[\"categories:fabric\"]");
        decoded.Should().Contain("[\"versions:1.21.1\"]");
    }

    [Fact]
    public async Task GetFeaturedModsAsync_ClampsLimit_Between1And100()
    {
        var query = new SearchQuery { Query = "", Limit = 500 };
        var facets = ModrinthClient.BuildFacets(query);
        var expectedPath = $"/v2/search?query=&limit=100&offset=0&index=downloads&facets={Uri.EscapeDataString(facets)}";

        _handler.MapJson(HttpMethod.Get, expectedPath, HttpStatusCode.OK,
            """{"hits":[],"total_hits":0,"offset":0,"limit":100}""");

        var result = await _client.GetFeaturedModsAsync(query);

        _handler.CallCount(HttpMethod.Get, expectedPath).Should().Be(1);
    }

    // ── 10. SM-13/14: LimitClamp_Boundaries ──

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    [InlineData(500, 100)]
    public void LimitClamp_SearchQuery_ClampsBetween1And100(int input, int expected)
    {
        var query = new SearchQuery { Limit = input };
        query.Limit.Should().Be(expected);
    }

    // ── 11. ER-1..4: ModrinthErrorMapper Tests ──

    [Fact]
    public void ErrorMapper_429_WithRetryAfter_ReturnsRateLimited()
    {
        var ex = new ModrinthRateLimitException("Too Many Requests", retryAfterSeconds: 2);
        var info = ModrinthErrorMapper.Map(ex);

        info.ErrorType.Should().Be(ModrinthErrorType.RateLimited);
        info.CanRetry.Should().BeTrue();
        info.RetryAfterSeconds.Should().Be(2);
        info.Message.Should().Be("Bạn thao tác quá nhanh. Vui lòng đợi một lát và thử lại.");
    }

    [Fact]
    public void ErrorMapper_500_ReturnsServer()
    {
        var ex = new HttpRequestException("Server error", null, HttpStatusCode.InternalServerError);
        var info = ModrinthErrorMapper.Map(ex);

        info.ErrorType.Should().Be(ModrinthErrorType.Server);
        info.CanRetry.Should().BeTrue();
        info.Message.Should().Be("Máy chủ đang gặp sự cố. Vui lòng thử lại sau.");
    }

    [Fact]
    public void ErrorMapper_Timeout_ReturnsTimeout()
    {
        var ex = new TimeoutException("Operation timed out");
        var info = ModrinthErrorMapper.Map(ex);

        info.ErrorType.Should().Be(ModrinthErrorType.Timeout);
        info.CanRetry.Should().BeTrue();
        info.Message.Should().Be("Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối Internet của bạn và thử lại.");
    }

    [Fact]
    public void ErrorMapper_Network_ReturnsNetwork()
    {
        var ex = new HttpRequestException("Connection refused");
        var info = ModrinthErrorMapper.Map(ex);

        info.ErrorType.Should().Be(ModrinthErrorType.Network);
        info.CanRetry.Should().BeTrue();
        info.Message.Should().Be("Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối Internet của bạn và thử lại.");
    }

    // ── 12. PH-2/3: PaginationState Tests ──

    [Fact]
    public void PaginationState_TracksTwoPages_Correctly()
    {
        var state = new PaginationState();
        state.Reset(limit: 20);

        state.Offset.Should().Be(0);
        state.Limit.Should().Be(20);
        state.HasMore.Should().BeFalse();

        // Trang 1: 20 ket qua, tong cong 35
        state.Update(hitsCount: 20, totalHits: 35);
        state.Offset.Should().Be(20);
        state.Total.Should().Be(35);
        state.HasMore.Should().BeTrue();

        // Trang 2: 15 ket qua tiep theo, tong cong 35
        state.Update(hitsCount: 15, totalHits: 35);
        state.Offset.Should().Be(35);
        state.Total.Should().Be(35);
        state.HasMore.Should().BeFalse(); // Da het trang
    }
}
