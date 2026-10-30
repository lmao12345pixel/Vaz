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

public sealed class ModInstallBulkUpdateTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _instanceDir;
    private readonly Mock<IModrinthClient> _modrinthMock;
    private readonly Dictionary<string, ProjectVersion> _hashMap;
    private readonly List<string> _requestedHashes;
    private readonly ModInstallService _service;

    public ModInstallBulkUpdateTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "upco-bulk-tests", Guid.NewGuid().ToString("N"));
        _instanceDir = Path.Combine(_tempRoot, "instances", "test-inst");
        Directory.CreateDirectory(_instanceDir);

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

        _hashMap = new Dictionary<string, ProjectVersion>();
        _requestedHashes = new List<string>();

        _modrinthMock = new Mock<IModrinthClient>();
        _modrinthMock.Setup(m => m.GetVersionsFromHashesAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback((IEnumerable<string> hashes, CancellationToken _) => _requestedHashes.AddRange(hashes))
            .ReturnsAsync((IEnumerable<string> hashes, CancellationToken _) =>
                hashes.Where(h => _hashMap.ContainsKey(h))
                      .ToDictionary(h => h, h => _hashMap[h]));

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
    public async Task ResolvedNewerVersion_MarksHasUpdate()
    {
        var sha = Sha(new byte[] { 1, 1, 1 });
        await WriteManifestAsync(new ModsManifest { Mods = new List<InstalledMod> { Mod("m1.jar", "v1", sha) } });
        _hashMap[sha] = Ver("v2", "1.1.0");

        var result = await _service.CheckUpdatesByHashAsync("test-inst");

        result.Updates.Should().HaveCount(1);
        result.Updates[0].CurrentMod.FileName.Should().Be("m1.jar");
        result.Updates[0].HasUpdate.Should().BeTrue();
        result.Updates[0].LatestVersionId.Should().Be("v2");
        result.Updates[0].LatestVersionNumber.Should().Be("1.1.0");
        _modrinthMock.Verify(
            m => m.GetVersionsFromHashesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResolvedSameVersionId_NoUpdate()
    {
        var sha = Sha(new byte[] { 2, 2, 2 });
        await WriteManifestAsync(new ModsManifest { Mods = new List<InstalledMod> { Mod("m2.jar", "v1", sha) } });
        _hashMap[sha] = Ver("v1", "1.0.0");

        var result = await _service.CheckUpdatesByHashAsync("test-inst");

        result.Updates.Should().HaveCount(1);
        result.Updates[0].HasUpdate.Should().BeFalse();
    }

    [Fact]
    public async Task ResolvedVersionNumberMatchesInstalledVersionId_NoUpdate()
    {
        var sha = Sha(new byte[] { 3, 3, 3 });
        await WriteManifestAsync(new ModsManifest { Mods = new List<InstalledMod> { Mod("m3.jar", "1.0.0", sha) } });
        _hashMap[sha] = Ver("xyz", "1.0.0");

        var result = await _service.CheckUpdatesByHashAsync("test-inst");

        result.Updates.Should().HaveCount(1);
        result.Updates[0].HasUpdate.Should().BeFalse();
    }

    [Fact]
    public async Task UnresolvedHash_FallsBackToNoUpdate()
    {
        await WriteManifestAsync(new ModsManifest { Mods = new List<InstalledMod> { Mod("m4.jar", "v1", Sha(new byte[] { 4, 4, 4 })) } });
        // _hashMap rỗng → server không nhận diện hash nào

        var result = await _service.CheckUpdatesByHashAsync("test-inst");

        result.Updates.Should().HaveCount(1);
        result.Updates[0].HasUpdate.Should().BeFalse();
        result.Updates[0].LatestVersionId.Should().Be("v1");
    }

    [Fact]
    public async Task CustomSourceMods_ExcludedFromBulkCheck()
    {
        var sha = Sha(new byte[] { 5, 5, 5 });
        await WriteManifestAsync(new ModsManifest
        {
            Mods = new List<InstalledMod>
            {
                Mod("from-modrinth.jar", "v1", sha),
                Mod("custom-manual.jar", "1.0.0", Sha(new byte[] { 6, 6, 6 })) with { Source = "custom" }
            }
        });

        var result = await _service.CheckUpdatesByHashAsync("test-inst");

        result.Updates.Should().ContainSingle(u => u.CurrentMod.FileName == "from-modrinth.jar");
        _requestedHashes.Should().ContainSingle().Which.Should().Be(sha);
    }

    [Fact]
    public async Task MissingManifestHash_ComputesFromDiskFile()
    {
        var bytes = new byte[] { 9, 8, 7, 6, 5, 4 };
        var sha = Sha(bytes);
        Directory.CreateDirectory(Path.Combine(_instanceDir, "mods"));
        await File.WriteAllBytesAsync(Path.Combine(_instanceDir, "mods", "ondisk.jar"), bytes);

        var installed = Mod("ondisk.jar", "v1", "") with { Sha512 = null };
        await WriteManifestAsync(new ModsManifest { Mods = new List<InstalledMod> { installed } });
        _hashMap[sha] = Ver("v2", "2.0.0");

        var result = await _service.CheckUpdatesByHashAsync("test-inst");

        result.Updates.Should().HaveCount(1);
        result.Updates[0].HasUpdate.Should().BeTrue();
        result.Updates[0].LatestVersionId.Should().Be("v2");
        _requestedHashes.Should().ContainSingle().Which.Should().Be(sha);
    }

    [Fact]
    public async Task Chunking_150Mods_MakesTwoBulkCalls()
    {
        var mods = new List<InstalledMod>();
        for (var i = 0; i < 150; i++)
        {
            var sha = Sha(new byte[] { 1, 1, 1, (byte)(i % 256), (byte)(i / 256) });
            mods.Add(Mod($"m{i}.jar", $"v{i}", sha));
        }

        await WriteManifestAsync(new ModsManifest { Mods = mods });

        var result = await _service.CheckUpdatesByHashAsync("test-inst");

        result.Updates.Should().HaveCount(150);
        _modrinthMock.Verify(
            m => m.GetVersionsFromHashesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        _requestedHashes.Should().HaveCount(150);
        _requestedHashes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task DuplicateHashes_AcrossMods_SentOnlyOnce()
    {
        var sha = Sha(new byte[] { 7, 7, 7 });
        await WriteManifestAsync(new ModsManifest
        {
            Mods = new List<InstalledMod> { Mod("one.jar", "v1", sha), Mod("two.jar", "v1", sha) }
        });

        var result = await _service.CheckUpdatesByHashAsync("test-inst");

        result.Updates.Should().HaveCount(2);
        _requestedHashes.Should().ContainSingle().Which.Should().Be(sha);
    }

    private static InstalledMod Mod(string fileName, string versionId, string sha512)
    {
        return new InstalledMod
        {
            FileName = fileName,
            ProjectId = fileName.Replace(".jar", string.Empty),
            VersionId = versionId,
            Source = "modrinth",
            Enabled = true,
            InstalledAt = DateTimeOffset.UtcNow,
            Sha512 = sha512
        };
    }

    private static ProjectVersion Ver(string id, string versionNumber)
    {
        return new ProjectVersion
        {
            Id = id,
            VersionNumber = versionNumber,
            VersionType = "release",
            DatePublished = DateTimeOffset.UtcNow,
            Dependencies = Array.Empty<VersionDependency>(),
            Files = Array.Empty<VersionFile>()
        };
    }

    private static string Sha(byte[] data)
    {
        return Convert.ToHexString(SHA512.HashData(data)).ToLowerInvariant();
    }

    private async Task WriteManifestAsync(ModsManifest manifest)
    {
        var json = JsonSerializer.Serialize(
            manifest,
            new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await File.WriteAllTextAsync(Path.Combine(_instanceDir, "mods.json"), json);
    }
}
