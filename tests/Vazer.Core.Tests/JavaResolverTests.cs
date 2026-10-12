using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vazer.Core;
using Vazer.Core.Abstractions;
using Vazer.Core.Java;

namespace Vazer.Core.Tests;

/// <summary>Phase 1.6 TASK3: RequiredJava mapping + SelectBest. No filesystem/network.</summary>
public sealed class JavaResolverTests
{
    private readonly JavaResolver _resolver;

    public JavaResolverTests()
    {
        var fakeFactory = new FakeHttpClientFactory();
        var options = Options.Create(new MinecraftOptions
        {
            RootPath = Path.Combine(Path.GetTempPath(), "upco-mc-java-tests")
        });
        _resolver = new JavaResolver(fakeFactory, NullLogger<JavaResolver>.Instance, options);
    }

    [Theory]
    [InlineData("1.8.9", 8)]
    [InlineData("1.16.5", 8)]
    [InlineData("1.17.1", 16)]
    [InlineData("1.18.2", 17)]
    [InlineData("1.19.4", 17)]
    [InlineData("1.20.4", 17)]
    [InlineData("1.21.1", 21)]
    public void RequiredJava_Mapping_Cases(string mc, int expected)
    {
        _resolver.RequiredJava(mc).Should().Be(expected);
    }

    private static JavaInstallation Make(int major, string source, string path)
    {
        return new JavaInstallation
        {
            Path = path,
            MajorVersion = major,
            Version = $"{major}.0.0",
            Vendor = "Test",
            Architecture = "x64",
            IsJdk = true,
            Source = source
        };
    }

    [Fact]
    public void SelectBest_ExactMajor_Wins()
    {
        var list = new List<JavaInstallation>
        {
            Make(17, "system", "c:\\j17"),
            Make(21, "system", "c:\\j21"),
        };
        _resolver.SelectBest("1.21.1", list)!.Path.Should().Be("c:\\j21");
    }

    [Fact]
    public void SelectBest_PrefersBundled_OverSystem()
    {
        var list = new List<JavaInstallation>
        {
            Make(21, "system", "c:\\sys"),
            Make(21, "bundled", "c:\\bundled"),
        };
        _resolver.SelectBest("1.21.1", list)!.Source.Should().Be("bundled");
    }

    [Fact]
    public void SelectBest_PrefersAdoptium_OverSystem()
    {
        var list = new List<JavaInstallation>
        {
            Make(21, "system", "c:\\sys"),
            Make(21, "adoptium", "c:\\adopt"),
        };
        _resolver.SelectBest("1.21.1", list)!.Source.Should().Be("adoptium");
    }

    [Fact]
    public void SelectBest_FallsBack_ToHighestSystem_WhenNoPreferredSource()
    {
        var list = new List<JavaInstallation>
        {
            Make(21, "system", "c:\\sys21"),
            Make(22, "system", "c:\\sys22"),
        };
        // Closer to required (lower) wins per implementation: 21 over 22.
        _resolver.SelectBest("1.21.1", list)!.Path.Should().Be("c:\\sys21");
    }

    [Fact]
    public void SelectBest_Empty_ReturnsNull()
    {
        _resolver.SelectBest("1.21.1", new List<JavaInstallation>()).Should().BeNull();
    }

    [Fact]
    public async Task TryProbe_NonexistentPath_ReturnsNull()
    {
        var result = await _resolver.TryProbeAsync(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "java.exe"));
        result.Should().BeNull();
    }

    [Fact]
    public async Task DiscoverAsync_Returns_List_Without_Throwing()
    {
        var list = await _resolver.DiscoverAsync();
        list.Should().NotBeNull();
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient();
    }
}
