using System.Text.RegularExpressions;
using FluentAssertions;
using Vazer.Core.Auth;

namespace Vazer.Core.Tests;

/// <summary>Phase 1.6 TASK3: Offline UUID determinism + format.</summary>
public sealed class OfflineUuidTests
{
    [Fact]
    public void SameUsername_SameUuid()
    {
        var a = OfflineAuthProvider.GenerateOfflineUuid("Steve");
        var b = OfflineAuthProvider.GenerateOfflineUuid("Steve");
        a.Should().Be(b);
    }

    [Fact]
    public void DifferentUsername_DifferentUuid()
    {
        var a = OfflineAuthProvider.GenerateOfflineUuid("Alice");
        var b = OfflineAuthProvider.GenerateOfflineUuid("Bob");
        a.Should().NotBe(b);
    }

    [Fact]
    public async Task LoginAsync_ValidUsername_Returns_Offline_Account()
    {
        using var loggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        var provider = new OfflineAuthProvider(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<OfflineAuthProvider>.Instance);
        var acc = await provider.LoginAsync("Steve");
        acc.Kind.Should().Be(Vazer.Core.Abstractions.AccountKind.Offline);
        acc.DisplayName.Should().Be("Steve");
        acc.Id.Should().Be(OfflineAuthProvider.GenerateOfflineUuid("Steve"));
    }

    [Fact]
    public async Task LoginAsync_EmptyUsername_Throws()
    {
        var provider = new OfflineAuthProvider(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<OfflineAuthProvider>.Instance);
        await Assert.ThrowsAsync<ArgumentException>(() => provider.LoginAsync(""));
    }

    [Fact]
    public async Task Refresh_And_Logout_Offline_Are_Noop()
    {
        var provider = new OfflineAuthProvider(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<OfflineAuthProvider>.Instance);
        var acc = await provider.LoginAsync("Steve");
        (await provider.RefreshAsync(acc)).Should().BeSameAs(acc);
        await provider.LogoutAsync(acc); // should not throw
    }

    [Fact]
    public void Uuid_Format_Is32Hex_Version3_Rfc4122Variant()
    {
        var uuid = OfflineAuthProvider.GenerateOfflineUuid("Steve");
        uuid.Should().HaveLength(32);
        Regex.IsMatch(uuid, "^[0-9a-f]{32}$").Should().BeTrue();

        var guid = Guid.ParseExact(uuid, "N");
        var bytes = guid.ToByteArray();
        // Version nibble is at byte 7 high bits in .NET little-endian layout; instead check string positions:
        // GUID string "xxxxxxxx-xxxx-Mxxx-Nxxx-xxxxxxxxxxxx", N-format index 12 = version, 16 = variant.
        uuid[12].Should().Be('3');
        "89ab".Should().Contain(uuid[16].ToString());
        _ = bytes;
    }
}
