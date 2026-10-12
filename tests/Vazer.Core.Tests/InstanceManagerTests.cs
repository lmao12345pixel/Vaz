using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vazer.Core;
using Vazer.Core.Abstractions;
using Vazer.Core.Instances;
using Vazer.Core.Loaders;

namespace Vazer.Core.Tests;

/// <summary>
/// Phase 1.6 TASK3 + 1.7 TASK1: InstanceManager metadata round-trip without network.
/// CreateFabric/CreateVanilla require network (Mojang/Fabric APIs) and are NOT
/// covered here. NeoForge goes through injectable <see cref="INeoForgeInstallerService"/>.
/// </summary>
public sealed class InstanceManagerTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly InstanceManager _manager;

    public InstanceManagerTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "upco-mc-inst-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        var options = Options.Create(new MinecraftOptions { RootPath = _tempRoot });
        _manager = new InstanceManager(
            NullLogger<InstanceManager>.Instance,
            options,
            new FakeHttpClientFactory());
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, true); } catch { }
    }

    private async Task SeedInstanceAsync(string id, string name = "Test", string mc = "1.21.1")
    {
        var dir = Path.Combine(_tempRoot, "instances", id);
        Directory.CreateDirectory(dir);
        var inst = new MinecraftInstance
        {
            Id = id,
            Name = name,
            MinecraftVersion = mc,
            LoaderType = "fabric",
            LoaderVersion = "0.15.11",
            Path = dir,
            IconPath = null,
            CreatedAt = DateTimeOffset.UtcNow,
            LastPlayedAt = null
        };
        var json = JsonSerializer.Serialize(inst, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await File.WriteAllTextAsync(Path.Combine(dir, "instance.json"), json);
    }

    [Fact]
    public async Task List_EmptyDir_ReturnsEmpty()
    {
        (await _manager.ListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Get_List_RoundTrip()
    {
        await SeedInstanceAsync("my-fabric-1211", "My Fabric", "1.21.1");
        var got = await _manager.GetAsync("my-fabric-1211");
        got.Should().NotBeNull();
        got!.Name.Should().Be("My Fabric");
        got.MinecraftVersion.Should().Be("1.21.1");

        var list = await _manager.ListAsync();
        list.Should().HaveCount(1);
    }

    [Fact]
    public async Task Delete_Removes_Metadata_And_Folder()
    {
        await SeedInstanceAsync("todelete");
        var dir = Path.Combine(_tempRoot, "instances", "todelete");
        Directory.Exists(dir).Should().BeTrue();

        await _manager.DeleteAsync("todelete", deleteFiles: true);

        Directory.Exists(dir).Should().BeFalse();
        (await _manager.GetAsync("todelete")).Should().BeNull();
    }

    [Fact]
    public async Task UpdateLastPlayed_Sets_Timestamp()
    {
        await SeedInstanceAsync("played");
        await _manager.UpdateLastPlayedAsync("played");
        var got = await _manager.GetAsync("played");
        got!.LastPlayedAt.Should().NotBeNull();
        got.LastPlayedAt!.Value.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CreateNeoForgeAsync_Uses_Service_And_Writes_Metadata()
    {
        var fake = new FakeNeoForgeService { Version = "21.1.169" };
        var options = Microsoft.Extensions.Options.Options.Create(
            new MinecraftOptions { RootPath = _tempRoot });
        var manager = new InstanceManager(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<InstanceManager>.Instance,
            options,
            new FakeHttpClientFactory(),
            fake);

        var inst = await manager.CreateNeoForgeAsync("My NeoForge", "1.21.1");

        inst.LoaderType.Should().Be("neoforge");
        inst.LoaderVersion.Should().Be("21.1.169");
        fake.Installed.Should().ContainSingle().Which.Should().BeSameAs(inst);

        // Metadata round-trips through the real store path.
        var reloaded = await manager.GetAsync(inst.Id);
        reloaded.Should().NotBeNull();
        reloaded!.LoaderVersion.Should().Be("21.1.169");
    }

    [Fact]
    public async Task CreateNeoForgeAsync_ExplicitVersion_Skips_Resolve()
    {
        var fake = new FakeNeoForgeService { Version = "SHOULD-NOT-BE-USED" };
        var options = Microsoft.Extensions.Options.Options.Create(
            new MinecraftOptions { RootPath = _tempRoot });
        var manager = new InstanceManager(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<InstanceManager>.Instance,
            options,
            new FakeHttpClientFactory(),
            fake);

        var inst = await manager.CreateNeoForgeAsync("Pinned", "1.21.1", "21.1.72");

        inst.LoaderVersion.Should().Be("21.1.72");
        fake.ResolveCalls.Should().Be(0);
        fake.Installed.Should().HaveCount(1);
    }

    private sealed class FakeNeoForgeService : INeoForgeInstallerService
    {
        public string Version { get; set; } = "21.1.169";
        public int ResolveCalls { get; private set; }
        public List<MinecraftInstance> Installed { get; } = new();

        public Task<string> ResolveLatestVersionAsync(string minecraftVersion, CancellationToken ct = default)
        {
            ResolveCalls++;
            return Task.FromResult(Version);
        }

        public Task InstallAsync(MinecraftInstance instance, IProgress<InstallProgress>? progress = null, CancellationToken ct = default)
        {
            Installed.Add(instance);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient();
    }
}
