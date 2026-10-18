using System.Net;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Vazer.Core;
using Vazer.Core.Abstractions;
using Vazer.Core.Auth;
using Vazer.Core.Tests.TestHelpers;

namespace Vazer.Core.Tests;

/// <summary>
/// Phase 1.7 TASK2: MsalAuthProvider OAuth chain with mocked HTTP + mocked MSAL.
/// No real network, no interactive UI. Tokens in this file are fake fixtures.
/// </summary>
public sealed class MsalAuthProviderTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly FakeHttpMessageHandler _http;
    private readonly FakeMsalClientWrapper _msal;

    private const string MsToken = "ms-token-abc";
    private const string XblToken = "xbl-tok-xyz";
    private const string Uhs = "uhs123";
    private const string XstsToken = "xsts-tok-xyz";
    private const string McToken = "mc-access-tok";
    private const string McUuid = "0123456789abcdef0123456789abcdef";
    private const string McName = "Steve";

    public MsalAuthProviderTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "upco-mc-msal-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _http = new FakeHttpMessageHandler();
        _msal = new FakeMsalClientWrapper();
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, true); } catch { }
    }

    private MsalAuthProvider CreateProvider(int maxRetries = 0)
    {
        var options = Options.Create(new MinecraftOptions
        {
            RootPath = _tempRoot,
            MsalClientId = "test-client-id",
            HttpTimeoutSeconds = 30,
            HttpMaxRetries = maxRetries
        });
        return new MsalAuthProvider(
            new FakeHttpClientFactory(_http),
            NullLogger<MsalAuthProvider>.Instance,
            options,
            _msal);
    }

    /// <summary>
    /// Provider cho che do OfflineDemoMode=true: MsalClientId rong, wrapper null
    /// (appsettings offline-only — graph phai resolve duoc, login MSAL throw message ro rang).
    /// </summary>
    private MsalAuthProvider CreateOfflineProvider()
    {
        var options = Options.Create(new MinecraftOptions
        {
            RootPath = _tempRoot,
            MsalClientId = string.Empty,
            OfflineDemoMode = true,
            HttpTimeoutSeconds = 30
        });
        return new MsalAuthProvider(
            new FakeHttpClientFactory(_http),
            NullLogger<MsalAuthProvider>.Instance,
            options,
            null);
    }

    private void MapHappyPath()
    {
        _http.MapJson(HttpMethod.Post, "/user/authenticate", HttpStatusCode.OK,
            /*lang=json*/ """{"Token":"xbl-tok-xyz","DisplayClaims":{"xui":[{"uhs":"uhs123"}]}}""");
        _http.MapJson(HttpMethod.Post, "/xsts/authorize", HttpStatusCode.OK,
            /*lang=json*/ """{"Token":"xsts-tok-xyz","DisplayClaims":{"xui":[{"uhs":"uhs123"}]}}""");
        _http.MapJson(HttpMethod.Post, "/authentication/login_with_xbox", HttpStatusCode.OK,
            /*lang=json*/ """{"access_token":"mc-access-tok","expires_in":86400}""");
        _http.MapJson(HttpMethod.Get, "/minecraft/profile", HttpStatusCode.OK,
            /*lang=json*/ """{"id":"0123456789abcdef0123456789abcdef","name":"Steve"}""");
    }

    [Fact]
    public async Task LoginAsync_HappyPath_Returns_Account()
    {
        MapHappyPath();
        _msal.InteractiveResult = new MsalTokenResult
        {
            AccessToken = MsToken,
            Username = "user@example.com",
            HomeAccountId = "home-1"
        };

        var provider = CreateProvider();
        var before = DateTimeOffset.UtcNow;
        var acc = await provider.LoginAsync();

        acc.Id.Should().Be(McUuid);
        acc.DisplayName.Should().Be(McName);
        acc.Kind.Should().Be(AccountKind.Microsoft);
        acc.Uuid.Should().Be(McUuid);
        acc.AccessToken.Should().Be(McToken);
        acc.ExpiresAt.Should().BeCloseTo(before.AddSeconds(86400), TimeSpan.FromMinutes(2));

        // Request body contracts (tokens stay in fixtures, never logged).
        var xblBody = JsonNode.Parse(_http.RequestBodies["POST /user/authenticate"])!;
        xblBody["properties"]!["rpsTicket"]!.GetValue<string>().Should().Be($"d={MsToken}");

        var mcBody = JsonNode.Parse(_http.RequestBodies["POST /authentication/login_with_xbox"])!;
        mcBody["identityToken"]!.GetValue<string>().Should().Be($"XBL3.0 x={Uhs};{XstsToken}");
    }

    [Fact]
    public async Task LoginAsync_XboxLive401_Throws_Clear_Message()
    {
        _http.MapJson(HttpMethod.Post, "/user/authenticate", HttpStatusCode.Unauthorized, "{}");
        _msal.InteractiveResult = new MsalTokenResult
        {
            AccessToken = MsToken,
            Username = "user@example.com",
            HomeAccountId = "home-1"
        };

        var provider = CreateProvider();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.LoginAsync());
        ex.Message.Should().Contain("401");
    }

    [Theory]
    [InlineData(2148916233u, "Xbox")]
    [InlineData(2148916235u, "Region")]
    [InlineData(2148916238u, "parental")]
    public async Task LoginAsync_Xsts_Errors_Throw_Clear_Messages(uint xerr, string expectedFragment)
    {
        _http.MapJson(HttpMethod.Post, "/user/authenticate", HttpStatusCode.OK,
            /*lang=json*/ """{"Token":"xbl-tok-xyz","DisplayClaims":{"xui":[{"uhs":"uhs123"}]}}""");
        _http.MapJson(HttpMethod.Post, "/xsts/authorize", HttpStatusCode.OK,
            /*lang=json*/ $$"""{"XErr":{{xerr}}}""");
        _msal.InteractiveResult = new MsalTokenResult
        {
            AccessToken = MsToken,
            Username = "user@example.com",
            HomeAccountId = "home-1"
        };

        var provider = CreateProvider();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.LoginAsync());
        ex.Message.Should().Contain(expectedFragment);
    }

    [Fact]
    public async Task LoginAsync_Profile404_Throws_NoOwnership_Message()
    {
        _http.MapJson(HttpMethod.Post, "/user/authenticate", HttpStatusCode.OK,
            /*lang=json*/ """{"Token":"xbl-tok-xyz","DisplayClaims":{"xui":[{"uhs":"uhs123"}]}}""");
        _http.MapJson(HttpMethod.Post, "/xsts/authorize", HttpStatusCode.OK,
            /*lang=json*/ """{"Token":"xsts-tok-xyz","DisplayClaims":{"xui":[{"uhs":"uhs123"}]}}""");
        _http.MapJson(HttpMethod.Post, "/authentication/login_with_xbox", HttpStatusCode.OK,
            /*lang=json*/ """{"access_token":"mc-access-tok","expires_in":86400}""");
        _http.MapJson(HttpMethod.Get, "/minecraft/profile", HttpStatusCode.NotFound, "{}");
        _msal.InteractiveResult = new MsalTokenResult
        {
            AccessToken = MsToken,
            Username = "user@example.com",
            HomeAccountId = "home-1"
        };

        var provider = CreateProvider();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.LoginAsync());
        ex.Message.Should().Contain("owns no Minecraft");
    }

    [Fact]
    public async Task LoginAsync_Retries_500_Then_Succeeds()
    {
        var calls = 0;
        _http.Map(HttpMethod.Post, "/user/authenticate", _ =>
        {
            calls++;
            return calls == 1
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        /*lang=json*/ """{"Token":"xbl-tok-xyz","DisplayClaims":{"xui":[{"uhs":"uhs123"}]}}""",
                        System.Text.Encoding.UTF8, "application/json")
                };
        });
        _http.MapJson(HttpMethod.Post, "/xsts/authorize", HttpStatusCode.OK,
            /*lang=json*/ """{"Token":"xsts-tok-xyz","DisplayClaims":{"xui":[{"uhs":"uhs123"}]}}""");
        _http.MapJson(HttpMethod.Post, "/authentication/login_with_xbox", HttpStatusCode.OK,
            /*lang=json*/ """{"access_token":"mc-access-tok","expires_in":86400}""");
        _http.MapJson(HttpMethod.Get, "/minecraft/profile", HttpStatusCode.OK,
            /*lang=json*/ """{"id":"0123456789abcdef0123456789abcdef","name":"Steve"}""");
        _msal.InteractiveResult = new MsalTokenResult
        {
            AccessToken = MsToken,
            Username = "user@example.com",
            HomeAccountId = "home-1"
        };

        var provider = CreateProvider(maxRetries: 1);
        var acc = await provider.LoginAsync();

        acc.DisplayName.Should().Be("Steve");
        _http.CallCount(HttpMethod.Post, "/user/authenticate").Should().Be(2);
    }

    [Fact]
    public async Task RefreshAsync_SilentSuccess_Replays_Chain()
    {
        MapHappyPath();
        _msal.SilentResult = new MsalTokenResult
        {
            AccessToken = "ms-refreshed",
            Username = "user@example.com",
            HomeAccountId = "home-1"
        };

        var provider = CreateProvider();
        var account = new MinecraftAccount
        {
            Id = McUuid,
            DisplayName = McName,
            Kind = AccountKind.Microsoft,
            Uuid = McUuid,
            AccessToken = "old",
            RefreshToken = "",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        var before = DateTimeOffset.UtcNow;
        var refreshed = await provider.RefreshAsync(account);

        refreshed.AccessToken.Should().Be(McToken);
        refreshed.Id.Should().Be(McUuid);
        refreshed.ExpiresAt.Should().BeCloseTo(before.AddSeconds(86400), TimeSpan.FromMinutes(2));

        var xblBody = JsonNode.Parse(_http.RequestBodies["POST /user/authenticate"])!;
        xblBody["properties"]!["rpsTicket"]!.GetValue<string>().Should().Be("d=ms-refreshed");
    }

    [Fact]
    public async Task RefreshAsync_MsalUiRequired_Rethrows()
    {
        _msal.SilentException = new MsalUiRequiredException("interaction_required", "Interaction required.");

        var provider = CreateProvider();
        var account = new MinecraftAccount
        {
            Id = McUuid,
            DisplayName = McName,
            Kind = AccountKind.Microsoft,
            Uuid = McUuid,
            AccessToken = "old",
            RefreshToken = "",
            ExpiresAt = null
        };

        await Assert.ThrowsAsync<MsalUiRequiredException>(() => provider.RefreshAsync(account));
    }

    [Fact]
    public void Ctor_OfflineDemoMode_EmptyClientId_DoesNotThrow()
    {
        // OfflineDemoMode=true + MsalClientId rong: phai resolve duoc (khong throw).
        var provider = CreateOfflineProvider();

        provider.Kind.Should().Be(AccountKind.Microsoft);
    }

    [Fact]
    public async Task LoginAsync_OfflineDemoMode_NullWrapper_Throws_Clear_Message()
    {
        var provider = CreateOfflineProvider();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.LoginAsync());
        ex.Message.Should().Contain("MSAL login requires MsalClientId");
    }

    [Fact]
    public async Task RefreshAsync_OfflineDemoMode_NullWrapper_Throws_Clear_Message()
    {
        var provider = CreateOfflineProvider();
        var account = new MinecraftAccount
        {
            Id = McUuid,
            DisplayName = McName,
            Kind = AccountKind.Microsoft,
            Uuid = McUuid,
            AccessToken = "old",
            RefreshToken = "",
            ExpiresAt = null
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.RefreshAsync(account));
        ex.Message.Should().Contain("MSAL login requires MsalClientId");
    }

    private sealed class FakeMsalClientWrapper : IMsalClientWrapper
    {
        public MsalTokenResult? InteractiveResult { get; set; }
        public MsalTokenResult? SilentResult { get; set; }
        public Exception? SilentException { get; set; }
        public List<string> RemovedHints { get; } = new();

        public Task<IReadOnlyList<MsalCachedAccount>> GetAccountsAsync(CancellationToken ct = default)
        {
            IReadOnlyList<MsalCachedAccount> list = Array.Empty<MsalCachedAccount>();
            return Task.FromResult(list);
        }

        public Task<MsalTokenResult> AcquireTokenInteractiveAsync(string[] scopes, CancellationToken ct = default)
        {
            return Task.FromResult(InteractiveResult
                ?? throw new InvalidOperationException("InteractiveResult not configured."));
        }

        public Task<MsalTokenResult> AcquireTokenSilentAsync(string[] scopes, string accountHint, CancellationToken ct = default)
        {
            if (SilentException != null)
            {
                return Task.FromException<MsalTokenResult>(SilentException);
            }
            return Task.FromResult(SilentResult
                ?? throw new MsalUiRequiredException("interaction_required", "Interaction required."));
        }

        public Task RemoveAccountAsync(string accountHint, CancellationToken ct = default)
        {
            RemovedHints.Add(accountHint);
            return Task.CompletedTask;
        }
    }
}
