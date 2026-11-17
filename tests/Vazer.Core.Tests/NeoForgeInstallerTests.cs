using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vazer.Core;
using Vazer.Core.Loaders;
using Vazer.Core.Tests.TestHelpers;

namespace Vazer.Core.Tests;

/// <summary>
/// Phase 1.7 TASK1: NeoForge version resolution with mocked Maven metadata.
/// The live installer-JAR run needs Java + network and is covered manually
/// (see docs/minecraft-smoke-test.md).
/// </summary>
public sealed class NeoForgeInstallerTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly FakeHttpMessageHandler _http;

    private static readonly string MetadataXml = string.Join("\n",
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
        "<metadata>",
        "  <groupId>net.neoforged</groupId>",
        "  <artifactId>neoforge</artifactId>",
        "  <versioning>",
        "    <versions>",
        "      <version>20.4.237</version>",
        "      <version>21.0.167</version>",
        "      <version>21.1.72</version>",
        "      <version>21.1.169</version>",
        "      <version>21.4.4</version>",
        "    </versions>",
        "  </versioning>",
        "</metadata>");

    public NeoForgeInstallerTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "upco-mc-neoforge-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _http = new FakeHttpMessageHandler();
        _http.Map(HttpMethod.Get, "/releases/net/neoforged/neoforge/maven-metadata.xml", _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(MetadataXml, System.Text.Encoding.UTF8, "application/xml")
            });
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, true); } catch { }
    }

    private NeoForgeInstaller CreateService()
    {
        var options = Options.Create(new MinecraftOptions { RootPath = _tempRoot });
        return new NeoForgeInstaller(
            new FakeHttpClientFactory(_http),
            NullLogger<NeoForgeInstaller>.Instance,
            options);
    }

    [Theory]
    [InlineData("1.20.1", "20.1.")]
    [InlineData("1.20.4", "20.4.")]
    [InlineData("1.21", "21.0.")]
    [InlineData("1.21.1", "21.1.")]
    [InlineData("1.21.4", "21.4.")]
    public void McToNeoForgePrefix_Mapping(string mc, string expected)
    {
        NeoForgeInstaller.McToNeoForgePrefix(mc).Should().Be(expected);
    }

    [Theory]
    [InlineData("24w14a")]
    [InlineData("1.16.5")]
    [InlineData("1.19.4")]
    public void McToNeoForgePrefix_Unsupported_Throws(string mc)
    {
        Assert.Throws<NotSupportedException>(() => NeoForgeInstaller.McToNeoForgePrefix(mc));
    }

    [Fact]
    public async Task ResolveLatest_Picks_Max_Build_For_Prefix()
    {
        var svc = CreateService();
        (await svc.ResolveLatestVersionAsync("1.21.1")).Should().Be("21.1.169");
    }

    [Fact]
    public async Task ResolveLatest_Other_Prefixes()
    {
        var svc = CreateService();
        (await svc.ResolveLatestVersionAsync("1.20.4")).Should().Be("20.4.237");
        (await svc.ResolveLatestVersionAsync("1.21.4")).Should().Be("21.4.4");
    }

    [Fact]
    public async Task ResolveLatest_NoBuild_Throws_InvalidOperation()
    {
        var svc = CreateService();
        // 1.20.2 -> prefix 20.2. has no entry in the fixture.
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ResolveLatestVersionAsync("1.20.2"));
    }

    [Fact]
    public void Registry_Register_Resolve_RoundTrip()
    {
        var registry = new LoaderInstallerRegistry();
        var svc = CreateService();

        registry.TryGet<INeoForgeInstaller>("neoforge", out _).Should().BeFalse();
        registry.Register("neoforge", svc);
        registry.TryGet<INeoForgeInstaller>("neoforge", out var resolved).Should().BeTrue();
        resolved.Should().BeSameAs(svc);
        registry.TryGet<INeoForgeInstaller>("NEOFORGE", out _).Should().BeTrue();
    }

    // ---- Không có launcher_profiles.json → installer exit code 1 (bug thực tế) ----

    [Fact]
    public void EnsureLauncherProfiles_CreatesMinimalFile_WhenMissing()
    {
        var dir = Path.Combine(_tempRoot, "gamedir-test");

        var created = NeoForgeInstaller.EnsureLauncherProfiles(dir);

        created.Should().BeTrue();
        var json = File.ReadAllText(Path.Combine(dir, "launcher_profiles.json"));
        json.Should().Contain("\"profiles\"");
    }

    [Fact]
    public void EnsureLauncherProfiles_DoesNotOverwrite_ExistingFile()
    {
        var dir = Path.Combine(_tempRoot, "gamedir-test-2");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "launcher_profiles.json");
        File.WriteAllText(path, "{\"profiles\":{\"x\":{}},\"version\":3}");

        var created = NeoForgeInstaller.EnsureLauncherProfiles(dir);

        created.Should().BeFalse();
        File.ReadAllText(path).Should().Contain("\"x\"");
    }

    [Fact]
    public void ExtractInstallerError_PicksErrorLine_FromRealFailureOutput()
    {
        // Output thật từ NeoForge installer khi thiếu launcher_profiles.json
        var stdout = string.Join("\n",
            "Outputting log to file neoforge-21.0.167-installer.jar.log",
            "JVM info: Eclipse Adoptium - 21.0.12.1",
            "Current Time: 05/11/2026 12:31:52",
            "Found java version 21.0.12.1",
            "Target Directory: C:\\gamedir",
            "There is no minecraft launcher profile in \"C:\\gamedir\", you need to run the launcher first!",
            "There was an error during installation");

        var reason = NeoForgeInstaller.ExtractInstallerError(stdout, null);

        reason.Should().Contain("error");
    }

    [Fact]
    public void ExtractInstallerError_EmptyOutput_ReturnsFallback()
    {
        NeoForgeInstaller.ExtractInstallerError(string.Empty, string.Empty)
            .Should().Contain("không in ra");
    }
}
