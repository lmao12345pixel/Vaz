using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Vazer.Core.Abstractions;
using Vazer.Core.Auth;

namespace Vazer.Core.Tests;

/// <summary>
/// Item 5: CompositeAuthProvider dieu phoi login/refresh/logout giua cac provider con + store.
/// Khong network, provider va store deu mock.
/// </summary>
public sealed class CompositeAuthProviderTests
{
    private readonly Mock<IAuthProvider> _msProvider;
    private readonly Mock<IAuthProvider> _offlineProvider;
    private readonly Mock<IAccountStore> _store;
    private readonly CompositeAuthProvider _sut;

    public CompositeAuthProviderTests()
    {
        _msProvider = new Mock<IAuthProvider>();
        _msProvider.SetupGet(p => p.Kind).Returns(AccountKind.Microsoft);
        _offlineProvider = new Mock<IAuthProvider>();
        _offlineProvider.SetupGet(p => p.Kind).Returns(AccountKind.Offline);

        _store = new Mock<IAccountStore>();
        _sut = new CompositeAuthProvider(
            new[] { _msProvider.Object, _offlineProvider.Object },
            _store.Object,
            NullLogger<CompositeAuthProvider>.Instance);
    }

    private static MinecraftAccount Account(AccountKind kind, string id, DateTimeOffset? expiresAt) => new()
    {
        Id = id,
        DisplayName = "Player" + id,
        Kind = kind,
        AccessToken = "token-" + id,
        RefreshToken = "refresh-" + id,
        ExpiresAt = expiresAt
    };

    [Fact]
    public void GetProvider_ReturnsMatchingProvider()
    {
        _sut.GetProvider(AccountKind.Microsoft).Should().BeSameAs(_msProvider.Object);
        _sut.GetProvider(AccountKind.Offline).Should().BeSameAs(_offlineProvider.Object);
    }

    [Fact]
    public void GetProvider_UnknownKind_Throws()
    {
        var empty = new CompositeAuthProvider(
            Array.Empty<IAuthProvider>(), _store.Object, NullLogger<CompositeAuthProvider>.Instance);
        Assert.Throws<InvalidOperationException>(() => empty.GetProvider(AccountKind.Microsoft));
    }

    [Fact]
    public async Task LoginAndStoreAsync_SavesAndSetsActive()
    {
        var account = Account(AccountKind.Offline, "offline-1", DateTimeOffset.UtcNow.AddHours(1));
        _offlineProvider.Setup(p => p.LoginAsync(It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var result = await _sut.LoginAndStoreAsync(AccountKind.Offline);

        result.Should().Be(account);
        _store.Verify(s => s.SaveAsync(account, It.IsAny<CancellationToken>()), Times.Once);
        _store.Verify(s => s.SetActiveAsync(account.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_DefaultsToMicrosoft()
    {
        var account = Account(AccountKind.Microsoft, "ms-1", DateTimeOffset.UtcNow.AddHours(1));
        _msProvider.Setup(p => p.LoginAsync(It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var result = await _sut.LoginAsync();

        result.Should().Be(account);
        _store.Verify(s => s.SaveAsync(account, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TrySilentLoginAsync_UnknownAccount_ReturnsNull()
    {
        _store.Setup(s => s.GetAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((MinecraftAccount?)null);

        (await _sut.TrySilentLoginAsync("missing")).Should().BeNull();
        _msProvider.Verify(p => p.RefreshAsync(It.IsAny<MinecraftAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TrySilentLoginAsync_ValidToken_ReturnsWithoutRefresh()
    {
        var account = Account(AccountKind.Microsoft, "ms-1", DateTimeOffset.UtcNow.AddHours(1));
        _store.Setup(s => s.GetAsync(account.Id, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        var result = await _sut.TrySilentLoginAsync(account.Id);

        result.Should().Be(account);
        _msProvider.Verify(p => p.RefreshAsync(It.IsAny<MinecraftAccount>(), It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(s => s.SaveAsync(It.IsAny<MinecraftAccount>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TrySilentLoginAsync_ExpiredToken_RefreshesAndSaves()
    {
        var stale = Account(AccountKind.Microsoft, "ms-1", DateTimeOffset.UtcNow.AddHours(-1));
        var fresh = stale.WithTokens("new-token", "new-refresh", DateTimeOffset.UtcNow.AddHours(1));
        _store.Setup(s => s.GetAsync(stale.Id, It.IsAny<CancellationToken>())).ReturnsAsync(stale);
        _msProvider.Setup(p => p.RefreshAsync(stale, It.IsAny<CancellationToken>())).ReturnsAsync(fresh);

        var result = await _sut.TrySilentLoginAsync(stale.Id);

        result.Should().Be(fresh);
        _store.Verify(s => s.SaveAsync(fresh, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TrySilentLoginAsync_RefreshFails_ReturnsNull()
    {
        var stale = Account(AccountKind.Microsoft, "ms-1", DateTimeOffset.UtcNow.AddHours(-1));
        _store.Setup(s => s.GetAsync(stale.Id, It.IsAny<CancellationToken>())).ReturnsAsync(stale);
        _msProvider.Setup(p => p.RefreshAsync(stale, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("refresh revoked"));

        (await _sut.TrySilentLoginAsync(stale.Id)).Should().BeNull();
    }

    [Fact]
    public async Task RefreshAsync_DelegatesAndSaves()
    {
        var stale = Account(AccountKind.Offline, "off-1", DateTimeOffset.UtcNow.AddHours(-1));
        var fresh = stale.WithTokens("t2", null, DateTimeOffset.UtcNow.AddHours(1));
        _offlineProvider.Setup(p => p.RefreshAsync(stale, It.IsAny<CancellationToken>())).ReturnsAsync(fresh);

        var result = await _sut.RefreshAsync(stale);

        result.Should().Be(fresh);
        _store.Verify(s => s.SaveAsync(fresh, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_ClearsTokensAndSaves()
    {
        var account = Account(AccountKind.Microsoft, "ms-1", DateTimeOffset.UtcNow.AddHours(1));

        await _sut.LogoutAsync(account);

        _msProvider.Verify(p => p.LogoutAsync(account, It.IsAny<CancellationToken>()), Times.Once);
        _store.Verify(s => s.SaveAsync(
            It.Is<MinecraftAccount>(a =>
                a.Id == account.Id &&
                a.AccessToken == null &&
                a.RefreshToken == null &&
                a.ExpiresAt == null &&
                a.AccessTokenProtected == null &&
                a.RefreshTokenProtected == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
