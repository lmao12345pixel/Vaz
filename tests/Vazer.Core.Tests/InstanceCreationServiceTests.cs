using System.IO;
using System.Text.Json;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vazer.Core;
using Vazer.Core.Abstractions;
using Vazer.Core.Instances;
using Vazer.Core.Json;
using Vazer.Core.Loaders;
using Vazer.Core.Versions;

namespace Vazer.Core.Tests;

public sealed class InstanceCreationServiceTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "vaz-creation-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, true); } catch { }
    }

    [Fact]
    public async Task CreateAsync_PicksCorrectInstaller_Fabric()
    {
        var (svc, installers, _) = CreateService();
        await svc.CreateAsync("My Pack", "1.21.1", "fabric", "0.16.9", null, CancellationToken.None);

        installers["fabric"].Called.Should().BeTrue();
        installers["vanilla"].Called.Should().BeFalse();
        installers["quilt"].Called.Should().BeFalse();
        installers["neoforge"].Called.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_UnknownLoader_Throws()
    {
        var (svc, _, _) = CreateService();
        var act = async () => await svc.CreateAsync("X", "1.21.1", "forge-custom", null, null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*không được hỗ trợ*");
    }

    [Fact]
    public async Task CreateAsync_UnsupportedMcVersion_Throws()
    {
        var (svc, installers, _) = CreateService();
        installers["neoforge"].Supports = false;

        var act = async () => await svc.CreateAsync("X", "1.19.4", "neoforge", "20.1.1", null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*không hỗ trợ Minecraft*");
        installers["neoforge"].Called.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_ResolvesLoaderVersion_WhenNotProvided()
    {
        var (svc, installers, versions) = CreateService();
        versions.LoaderVersions = new[]
        {
            new LoaderVersionInfo { Version = "0.16.9" },
            new LoaderVersionInfo { Version = "0.17.0" },
            new LoaderVersionInfo { Version = "0.18.0-beta.1", Release = "beta" },
        };

        var info = await svc.CreateAsync("Resolved", "1.21.1", "fabric", null, null, CancellationToken.None);

        installers["fabric"].ReceivedLoaderVersion.Should().Be("0.17.0", "stable mới nhất (LoaderVersionComparer)");
        info.LoaderVersion.Should().Be("0.17.0");
    }

    [Fact]
    public async Task CreateAsync_NoCompatibleLoaderVersion_Throws()
    {
        var (svc, installers, versions) = CreateService();
        versions.LoaderVersions = Array.Empty<LoaderVersionInfo>();

        var act = async () => await svc.CreateAsync("X", "1.21.1", "fabric", null, null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Không tìm thấy loader version*");
        installers["fabric"].Called.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_WritesInstanceJson_And_ComputesFabricLaunchVersion()
    {
        var (svc, installers, _) = CreateService();
        installers["fabric"].Supports = true;

        var info = await svc.CreateAsync("My Pack", "1.21.1", "fabric", "0.16.9", null, CancellationToken.None);

        var json = await File.ReadAllTextAsync(Path.Combine(info.Path, "instance.json"));
        var instance = JsonSerializer.Deserialize(json, VazJsonContext.CamelCase.MinecraftInstance)!;

        instance.Id.Should().Be("my pack");
        instance.Name.Should().Be("My Pack");
        instance.MinecraftVersion.Should().Be("1.21.1");
        instance.LoaderType.Should().Be("fabric");
        instance.LoaderVersion.Should().Be("0.16.9");
        instance.LaunchVersionName.Should().Be("fabric-loader-0.16.9-1.21.1");
        info.LaunchVersionName.Should().Be("fabric-loader-0.16.9-1.21.1");
    }

    [Fact]
    public async Task CreateAsync_Vanilla_HasNullLoader_AndScantLaunchVersion()
    {
        var (svc, _, _) = CreateService();

        var info = await svc.CreateAsync("Plain", "1.21.1", "vanilla", null, null, CancellationToken.None);

        info.LoaderType.Should().Be("vanilla");
        info.LoaderVersion.Should().BeNull();
        info.LaunchVersionName.Should().Be("1.21.1");
    }

    [Fact]
    public async Task CreateAsync_ForwardsProgress_PreparingAndDone_Last()
    {
        var (svc, installers, _) = CreateService();
        var events = new List<InstallerProgress>();
        var progress = new Progress<InstallerProgress>(p => events.Add(p));

        await svc.CreateAsync("Prog", "1.21.1", "fabric", "0.16.9", progress, CancellationToken.None);
        await Task.Delay(150); // Progress<T> post vào sync context

        events.Count.Should().BeGreaterThanOrEqualTo(2);
        events.First().Stage.Should().Be(InstallerStage.Preparing);
        events.Last().Stage.Should().Be(InstallerStage.Done);
        installers["fabric"].ProgressEvents.Should().NotBeEmpty("facade phải forward progress xuống installer");
    }

    private (InstanceCreationService Service, Dictionary<string, FakeInstaller> Installers, FakeVersionService Versions) CreateService()
    {
        Directory.CreateDirectory(_tempRoot);
        var installers = new Dictionary<string, FakeInstaller>
        {
            ["vanilla"] = new FakeInstaller("vanilla"),
            ["fabric"] = new FakeInstaller("fabric"),
            ["quilt"] = new FakeInstaller("quilt"),
            ["neoforge"] = new FakeInstaller("neoforge"),
            ["forge"] = new FakeInstaller("forge"),
        };
        var versions = new FakeVersionService();
        var options = Options.Create(new MinecraftOptions { RootPath = _tempRoot });
        var svc = new InstanceCreationService(
            installers.Values.Cast<ILoaderInstaller>(),
            versions,
            options,
            NullLogger<InstanceCreationService>.Instance);
        return (svc, installers, versions);
    }

    private sealed class FakeInstaller : ILoaderInstaller
    {
        public FakeInstaller(string loaderType) { LoaderType = loaderType; }

        public string LoaderType { get; }
        public bool Supports { get; set; } = true;
        public bool Called { get; private set; }
        public string? ReceivedLoaderVersion { get; private set; }
        public List<InstallerProgress> ProgressEvents { get; } = new();

        public bool SupportsVersion(string mcVersion) => Supports;

        public Task InstallAsync(string instancePath, string mcVersion, string? loaderVersion, IProgress<InstallerProgress>? progress, CancellationToken ct)
        {
            Called = true;
            ReceivedLoaderVersion = loaderVersion;
            Directory.CreateDirectory(instancePath);
            progress?.Report(new InstallerProgress { Stage = InstallerStage.Installing, Percent = -1, Message = "fake installing" });
            ProgressEvents.Add(new InstallerProgress { Stage = InstallerStage.Installing, Percent = -1, Message = "fake installing" });
            progress?.Report(new InstallerProgress { Stage = InstallerStage.Done, Percent = 100, Message = "fake done" });
            return Task.CompletedTask;
        }
    }

    private sealed class FakeVersionService : IMinecraftVersionService
    {
        public IReadOnlyList<LoaderVersionInfo> LoaderVersions { get; set; } = Array.Empty<LoaderVersionInfo>();

        public Task<IReadOnlyList<MinecraftVersionInfo>> GetVanillaVersionsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MinecraftVersionInfo>>(Array.Empty<MinecraftVersionInfo>());

        public Task<IReadOnlyList<LoaderVersionInfo>> GetLoaderVersionsAsync(string loader, string mcVersion, CancellationToken ct = default)
            => Task.FromResult(LoaderVersions);

        public Task<IReadOnlyList<LoaderVersionInfo>> GetForgeVersionsAsync(string mcVersion, CancellationToken ct = default)
            => GetLoaderVersionsAsync("forge", mcVersion, ct);

        public Task<bool> ClearCacheAsync(CancellationToken ct = default) => Task.FromResult(true);

        public Task<DateTimeOffset?> GetCacheTimestampAsync(string cacheFile, CancellationToken ct = default)
            => Task.FromResult<DateTimeOffset?>(null);
    }
}
