using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vazer.Core;
using Vazer.Core.Loaders;

namespace Vazer.Core.Tests;

/// <summary>
/// Unit tests cho loader installers (ILoaderInstaller). CmlLib được dùng thật
/// nhưng không gọi network trong các test này — chỉ kiểm tra metadata/validation/selection logic.
/// </summary>
public sealed class LoaderInstallersTests
{
    [Fact]
    public void VanillaInstaller_LoaderType_And_SupportsVersion()
    {
        var options = Options.Create(new MinecraftOptions { RootPath = Path.Combine(Path.GetTempPath(), "vaz-test-vanilla") });
        var svc = new VanillaInstaller(options, NullLogger<VanillaInstaller>.Instance);

        svc.LoaderType.Should().Be("vanilla");
        svc.SupportsVersion("1.21.1").Should().BeTrue();
        svc.SupportsVersion("").Should().BeFalse();
    }

    [Fact]
    public void FabricInstaller_LoaderType_And_SupportsVersion()
    {
        var svc = new FabricInstaller(new TestHttpClientFactory(), NullLogger<FabricInstaller>.Instance);

        svc.LoaderType.Should().Be("fabric");
        svc.SupportsVersion("1.21.1").Should().BeTrue();
    }

    [Fact]
    public void QuiltInstaller_LoaderType_And_SupportsVersion()
    {
        var svc = new QuiltInstaller(new TestHttpClientFactory(), NullLogger<QuiltInstaller>.Instance);

        svc.LoaderType.Should().Be("quilt");
        svc.SupportsVersion("1.21.1").Should().BeTrue();
    }

    [Fact]
    public async Task ForgeInstaller_ThrowsNotSupported_OnInstall_WhenLegacyMc()
    {
        var options = Options.Create(new MinecraftOptions { RootPath = Path.Combine(Path.GetTempPath(), "vaz-test-forge") });
        var svc = new ForgeInstaller(new TestHttpClientFactory(), NullLogger<ForgeInstaller>.Instance, options);

        svc.LoaderType.Should().Be("forge");
        svc.SupportsVersion("1.20.1").Should().BeTrue();
        svc.SupportsVersion("1.8.9").Should().BeFalse();

        var act = async () => await svc.InstallAsync(Path.GetTempPath(), "1.8.9", null, null, CancellationToken.None);
        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Theory]
    [InlineData("1.21.1", true)]
    [InlineData("1.21", true)]
    [InlineData("1.20.4", true)]
    [InlineData("1.19.4", false)]
    [InlineData("24w14a", false)]
    public void NeoForgeInstaller_SupportsVersion(string mc, bool expected)
    {
        var svc = new NeoForgeInstaller(new TestHttpClientFactory(), NullLogger<NeoForgeInstaller>.Instance,
            Options.Create(new MinecraftOptions { RootPath = Path.GetTempPath() }));

        svc.SupportsVersion(mc).Should().Be(expected);
    }

    [Fact]
    public void SelectLatestStable_PicksStableNewest()
    {
        var loaders = new[]
        {
            ("0.16.0", true),
            ("0.17.0-beta.1", false),
            ("0.18.0", true),
            ("0.18.0-beta.2", false),
        };

        LoaderHelpers.SelectLatestStable(loaders, requested: null).Should().Be("0.18.0");
    }

    [Fact]
    public void SelectLatestStable_FallsBackToNewest_WhenNoStable()
    {
        var loaders = new[]
        {
            ("0.17.0-beta.1", false),
            ("0.18.0-beta.2", false),
        };

        LoaderHelpers.SelectLatestStable(loaders, requested: null).Should().Be("0.18.0-beta.2");
    }

    [Fact]
    public void SelectLatestStable_ReturnsRequested_WhenPresent()
    {
        var loaders = new[] { ("0.16.0", true), ("0.18.0", true) };

        LoaderHelpers.SelectLatestStable(loaders, "0.16.0").Should().Be("0.16.0");
    }

    [Fact]
    public void SelectLatestStable_ReturnsNull_WhenRequestedMissing()
    {
        var loaders = new[] { ("0.16.0", true), ("0.18.0", true) };

        LoaderHelpers.SelectLatestStable(loaders, "9.9.9").Should().BeNull();
    }

    [Fact]
    public void SelectLatestStable_OrdersByLoaderVersionComparer_NotOrdinal()
    {
        // Ordinal string compare sẽ xếp "21.1.206" < "21.1.99" — sai, phải dùng LoaderVersionComparer.
        var loaders = new[] { ("21.1.99", true), ("21.1.206", true), ("21.1.10", true) };

        LoaderHelpers.SelectLatestStable(loaders, requested: null).Should().Be("21.1.206");
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
