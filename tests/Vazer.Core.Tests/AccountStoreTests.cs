using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vazer.Core;
using Vazer.Core.Abstractions;
using Vazer.Core.Store;

namespace Vazer.Core.Tests;

/// <summary>Phase 1.6 TASK3: AccountStore round-trip tests. Temp dir, self-cleanup.</summary>
public sealed class AccountStoreTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly AccountStore _store;

    public AccountStoreTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "upco-mc-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        var options = Options.Create(new MinecraftOptions { RootPath = _tempRoot });
        _store = new AccountStore(NullLogger<AccountStore>.Instance, options);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, true); } catch { }
    }

    private static MinecraftAccount MakeAccount(string id, string name, AccountKind kind = AccountKind.Offline)
    {
        return new MinecraftAccount
        {
            Id = id,
            DisplayName = name,
            Kind = kind,
            Uuid = id,
            AccessToken = "token-" + id,
            RefreshToken = "refresh-" + id,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };
    }

    [Fact]
    public async Task Save_List_Get_RoundTrip()
    {
        var acc = MakeAccount("uuid-1", "Steve");
        await _store.SaveAsync(acc);

        var list = await _store.ListAsync();
        list.Should().HaveCount(1);
        list[0].Id.Should().Be("uuid-1");
        list[0].DisplayName.Should().Be("Steve");

        // List must NOT expose tokens
        list[0].AccessToken.Should().BeNull();
        list[0].RefreshToken.Should().BeNull();

        var got = await _store.GetAsync("uuid-1");
        got.Should().NotBeNull();
        got!.DisplayName.Should().Be("Steve");
        got.Uuid.Should().Be("uuid-1");
        got.Kind.Should().Be(AccountKind.Offline);
        // DPAPI round-trip restores tokens for runtime use
        got.AccessToken.Should().Be("token-uuid-1");
    }

    [Fact]
    public async Task Save_TwoAccounts_ListCount_IsTwo()
    {
        await _store.SaveAsync(MakeAccount("a", "Alice"));
        await _store.SaveAsync(MakeAccount("b", "Bob"));
        var list = await _store.ListAsync();
        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task SetActive_GetActive_ReturnsCorrect()
    {
        await _store.SaveAsync(MakeAccount("a", "Alice"));
        await _store.SaveAsync(MakeAccount("b", "Bob"));
        await _store.SetActiveAsync("b");

        var active = await _store.GetActiveAsync();
        active.Should().NotBeNull();
        active!.Id.Should().Be("b");
    }

    [Fact]
    public async Task Delete_Removes_Account_And_Clears_Active()
    {
        await _store.SaveAsync(MakeAccount("a", "Alice"));
        await _store.SetActiveAsync("a");
        await _store.DeleteAsync("a");

        (await _store.GetAsync("a")).Should().BeNull();
        (await _store.ListAsync()).Should().BeEmpty();
        (await _store.GetActiveAsync()).Should().BeNull();
    }

    [Fact]
    public async Task Save_IsAtomic_NoTmpLeftBehind()
    {
        await _store.SaveAsync(MakeAccount("a", "Alice"));
        var dir = Path.Combine(_tempRoot, "accounts");
        Directory.GetFiles(dir, "*.tmp").Should().BeEmpty();
        File.Exists(Path.Combine(dir, "accounts.json")).Should().BeTrue();
    }
}
