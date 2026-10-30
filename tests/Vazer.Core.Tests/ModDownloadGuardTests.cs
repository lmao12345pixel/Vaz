using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Vazer.Core;
using Vazer.Core.Abstractions;
using Vazer.Core.Modrinth;
using Vazer.Core.Mods;

namespace Vazer.Core.Tests;

public sealed class ModDownloadGuardTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _instanceDir;
    private readonly Mock<IModrinthClient> _modrinthMock;
    private readonly ModInstallService _service;

    public ModDownloadGuardTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "upco-guard-tests", Guid.NewGuid().ToString("N"));
        _instanceDir = Path.Combine(_tempRoot, "instances", "test-inst");
        Directory.CreateDirectory(Path.Combine(_instanceDir, "mods"));

        var instance = new MinecraftInstance
        {
            Id = "test-inst",
            Name = "Test Instance",
            MinecraftVersion = "1.21.1",
            LoaderType = "fabric",
            Path = _instanceDir,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var instanceManagerMock = new Mock<IInstanceManager>();
        instanceManagerMock.Setup(m => m.GetAsync("test-inst", It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        _modrinthMock = new Mock<IModrinthClient>();
        _service = new ModInstallService(
            instanceManagerMock.Object,
            _modrinthMock.Object,
            Options.Create(new MinecraftOptions { RootPath = _tempRoot }),
            NullLogger<ModInstallService>.Instance);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failure
        }
    }

    [Fact]
    public void IsAllowedDownloadUrl_CdnHttps_True()
    {
        ModInstallService.IsAllowedDownloadUrl("https://cdn.modrinth.com/data/AABBCC/versions/1.0/file.jar")
            .Should().BeTrue();
    }

    [Fact]
    public void IsAllowedDownloadUrl_HttpScheme_False()
    {
        ModInstallService.IsAllowedDownloadUrl("http://cdn.modrinth.com/file.jar")
            .Should().BeFalse();
    }

    [Fact]
    public void IsAllowedDownloadUrl_OtherHost_False()
    {
        ModInstallService.IsAllowedDownloadUrl("https://evil.example.com/file.jar")
            .Should().BeFalse();
    }

    [Fact]
    public void IsAllowedDownloadUrl_HostCaseInsensitive_True()
    {
        ModInstallService.IsAllowedDownloadUrl("https://CDN.MODRINTH.COM/file.jar")
            .Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("/relative/file.jar")]
    public void IsAllowedDownloadUrl_EmptyOrNonAbsolute_False(string url)
    {
        ModInstallService.IsAllowedDownloadUrl(url).Should().BeFalse();
    }

    [Fact]
    public void SanitizeModFileName_SafeName_Unchanged()
    {
        ModInstallService.SanitizeModFileName("sodium-fabric-0.8.13+mc1.21.1.jar")
            .Should().Be("sodium-fabric-0.8.13+mc1.21.1.jar");
    }

    [Fact]
    public void SanitizeModFileName_TraversalOrPathedName_Null()
    {
        ModInstallService.SanitizeModFileName("../evil.jar").Should().BeNull();
        ModInstallService.SanitizeModFileName("..\\evil.jar").Should().BeNull();
        ModInstallService.SanitizeModFileName("sub/evil.jar").Should().BeNull();
        ModInstallService.SanitizeModFileName("sub\\evil.jar").Should().BeNull();
        ModInstallService.SanitizeModFileName("..jar").Should().BeNull();
    }

    [Fact]
    public void SanitizeModFileName_ControlChars_Null()
    {
        ModInstallService.SanitizeModFileName("bad\u0000name.jar").Should().BeNull();
    }

    [Fact]
    public void SanitizeModFileName_Empty_Null()
    {
        ModInstallService.SanitizeModFileName("").Should().BeNull();
        ModInstallService.SanitizeModFileName("   ").Should().BeNull();
    }

    [Fact]
    public void IsSafeModFileName_Safe_TrueTraversal_False()
    {
        ModInstallService.IsSafeModFileName("iron-chests.jar").Should().BeTrue();
        ModInstallService.IsSafeModFileName("../evil.jar").Should().BeFalse();
        ModInstallService.IsSafeModFileName("a/b.jar").Should().BeFalse();
    }

    [Fact]
    public async Task InstallFromModrinthAsync_NonCdnUrl_ThrowsAndNeverDownloads()
    {
        var content = new byte[] { 1, 2, 3 };
        var ver = CreateVersion("v1", "mod-1", "1.0.0", "release", "m.jar", content);
        var evil = ver with { Files = new[] { ver.Files[0] with { Url = "https://evil.example.com/m.jar" } } };

        _modrinthMock.Setup(m => m.GetProjectAsync("test-mod", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProject("mod-1", "test-mod", "Test Mod"));
        _modrinthMock.Setup(m => m.GetVersionsAsync("test-mod", "fabric", "1.21.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { evil });

        var act = () => _service.InstallFromModrinthAsync("test-inst", "test-mod");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not allowed*");
        _modrinthMock.Verify(
            m => m.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InstallFromModrinthAsync_UnsafeApiFilename_ThrowsAndNeverDownloads()
    {
        var content = new byte[] { 1, 2, 3 };
        var ver = CreateVersion("v1", "mod-1", "1.0.0", "release", "evil.jar", content);
        var untrusted = ver with
        {
            Files = new[] { ver.Files[0] with
            {
                Url = "https://cdn.modrinth.com/evil.jar",
                Filename = "..\\evil.jar"
            } }
        };

        _modrinthMock.Setup(m => m.GetProjectAsync("test-mod", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProject("mod-1", "test-mod", "Test Mod"));
        _modrinthMock.Setup(m => m.GetVersionsAsync("test-mod", "fabric", "1.21.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { untrusted });

        var act = () => _service.InstallFromModrinthAsync("test-inst", "test-mod");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*unsafe filename*");
        _modrinthMock.Verify(
            m => m.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // Không được có file nào trong mods hoặc manifest dính vết
        Directory.EnumerateFiles(Path.Combine(_instanceDir, "mods")).Should().BeEmpty();
    }

    private static ModrinthProject CreateProject(string id, string slug, string title, string projectType = "mod")
    {
        return new ModrinthProject
        {
            Id = id,
            Slug = slug,
            Title = title,
            ProjectType = projectType
        };
    }

    private static ProjectVersion CreateVersion(
        string versionId,
        string projectId,
        string versionNumber,
        string versionType,
        string filename,
        byte[] content)
    {
        return new ProjectVersion
        {
            Id = versionId,
            ProjectId = projectId,
            VersionNumber = versionNumber,
            VersionType = versionType,
            DatePublished = DateTimeOffset.UtcNow,
            Dependencies = Array.Empty<VersionDependency>(),
            Files = new[]
            {
                new VersionFile
                {
                    Filename = filename,
                    Primary = true,
                    Size = content.Length,
                    Url = $"https://cdn.modrinth.com/{filename}",
                    Hashes = new FileHashes
                    {
                        Sha512 = null
                    }
                }
            }
        };
    }
}
