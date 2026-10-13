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

public sealed class ModInstallServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _instanceDir;
    private readonly MinecraftInstance _instance;
    private readonly Mock<IInstanceManager> _instanceManagerMock;
    private readonly Mock<IModrinthClient> _modrinthMock;
    private readonly IOptions<MinecraftOptions> _options;
    private readonly ModInstallService _service;

    public ModInstallServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "upco-mod-tests", Guid.NewGuid().ToString("N"));
        _instanceDir = Path.Combine(_tempRoot, "instances", "test-inst");
        Directory.CreateDirectory(_instanceDir);

        _instance = new MinecraftInstance
        {
            Id = "test-inst",
            Name = "Test Instance",
            MinecraftVersion = "1.21.1",
            LoaderType = "fabric",
            Path = _instanceDir,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _instanceManagerMock = new Mock<IInstanceManager>();
        _instanceManagerMock.Setup(m => m.GetAsync("test-inst", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_instance);

        _modrinthMock = new Mock<IModrinthClient>();
        _options = Options.Create(new MinecraftOptions { RootPath = _tempRoot });

        _service = new ModInstallService(
            _instanceManagerMock.Object,
            _modrinthMock.Object,
            _options,
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
    public async Task InstallFromModrinthAsync_HappyPath_VersionIdNull_LatestReleaseInstalled()
    {
        // Arrange
        var content = new byte[] { 1, 2, 3, 4 };
        var sha512 = ComputeSha512Hex(content);

        var project = CreateProject("mod-1", "test-mod", "Test Mod");
        var vOld = CreateVersion("v1", "mod-1", "1.0.0", "release", "test-mod-1.0.0.jar", content, sha512, datePublished: DateTimeOffset.UtcNow.AddDays(-2));
        var vLatest = CreateVersion("v2", "mod-1", "2.0.0", "release", "test-mod-2.0.0.jar", content, sha512, datePublished: DateTimeOffset.UtcNow.AddDays(-1));
        var vBeta = CreateVersion("v3", "mod-1", "3.0.0-beta", "beta", "test-mod-3.0.0-beta.jar", content, sha512, datePublished: DateTimeOffset.UtcNow);

        _modrinthMock.Setup(m => m.GetProjectAsync("test-mod", It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _modrinthMock.Setup(m => m.GetVersionsAsync("test-mod", "fabric", "1.21.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { vOld, vLatest, vBeta });
        _modrinthMock.Setup(m => m.DownloadAsync(vLatest.Files[0].Url, It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream(content)));

        // Act
        var result = await _service.InstallFromModrinthAsync("test-inst", "test-mod");

        // Assert
        result.Success.Should().BeTrue();
        result.VersionId.Should().Be("v2");
        result.FileName.Should().Be("test-mod-2.0.0.jar");
        result.Warning.Should().BeNull();

        var jarPath = Path.Combine(_instanceDir, "mods", "test-mod-2.0.0.jar");
        File.Exists(jarPath).Should().BeTrue();

        var manifest = await ReadManifestAsync();
        manifest.Mods.Should().HaveCount(1);
        manifest.Mods[0].ProjectId.Should().Be("mod-1");
        manifest.Mods[0].VersionId.Should().Be("v2");
        manifest.Mods[0].Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task InstallFromModrinthAsync_DependencyRequired_InstallsBoth_CycleTerminates()
    {
        // Arrange: A -> B -> A
        var contentA = new byte[] { 10, 20 };
        var contentB = new byte[] { 30, 40 };
        var shaA = ComputeSha512Hex(contentA);
        var shaB = ComputeSha512Hex(contentB);

        var projA = CreateProject("modA", "modA", "Mod A");
        var projB = CreateProject("modB", "modB", "Mod B");

        var depToB = new VersionDependency { ProjectId = "modB", DependencyType = "required" };
        var depToA = new VersionDependency { ProjectId = "modA", DependencyType = "required" };

        var verA = CreateVersion("vA", "modA", "1.0", "release", "modA.jar", contentA, shaA, dependencies: new[] { depToB });
        var verB = CreateVersion("vB", "modB", "1.0", "release", "modB.jar", contentB, shaB, dependencies: new[] { depToA });

        _modrinthMock.Setup(m => m.GetProjectAsync("modA", It.IsAny<CancellationToken>())).ReturnsAsync(projA);
        _modrinthMock.Setup(m => m.GetProjectAsync("modB", It.IsAny<CancellationToken>())).ReturnsAsync(projB);

        _modrinthMock.Setup(m => m.GetVersionsAsync("modA", "fabric", "1.21.1", It.IsAny<CancellationToken>())).ReturnsAsync(new[] { verA });
        _modrinthMock.Setup(m => m.GetVersionsAsync("modB", "fabric", "1.21.1", It.IsAny<CancellationToken>())).ReturnsAsync(new[] { verB });

        _modrinthMock.Setup(m => m.DownloadAsync(verA.Files[0].Url, It.IsAny<CancellationToken>())).Returns(() => Task.FromResult<Stream>(new MemoryStream(contentA)));
        _modrinthMock.Setup(m => m.DownloadAsync(verB.Files[0].Url, It.IsAny<CancellationToken>())).Returns(() => Task.FromResult<Stream>(new MemoryStream(contentB)));

        // Act
        var result = await _service.InstallFromModrinthAsync("test-inst", "modA");

        // Assert
        result.Success.Should().BeTrue();
        result.InstalledDependencies.Should().Contain("modB");

        var manifest = await ReadManifestAsync();
        manifest.Mods.Should().HaveCount(2);
        manifest.Mods.Should().Contain(m => m.ProjectId == "modA");
        manifest.Mods.Should().Contain(m => m.ProjectId == "modB");

        File.Exists(Path.Combine(_instanceDir, "mods", "modA.jar")).Should().BeTrue();
        File.Exists(Path.Combine(_instanceDir, "mods", "modB.jar")).Should().BeTrue();
    }

    [Fact]
    public async Task InstallFromModrinthAsync_IncompatibleDependency_ThrowsWithConflictMessage()
    {
        // Arrange: ModB is already installed. ModA declares ModB as incompatible.
        var initialManifest = new ModsManifest
        {
            Mods = new List<InstalledMod>
            {
                new InstalledMod
                {
                    FileName = "modB.jar",
                    ProjectId = "modB",
                    VersionId = "vB",
                    Source = "modrinth",
                    Enabled = true,
                    InstalledAt = DateTimeOffset.UtcNow
                }
            }
        };
        await WriteManifestAsync(initialManifest);

        var projA = CreateProject("modA", "modA", "Mod Alpha");
        var depIncompatible = new VersionDependency { ProjectId = "modB", DependencyType = "incompatible" };
        var verA = CreateVersion("vA", "modA", "1.0", "release", "modA.jar", new byte[] { 1 }, dependencies: new[] { depIncompatible });

        _modrinthMock.Setup(m => m.GetProjectAsync("modA", It.IsAny<CancellationToken>())).ReturnsAsync(projA);
        _modrinthMock.Setup(m => m.GetVersionsAsync("modA", "fabric", "1.21.1", It.IsAny<CancellationToken>())).ReturnsAsync(new[] { verA });

        // Act
        var act = () => _service.InstallFromModrinthAsync("test-inst", "modA");

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("Incompatible mod conflict between Mod Alpha and modB");

        var manifest = await ReadManifestAsync();
        manifest.Mods.Should().HaveCount(1);
        manifest.Mods[0].ProjectId.Should().Be("modB");
        File.Exists(Path.Combine(_instanceDir, "mods", "modA.jar")).Should().BeFalse();
    }

    [Fact]
    public async Task InstallFromModrinthAsync_SameProjectIdDifferentVersion_ReplacesOldJar_PreservesEnabledState()
    {
        // Arrange: Old version is disabled
        var oldContent = new byte[] { 1 };
        var oldFile = Path.Combine(_instanceDir, "mods", "modA-1.0.jar.disabled");
        Directory.CreateDirectory(Path.Combine(_instanceDir, "mods"));
        await File.WriteAllBytesAsync(oldFile, oldContent);

        var initialManifest = new ModsManifest
        {
            Mods = new List<InstalledMod>
            {
                new InstalledMod
                {
                    FileName = "modA-1.0.jar.disabled",
                    ProjectId = "modA",
                    VersionId = "v1",
                    Source = "modrinth",
                    Enabled = false,
                    InstalledAt = DateTimeOffset.UtcNow.AddDays(-1)
                }
            }
        };
        await WriteManifestAsync(initialManifest);

        var newContent = new byte[] { 2, 2 };
        var newSha = ComputeSha512Hex(newContent);
        var projA = CreateProject("modA", "modA", "Mod A");
        var verA2 = CreateVersion("v2", "modA", "2.0", "release", "modA-2.0.jar", newContent, newSha);

        _modrinthMock.Setup(m => m.GetProjectAsync("modA", It.IsAny<CancellationToken>())).ReturnsAsync(projA);
        _modrinthMock.Setup(m => m.GetVersionAsync("v2", It.IsAny<CancellationToken>())).ReturnsAsync(verA2);
        _modrinthMock.Setup(m => m.DownloadAsync(verA2.Files[0].Url, It.IsAny<CancellationToken>())).Returns(() => Task.FromResult<Stream>(new MemoryStream(newContent)));

        // Act
        var result = await _service.InstallFromModrinthAsync("test-inst", "modA", versionId: "v2");

        // Assert
        result.Success.Should().BeTrue();
        result.FileName.Should().Be("modA-2.0.jar.disabled");

        File.Exists(oldFile).Should().BeFalse();
        File.Exists(Path.Combine(_instanceDir, "mods", "modA-2.0.jar.disabled")).Should().BeTrue();

        var manifest = await ReadManifestAsync();
        manifest.Mods.Should().HaveCount(1);
        manifest.Mods[0].VersionId.Should().Be("v2");
        manifest.Mods[0].FileName.Should().Be("modA-2.0.jar.disabled");
        manifest.Mods[0].Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task InstallFromModrinthAsync_ChecksumMismatch_Throws_PartialDeleted_ModsJsonUnchanged()
    {
        // Arrange
        var content = new byte[] { 1, 2, 3 };
        var wrongSha = "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";

        var proj = CreateProject("modA", "modA", "Mod A");
        var ver = CreateVersion("v1", "modA", "1.0", "release", "modA.jar", content, wrongSha);

        _modrinthMock.Setup(m => m.GetProjectAsync("modA", It.IsAny<CancellationToken>())).ReturnsAsync(proj);
        _modrinthMock.Setup(m => m.GetVersionsAsync("modA", "fabric", "1.21.1", It.IsAny<CancellationToken>())).ReturnsAsync(new[] { ver });
        _modrinthMock.Setup(m => m.DownloadAsync(ver.Files[0].Url, It.IsAny<CancellationToken>())).Returns(() => Task.FromResult<Stream>(new MemoryStream(content)));

        // Act
        var act = () => _service.InstallFromModrinthAsync("test-inst", "modA");

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("SHA512 mismatch for modA.jar");

        File.Exists(Path.Combine(_instanceDir, "mods", "modA.jar.partial")).Should().BeFalse();
        File.Exists(Path.Combine(_instanceDir, "mods", "modA.jar")).Should().BeFalse();

        var manifest = await ReadManifestAsync();
        manifest.Mods.Should().BeEmpty();
    }

    [Fact]
    public async Task SetEnabledAsync_RenamesFile_AndUpdatesEnabledInModsJson()
    {
        // Arrange
        var content = new byte[] { 1 };
        var modsDir = Path.Combine(_instanceDir, "mods");
        Directory.CreateDirectory(modsDir);
        var jarFile = Path.Combine(modsDir, "modA.jar");
        await File.WriteAllBytesAsync(jarFile, content);

        var manifest = new ModsManifest
        {
            Mods = new List<InstalledMod>
            {
                new InstalledMod
                {
                    FileName = "modA.jar",
                    ProjectId = "modA",
                    VersionId = "v1",
                    Source = "modrinth",
                    Enabled = true,
                    InstalledAt = DateTimeOffset.UtcNow
                }
            }
        };
        await WriteManifestAsync(manifest);

        // Act 1: Disable
        await _service.SetEnabledAsync("test-inst", "modA.jar", false);

        // Assert 1
        File.Exists(jarFile).Should().BeFalse();
        File.Exists(Path.Combine(modsDir, "modA.jar.disabled")).Should().BeTrue();
        var disabledManifest = await ReadManifestAsync();
        disabledManifest.Mods[0].Enabled.Should().BeFalse();
        disabledManifest.Mods[0].FileName.Should().Be("modA.jar.disabled");

        // Act 2: Re-enable
        await _service.SetEnabledAsync("test-inst", "modA.jar.disabled", true);

        // Assert 2
        File.Exists(Path.Combine(modsDir, "modA.jar.disabled")).Should().BeFalse();
        File.Exists(jarFile).Should().BeTrue();
        var enabledManifest = await ReadManifestAsync();
        enabledManifest.Mods[0].Enabled.Should().BeTrue();
        enabledManifest.Mods[0].FileName.Should().Be("modA.jar");
    }

    [Fact]
    public async Task CheckUpdatesAsync_WithNewVersionMock_MarksHasUpdateTrue()
    {
        // Arrange
        var initialManifest = new ModsManifest
        {
            Mods = new List<InstalledMod>
            {
                new InstalledMod
                {
                    FileName = "modA.jar",
                    ProjectId = "modA",
                    VersionId = "v1",
                    Source = "modrinth",
                    Enabled = true,
                    InstalledAt = DateTimeOffset.UtcNow
                }
            }
        };
        await WriteManifestAsync(initialManifest);

        var ver1 = CreateVersion("v1", "modA", "1.0", "release", "modA.jar", new byte[] { 1 }, datePublished: DateTimeOffset.UtcNow.AddDays(-2));
        var ver2 = CreateVersion("v2", "modA", "2.0", "release", "modA.jar", new byte[] { 2 }, datePublished: DateTimeOffset.UtcNow.AddDays(-1));

        _modrinthMock.Setup(m => m.GetVersionsAsync("modA", "fabric", "1.21.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ver1, ver2 });

        // Act
        var result = await _service.CheckUpdatesAsync("test-inst");

        // Assert
        result.Updates.Should().HaveCount(1);
        result.Updates[0].HasUpdate.Should().BeTrue();
        result.Updates[0].LatestVersionId.Should().Be("v2");
        result.Updates[0].LatestVersionNumber.Should().Be("2.0");
    }

    [Fact]
    public async Task PerformancePack_Fabric_Happy_SodiumAndIrisInModsJson()
    {
        // Arrange
        SetupSodiumAndIrisMocks("fabric");

        // Act
        var result = await _service.InstallPerformancePackAsync("test-inst");

        // Assert
        result.Success.Should().BeTrue();
        result.Warning.Should().BeNull();

        var manifest = await ReadManifestAsync();
        manifest.Mods.Should().HaveCount(2);
        manifest.Mods.Should().Contain(m => m.ProjectId == "sodium");
        manifest.Mods.Should().Contain(m => m.ProjectId == "iris");
    }

    [Fact]
    public async Task PerformancePack_NeoForge_Happy_Works()
    {
        // Arrange
        var neoInstance = _instance with { LoaderType = "neoforge" };
        _instanceManagerMock.Setup(m => m.GetAsync("test-inst", It.IsAny<CancellationToken>()))
            .ReturnsAsync(neoInstance);

        SetupSodiumAndIrisMocks("neoforge");

        // Act
        var result = await _service.InstallPerformancePackAsync("test-inst");

        // Assert
        result.Success.Should().BeTrue();
        result.Warning.Should().BeNull();

        var manifest = await ReadManifestAsync();
        manifest.Mods.Should().HaveCount(2);
        manifest.Mods.Should().Contain(m => m.ProjectId == "sodium");
        manifest.Mods.Should().Contain(m => m.ProjectId == "iris");
    }

    [Fact]
    public async Task PerformancePack_Vanilla_ThrowsNotSupportedException()
    {
        // Arrange
        var vanillaInstance = _instance with { LoaderType = "vanilla" };
        _instanceManagerMock.Setup(m => m.GetAsync("test-inst", It.IsAny<CancellationToken>()))
            .ReturnsAsync(vanillaInstance);

        // Act
        var act = () => _service.InstallPerformancePackAsync("test-inst");

        // Assert
        var ex = await act.Should().ThrowAsync<NotSupportedException>();
        ex.WithMessage("Performance pack requires Fabric or NeoForge.");
    }

    [Fact]
    public async Task PerformancePack_SodiumOk_IrisFail_WarningNotNull_SodiumRemainsInstalled()
    {
        // Arrange
        SetupSodiumMock("fabric");
        _modrinthMock.Setup(m => m.GetProjectAsync("iris", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Iris network timeout"));

        // Act
        var result = await _service.InstallPerformancePackAsync("test-inst");

        // Assert
        result.Success.Should().BeTrue();
        result.Warning.Should().NotBeNull();
        result.Warning.Should().Contain("Iris installation failed").And.Contain("Sodium was installed successfully");

        var manifest = await ReadManifestAsync();
        manifest.Mods.Should().HaveCount(1);
        manifest.Mods[0].ProjectId.Should().Be("sodium");
        File.Exists(Path.Combine(_instanceDir, "mods", "sodium.jar")).Should().BeTrue();
    }

    [Fact]
    public async Task PerformancePack_SodiumFail_Throws_ModsJsonEmpty()
    {
        // Arrange
        _modrinthMock.Setup(m => m.GetProjectAsync("sodium", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Sodium download failure"));

        // Act
        var act = () => _service.InstallPerformancePackAsync("test-inst");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        var manifest = await ReadManifestAsync();
        manifest.Mods.Should().BeEmpty();
    }

    [Fact]
    public void TempDirectoryCleanup_ViaIDisposable_DeletesDirectory()
    {
        // Arrange: create a directory in tempRoot
        var subDir = Path.Combine(_tempRoot, "to-be-deleted");
        Directory.CreateDirectory(subDir);
        Directory.Exists(subDir).Should().BeTrue();

        // Act: Dispose test instance
        Dispose();

        // Assert
        Directory.Exists(_tempRoot).Should().BeFalse();
    }

    [Fact]
    public async Task UninstallAsync_RemovesFromModsJsonAndDisk()
    {
        // Arrange
        var content = new byte[] { 1 };
        var modsDir = Path.Combine(_instanceDir, "mods");
        Directory.CreateDirectory(modsDir);
        var jarFile = Path.Combine(modsDir, "modA.jar");
        await File.WriteAllBytesAsync(jarFile, content);

        var manifest = new ModsManifest
        {
            Mods = new List<InstalledMod>
            {
                new InstalledMod
                {
                    FileName = "modA.jar",
                    ProjectId = "modA",
                    VersionId = "v1",
                    Source = "modrinth",
                    Enabled = true,
                    InstalledAt = DateTimeOffset.UtcNow
                }
            }
        };
        await WriteManifestAsync(manifest);

        // Act
        await _service.UninstallAsync("test-inst", "modA.jar");

        // Assert
        File.Exists(jarFile).Should().BeFalse();
        var updated = await ReadManifestAsync();
        updated.Mods.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAllAsync_UpdatesOutdatedMods_ReturnsCount()
    {
        // Arrange
        var contentV1 = new byte[] { 1 };
        var contentV2 = new byte[] { 2 };
        var shaV2 = ComputeSha512Hex(contentV2);

        var manifest = new ModsManifest
        {
            Mods = new List<InstalledMod>
            {
                new InstalledMod
                {
                    FileName = "modA.jar",
                    ProjectId = "modA",
                    VersionId = "v1",
                    Source = "modrinth",
                    Enabled = true,
                    InstalledAt = DateTimeOffset.UtcNow
                }
            }
        };
        await WriteManifestAsync(manifest);

        var projA = CreateProject("modA", "modA", "Mod A");
        var verV1 = CreateVersion("v1", "modA", "1.0", "release", "modA-1.0.jar", contentV1, datePublished: DateTimeOffset.UtcNow.AddDays(-2));
        var verV2 = CreateVersion("v2", "modA", "2.0", "release", "modA-2.0.jar", contentV2, shaV2, datePublished: DateTimeOffset.UtcNow.AddDays(-1));

        _modrinthMock.Setup(m => m.GetProjectAsync("modA", It.IsAny<CancellationToken>())).ReturnsAsync(projA);
        _modrinthMock.Setup(m => m.GetVersionsAsync("modA", "fabric", "1.21.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { verV1, verV2 });
        _modrinthMock.Setup(m => m.GetVersionAsync("v2", It.IsAny<CancellationToken>())).ReturnsAsync(verV2);
        _modrinthMock.Setup(m => m.DownloadAsync(verV2.Files[0].Url, It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream(contentV2)));

        // Act
        int updated = await _service.UpdateAllAsync("test-inst");

        // Assert
        updated.Should().Be(1);
        var updatedManifest = await ReadManifestAsync();
        updatedManifest.Mods[0].VersionId.Should().Be("v2");
    }

    [Fact]
    public async Task InstallFromModrinthAsync_BetaFallback_WhenNoReleaseExists_PicksBetaWithWarning()
    {
        // Arrange: 0 release + 1 beta (không có alpha)
        var content = new byte[] { 7, 7, 7 };
        var sha = ComputeSha512Hex(content);

        var project = CreateProject("mod-beta", "mod-beta", "Mod Beta");
        var vBeta = CreateVersion("vB1", "mod-beta", "1.0-beta", "beta", "mod-beta-beta.jar", content, sha);

        _modrinthMock.Setup(m => m.GetProjectAsync("mod-beta", It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _modrinthMock.Setup(m => m.GetVersionsAsync("mod-beta", "fabric", "1.21.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { vBeta });
        _modrinthMock.Setup(m => m.DownloadAsync(vBeta.Files[0].Url, It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream(content)));

        // Act
        var result = await _service.InstallFromModrinthAsync("test-inst", "mod-beta");

        // Assert
        result.Success.Should().BeTrue();
        result.VersionId.Should().Be("vB1");
        result.Warning.Should().NotBeNullOrWhiteSpace();
        File.Exists(Path.Combine(_instanceDir, "mods", "mod-beta-beta.jar")).Should().BeTrue();
    }

    [Fact]
    public async Task InstallFromModrinthAsync_AlphaOnly_Throws_NoAlphaInstalled()
    {
        // Arrange: chỉ có alpha
        var content = new byte[] { 8, 8 };
        var sha = ComputeSha512Hex(content);

        var project = CreateProject("mod-alpha", "mod-alpha", "Mod Alpha");
        var vAlpha = CreateVersion("vA1", "mod-alpha", "1.0-alpha", "alpha", "mod-alpha-alpha.jar", content, sha);

        _modrinthMock.Setup(m => m.GetProjectAsync("mod-alpha", It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _modrinthMock.Setup(m => m.GetVersionsAsync("mod-alpha", "fabric", "1.21.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { vAlpha });

        // Act
        var act = () => _service.InstallFromModrinthAsync("test-inst", "mod-alpha");

        // Assert: KHÔNG bao giờ auto-pick alpha
        await act.Should().ThrowAsync<InvalidOperationException>();
        File.Exists(Path.Combine(_instanceDir, "mods", "mod-alpha-alpha.jar")).Should().BeFalse();
        (await ReadManifestAsync()).Mods.Should().BeEmpty();
    }

    [Fact]
    public async Task InstallFromModrinthAsync_PrimaryFilePreferredOverFirst()
    {
        // Arrange: file [0] primary=false, file [1] primary=true
        var content = new byte[] { 9, 9, 9 };
        var sha = ComputeSha512Hex(content);

        var project = CreateProject("mod-p", "mod-p", "Mod P");
        var version = new ProjectVersion
        {
            Id = "vP1",
            ProjectId = "mod-p",
            VersionNumber = "1.0",
            VersionType = "release",
            DatePublished = DateTimeOffset.UtcNow,
            Dependencies = Array.Empty<VersionDependency>(),
            Files = new[]
            {
                new VersionFile
                {
                    Filename = "first.jar",
                    Primary = false,
                    Size = content.Length,
                    Url = "https://cdn.modrinth.com/first.jar",
                    Hashes = new FileHashes { Sha512 = sha }
                },
                new VersionFile
                {
                    Filename = "primary.jar",
                    Primary = true,
                    Size = content.Length,
                    Url = "https://cdn.modrinth.com/primary.jar",
                    Hashes = new FileHashes { Sha512 = sha }
                }
            }
        };

        _modrinthMock.Setup(m => m.GetProjectAsync("mod-p", It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _modrinthMock.Setup(m => m.GetVersionsAsync("mod-p", "fabric", "1.21.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { version });
        _modrinthMock.Setup(m => m.DownloadAsync("https://cdn.modrinth.com/primary.jar", It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream(content)));

        // Act
        var result = await _service.InstallFromModrinthAsync("test-inst", "mod-p");

        // Assert
        result.Success.Should().BeTrue();
        result.FileName.Should().Be("primary.jar");
        File.Exists(Path.Combine(_instanceDir, "mods", "primary.jar")).Should().BeTrue();
        File.Exists(Path.Combine(_instanceDir, "mods", "first.jar")).Should().BeFalse();
    }

    [Fact]
    public async Task InstallFromModrinthAsync_FileNameCollision_SuffixesDash1()
    {
        // Arrange: 2 mod khác projectId nhưng cùng filename "foo.jar"
        var content1 = new byte[] { 11 };
        var content2 = new byte[] { 22 };
        var sha1 = ComputeSha512Hex(content1);
        var sha2 = ComputeSha512Hex(content2);

        var proj1 = CreateProject("mod-1", "mod-1", "Mod 1");
        var proj2 = CreateProject("mod-2", "mod-2", "Mod 2");
        var ver1 = CreateVersion("v1", "mod-1", "1.0", "release", "foo.jar", content1, sha1);
        var ver2 = CreateVersion("v2", "mod-2", "1.0", "release", "foo.jar", content2, sha2);
        // Hai version cùng filename nên URL mặc định trùng nhau (CreateVersion build URL từ filename).
        // Moq: setup sau đè setup trước → cả 2 download đều trả content2 → r1 mismatch SHA512.
        // Cho ver2 URL riêng để mỗi mock trả đúng bytes của mình.
        ver2 = ver2 with { Files = new[] { ver2.Files[0] with { Url = "https://cdn.modrinth.com/foo2.jar" } } };

        _modrinthMock.Setup(m => m.GetProjectAsync("mod-1", It.IsAny<CancellationToken>())).ReturnsAsync(proj1);
        _modrinthMock.Setup(m => m.GetProjectAsync("mod-2", It.IsAny<CancellationToken>())).ReturnsAsync(proj2);
        _modrinthMock.Setup(m => m.GetVersionsAsync("mod-1", "fabric", "1.21.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ver1 });
        _modrinthMock.Setup(m => m.GetVersionsAsync("mod-2", "fabric", "1.21.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ver2 });
        _modrinthMock.Setup(m => m.DownloadAsync(ver1.Files[0].Url, It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream(content1)));
        _modrinthMock.Setup(m => m.DownloadAsync(ver2.Files[0].Url, It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream(content2)));

        // Act
        var r1 = await _service.InstallFromModrinthAsync("test-inst", "mod-1");
        var r2 = await _service.InstallFromModrinthAsync("test-inst", "mod-2");

        // Assert
        r1.FileName.Should().Be("foo.jar");
        r2.FileName.Should().Be("foo-1.jar");
        var manifest = await ReadManifestAsync();
        manifest.Mods.Should().HaveCount(2);
        manifest.Mods.Select(m => m.FileName).Should().Contain(new[] { "foo.jar", "foo-1.jar" });
    }

    [Fact]
    public async Task InstallFromModrinthAsync_OptionalDependency_Skipped()
    {
        // Arrange: mod chính + 1 optional dep
        var contentMain = new byte[] { 33 };
        var shaMain = ComputeSha512Hex(contentMain);

        var projMain = CreateProject("mod-main", "mod-main", "Mod Main");
        var projOpt = CreateProject("mod-opt", "mod-opt", "Mod Opt");
        var optDep = new VersionDependency { ProjectId = "mod-opt", DependencyType = "optional" };
        var verMain = CreateVersion("vM", "mod-main", "1.0", "release", "mod-main.jar", contentMain, shaMain,
            dependencies: new[] { optDep });

        _modrinthMock.Setup(m => m.GetProjectAsync("mod-main", It.IsAny<CancellationToken>())).ReturnsAsync(projMain);
        _modrinthMock.Setup(m => m.GetProjectAsync("mod-opt", It.IsAny<CancellationToken>())).ReturnsAsync(projOpt);
        _modrinthMock.Setup(m => m.GetVersionsAsync("mod-main", "fabric", "1.21.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { verMain });
        _modrinthMock.Setup(m => m.DownloadAsync(verMain.Files[0].Url, It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream(contentMain)));

        // Act
        var result = await _service.InstallFromModrinthAsync("test-inst", "mod-main");

        // Assert: chỉ cài mod chính, KHÔNG cài optional dep
        result.Success.Should().BeTrue();
        result.InstalledDependencies.Should().BeEmpty();
        var manifest = await ReadManifestAsync();
        manifest.Mods.Should().HaveCount(1);
        manifest.Mods[0].ProjectId.Should().Be("mod-main");
        _modrinthMock.Verify(m => m.GetProjectAsync("mod-opt", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAllAsync_OneModFails_SkipsIt_ContinuesOthers()
    {
        // Arrange: 3 mod đã cài, mod thứ 2 fail khi update
        var contentNew1 = new byte[] { 41 };
        var contentNew2 = new byte[] { 42 };
        var contentNew3 = new byte[] { 43 };
        var shaNew1 = ComputeSha512Hex(contentNew1);
        var shaNew3 = ComputeSha512Hex(contentNew3);

        var manifest = new ModsManifest
        {
            Mods = new List<InstalledMod>
            {
                new InstalledMod { FileName = "m1.jar", ProjectId = "m1", VersionId = "v1", Source = "modrinth", Enabled = true, InstalledAt = DateTimeOffset.UtcNow },
                new InstalledMod { FileName = "m2.jar", ProjectId = "m2", VersionId = "v1", Source = "modrinth", Enabled = true, InstalledAt = DateTimeOffset.UtcNow },
                new InstalledMod { FileName = "m3.jar", ProjectId = "m3", VersionId = "v1", Source = "modrinth", Enabled = true, InstalledAt = DateTimeOffset.UtcNow }
            }
        };
        await WriteManifestAsync(manifest);

        SetupUpdatePair("m1", "v2", contentNew1, shaNew1);
        SetupUpdatePair("m3", "v2", contentNew3, shaNew3);

        // mod2: có version mới nhưng download fail
        var proj2 = CreateProject("m2", "m2", "M2");
        var ver2New = CreateVersion("v2-m2", "m2", "2.0", "release", "m2-2.0.jar", contentNew2);
        _modrinthMock.Setup(m => m.GetProjectAsync("m2", It.IsAny<CancellationToken>())).ReturnsAsync(proj2);
        _modrinthMock.Setup(m => m.GetVersionsAsync("m2", "fabric", "1.21.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ver2New });
        _modrinthMock.Setup(m => m.GetVersionAsync("v2-m2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ver2New);
        _modrinthMock.Setup(m => m.DownloadAsync(ver2New.Files[0].Url, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("simulated download failure"));

        // Act
        int updated = await _service.UpdateAllAsync("test-inst");

        // Assert
        updated.Should().Be(2);
        var after = await ReadManifestAsync();
        after.Mods.Should().HaveCount(3);
        after.Mods.First(m => m.ProjectId == "m1").VersionId.Should().Be("v2");
        after.Mods.First(m => m.ProjectId == "m2").VersionId.Should().Be("v1");
        after.Mods.First(m => m.ProjectId == "m3").VersionId.Should().Be("v2");
    }

    [Fact]
    public async Task InstallFromModrinthAsync_DependencyChainDepth6_InstallsSixMods_BlocksSeventh()
    {
        // Arrange: A -> B -> C -> D -> E -> F -> G (mỗi mod required dep mod kế)
        string[] chain = { "modA", "modB", "modC", "modD", "modE", "modF", "modG" };
        for (int i = 0; i < chain.Length; i++)
        {
            var content = new byte[] { (byte)(50 + i) };
            var sha = ComputeSha512Hex(content);
            var proj = CreateProject(chain[i], chain[i], $"Mod {chain[i]}");

            VersionDependency[] deps = i < chain.Length - 1
                ? new[] { new VersionDependency { ProjectId = chain[i + 1], DependencyType = "required" } }
                : Array.Empty<VersionDependency>();
            var ver = CreateVersion($"v-{chain[i]}", chain[i], "1.0", "release", $"{chain[i]}.jar", content, sha,
                dependencies: deps);

            _modrinthMock.Setup(m => m.GetProjectAsync(chain[i], It.IsAny<CancellationToken>())).ReturnsAsync(proj);
            _modrinthMock.Setup(m => m.GetVersionsAsync(chain[i], "fabric", "1.21.1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { ver });
            _modrinthMock.Setup(m => m.DownloadAsync(ver.Files[0].Url, It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Stream>(new MemoryStream(content)));
        }

        // Act: dùng timeout để phát hiện treo vô hạn
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        var result = await _service.InstallFromModrinthAsync("test-inst", "modA", null, true, null, cts.Token);

        // Assert: depth<=5 nghĩa là depth 0..5 = 6 levels → cài 6 mod (A-F), mod G (depth 6) bị chặn
        result.Success.Should().BeTrue();
        var after = await ReadManifestAsync();
        after.Mods.Should().HaveCount(6);
        after.Mods.Select(m => m.ProjectId).Should().Contain(new[] { "modA", "modB", "modC", "modD", "modE", "modF" });
        after.Mods.Select(m => m.ProjectId).Should().NotContain(new[] { "modG" });
        File.Exists(Path.Combine(_instanceDir, "mods", "modF.jar")).Should().BeTrue();
        File.Exists(Path.Combine(_instanceDir, "mods", "modG.jar")).Should().BeFalse();
    }

    private void SetupUpdatePair(string projectId, string newVersionId, byte[] newContent, string newSha)
    {
        var proj = CreateProject(projectId, projectId, projectId);
        var verNew = CreateVersion(newVersionId, projectId, "2.0", "release", $"{projectId}-2.0.jar", newContent, newSha);

        _modrinthMock.Setup(m => m.GetProjectAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(proj);
        _modrinthMock.Setup(m => m.GetVersionsAsync(projectId, "fabric", "1.21.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { verNew });
        _modrinthMock.Setup(m => m.GetVersionAsync(newVersionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(verNew);
        _modrinthMock.Setup(m => m.DownloadAsync(verNew.Files[0].Url, It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream(newContent)));
    }

    private void SetupSodiumMock(string loader)
    {
        var sodiumContent = new byte[] { 5, 5 };
        var sodiumSha = ComputeSha512Hex(sodiumContent);
        var sodiumProj = CreateProject("sodium", "sodium", "Sodium");
        var sodiumVer = CreateVersion("v-sodium", "sodium", "0.5.8", "release", "sodium.jar", sodiumContent, sodiumSha);

        _modrinthMock.Setup(m => m.GetProjectAsync("sodium", It.IsAny<CancellationToken>())).ReturnsAsync(sodiumProj);
        _modrinthMock.Setup(m => m.GetVersionsAsync("sodium", loader, "1.21.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { sodiumVer });
        _modrinthMock.Setup(m => m.DownloadAsync(sodiumVer.Files[0].Url, It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream(sodiumContent)));
    }

    private void SetupSodiumAndIrisMocks(string loader)
    {
        SetupSodiumMock(loader);

        var irisContent = new byte[] { 6, 6 };
        var irisSha = ComputeSha512Hex(irisContent);
        var irisProj = CreateProject("iris", "iris", "Iris");
        var irisVer = CreateVersion("v-iris", "iris", "1.7.0", "release", "iris.jar", irisContent, irisSha);

        _modrinthMock.Setup(m => m.GetProjectAsync("iris", It.IsAny<CancellationToken>())).ReturnsAsync(irisProj);
        _modrinthMock.Setup(m => m.GetVersionsAsync("iris", loader, "1.21.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { irisVer });
        _modrinthMock.Setup(m => m.DownloadAsync(irisVer.Files[0].Url, It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream(irisContent)));
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
        byte[] content,
        string? sha512 = null,
        bool primary = true,
        DateTimeOffset? datePublished = null,
        VersionDependency[]? dependencies = null)
    {
        return new ProjectVersion
        {
            Id = versionId,
            ProjectId = projectId,
            VersionNumber = versionNumber,
            VersionType = versionType,
            DatePublished = datePublished ?? DateTimeOffset.UtcNow,
            Dependencies = dependencies ?? Array.Empty<VersionDependency>(),
            Files = new[]
            {
                new VersionFile
                {
                    Filename = filename,
                    Primary = primary,
                    Size = content.Length,
                    Url = $"https://cdn.modrinth.com/{filename}",
                    Hashes = new FileHashes
                    {
                        Sha512 = sha512
                    }
                }
            }
        };
    }

    private static string ComputeSha512Hex(byte[] data)
    {
        using var sha = SHA512.Create();
        return Convert.ToHexString(sha.ComputeHash(data)).ToLowerInvariant();
    }

    private async Task<ModsManifest> ReadManifestAsync()
    {
        var manifestPath = Path.Combine(_instanceDir, "mods.json");
        if (!File.Exists(manifestPath))
        {
            return new ModsManifest();
        }

        var json = await File.ReadAllTextAsync(manifestPath);
        return JsonSerializer.Deserialize<ModsManifest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ModsManifest();
    }

    private async Task WriteManifestAsync(ModsManifest manifest)
    {
        var manifestPath = Path.Combine(_instanceDir, "mods.json");
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await File.WriteAllTextAsync(manifestPath, json);
    }
}
