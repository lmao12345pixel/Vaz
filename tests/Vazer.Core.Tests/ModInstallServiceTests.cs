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
            .ReturnsAsync(new MemoryStream(content));

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

        _modrinthMock.Setup(m => m.DownloadAsync(verA.Files[0].Url, It.IsAny<CancellationToken>())).ReturnsAsync(new MemoryStream(contentA));
        _modrinthMock.Setup(m => m.DownloadAsync(verB.Files[0].Url, It.IsAny<CancellationToken>())).ReturnsAsync(new MemoryStream(contentB));

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
        _modrinthMock.Setup(m => m.DownloadAsync(verA2.Files[0].Url, It.IsAny<CancellationToken>())).ReturnsAsync(new MemoryStream(newContent));

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
        _modrinthMock.Setup(m => m.DownloadAsync(ver.Files[0].Url, It.IsAny<CancellationToken>())).ReturnsAsync(new MemoryStream(content));

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
            .ReturnsAsync(new MemoryStream(contentV2));

        // Act
        int updated = await _service.UpdateAllAsync("test-inst");

        // Assert
        updated.Should().Be(1);
        var updatedManifest = await ReadManifestAsync();
        updatedManifest.Mods[0].VersionId.Should().Be("v2");
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
            .ReturnsAsync(new MemoryStream(sodiumContent));
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
            .ReturnsAsync(new MemoryStream(irisContent));
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
