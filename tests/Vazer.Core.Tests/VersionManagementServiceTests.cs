using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Vazer.Core;
using Vazer.Core.Abstractions;
using Vazer.Core.Versions;
using Xunit;

namespace Vazer.Core.Tests;

public sealed class VersionManagementServiceTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "vaz-ver-tests", Guid.NewGuid().ToString("N"));
    private readonly string _versionsDir;
    private readonly Mock<IInstanceManager> _instanceManagerMock = new();
    private readonly IOptions<MinecraftOptions> _options;

    public VersionManagementServiceTests()
    {
        _versionsDir = Path.Combine(_tempRoot, "instances", "versions");
        Directory.CreateDirectory(_versionsDir);
        _options = Options.Create(new MinecraftOptions { RootPath = _tempRoot });
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, true); } catch { }
    }

    private string CreateVersionDir(string versionId, string type = "release", long dummyFileBytes = 1024)
    {
        var dir = Path.Combine(_versionsDir, versionId);
        Directory.CreateDirectory(dir);
        var json = $"{{\"id\": \"{versionId}\", \"type\": \"{type}\"}}";
        File.WriteAllText(Path.Combine(dir, $"{versionId}.json"), json);
        if (dummyFileBytes > 0)
        {
            File.WriteAllBytes(Path.Combine(dir, $"{versionId}.jar"), new byte[dummyFileBytes]);
        }
        return dir;
    }

    [Fact]
    public async Task ListInstalledVersionsAsync_EmptyFolder_ReturnsEmptyList()
    {
        // 1. List empty: folder versions/ rỗng -> trả list rỗng
        var svc = new VersionManagementService(_instanceManagerMock.Object, _options, NullLogger<VersionManagementService>.Instance);
        var result = await svc.ListInstalledVersionsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListInstalledVersionsAsync_ThreeVersions_ReturnsThreeCorrectItems()
    {
        // 2. List with 3 versions: mock folder + 3 subfolder -> trả 3 item đúng
        CreateVersionDir("1.20.1", "release", dummyFileBytes: 2048);
        CreateVersionDir("fabric-loader-0.19.5-1.21.1", "custom", dummyFileBytes: 4096);
        CreateVersionDir("1.20.1-forge-47.4.28", "custom", dummyFileBytes: 8192);

        _instanceManagerMock
            .Setup(m => m.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MinecraftInstance>());

        var svc = new VersionManagementService(_instanceManagerMock.Object, _options, NullLogger<VersionManagementService>.Instance);
        var result = await svc.ListInstalledVersionsAsync();

        result.Should().HaveCount(3);
        result.Select(v => v.VersionId).Should().Contain(new[]
        {
            "1.20.1",
            "fabric-loader-0.19.5-1.21.1",
            "1.20.1-forge-47.4.28"
        });

        var vanilla = result.First(v => v.VersionId == "1.20.1");
        vanilla.Type.Should().Be("Vanilla");
        vanilla.SizeBytes.Should().BeGreaterThan(2048);
        vanilla.IsInUse.Should().BeFalse();

        var fabric = result.First(v => v.VersionId == "fabric-loader-0.19.5-1.21.1");
        fabric.Type.Should().Be("Fabric");

        var forge = result.First(v => v.VersionId == "1.20.1-forge-47.4.28");
        forge.Type.Should().Be("Forge");
    }

    [Fact]
    public async Task ListInstalledVersionsAsync_UsedByInstances_MapsInstancesCorrectly()
    {
        // 3. UsedByInstances: mock 1 version được dùng bởi 2 instance -> UsedByInstances.Count == 2
        CreateVersionDir("1.20.1", "release");

        var instances = new List<MinecraftInstance>
        {
            new()
            {
                Id = "inst-1",
                Name = "Survival 1.20.1",
                MinecraftVersion = "1.20.1",
                LoaderType = "vanilla",
                LaunchVersionName = "1.20.1",
                Path = Path.Combine(_tempRoot, "instances", "inst-1"),
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = "inst-2",
                Name = "Hardcore 1.20.1",
                MinecraftVersion = "1.20.1",
                LoaderType = "vanilla",
                LaunchVersionName = null, // fallback check qua MinecraftVersion
                Path = Path.Combine(_tempRoot, "instances", "inst-2"),
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _instanceManagerMock
            .Setup(m => m.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(instances);

        var svc = new VersionManagementService(_instanceManagerMock.Object, _options, NullLogger<VersionManagementService>.Instance);
        var result = await svc.ListInstalledVersionsAsync();

        result.Should().HaveCount(1);
        var item = result[0];
        item.VersionId.Should().Be("1.20.1");
        item.UsedByInstances.Should().HaveCount(2);
        item.UsedByInstances.Should().Contain(new[] { "Survival 1.20.1", "Hardcore 1.20.1" });
        item.IsInUse.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteVersionAsync_VersionInUse_ReturnsBlockedAndDoesNotDeleteFolder()
    {
        // 4. Delete version đang dùng: return DeleteResult.Blocked (không xóa folder)
        CreateVersionDir("1.21.1", "release");

        var instances = new List<MinecraftInstance>
        {
            new()
            {
                Id = "inst-active",
                Name = "Active 1.21.1",
                MinecraftVersion = "1.21.1",
                LoaderType = "vanilla",
                LaunchVersionName = "1.21.1",
                Path = Path.Combine(_tempRoot, "instances", "inst-active"),
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _instanceManagerMock
            .Setup(m => m.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(instances);

        var svc = new VersionManagementService(_instanceManagerMock.Object, _options, NullLogger<VersionManagementService>.Instance);
        var result = await svc.DeleteVersionAsync("1.21.1");

        result.Success.Should().BeFalse();
        result.Blocked.Should().BeTrue();
        result.Reason.Should().Contain("đang được sử dụng bởi 1 instance");

        // Folder vẫn tồn tại
        Directory.Exists(Path.Combine(_versionsDir, "1.21.1")).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteVersionAsync_OrphanVersion_DeletesFolderAndReturnsSuccess()
    {
        // 5. Delete version orphan: xóa folder thành công, DeleteResult.Success
        CreateVersionDir("1.20.6-orphan", "release", dummyFileBytes: 4096);

        _instanceManagerMock
            .Setup(m => m.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MinecraftInstance>());

        var svc = new VersionManagementService(_instanceManagerMock.Object, _options, NullLogger<VersionManagementService>.Instance);
        var result = await svc.DeleteVersionAsync("1.20.6-orphan");

        result.Success.Should().BeTrue();
        result.Blocked.Should().BeFalse();
        result.FreedBytes.Should().BeGreaterThan(4096);

        // Folder đã bị xóa
        Directory.Exists(Path.Combine(_versionsDir, "1.20.6-orphan")).Should().BeFalse();
    }

    [Fact]
    public async Task CleanupOrphanVersionsAsync_CleansOnlyUnusedVersions()
    {
        CreateVersionDir("1.20.1-used", "release");
        CreateVersionDir("1.20.6-orphan1", "release");
        CreateVersionDir("1.21.0-orphan2", "release");

        var instances = new List<MinecraftInstance>
        {
            new()
            {
                Id = "inst-1",
                Name = "Main Instance",
                MinecraftVersion = "1.20.1-used",
                LoaderType = "vanilla",
                LaunchVersionName = "1.20.1-used",
                Path = Path.Combine(_tempRoot, "instances", "inst-1"),
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _instanceManagerMock
            .Setup(m => m.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(instances);

        var svc = new VersionManagementService(_instanceManagerMock.Object, _options, NullLogger<VersionManagementService>.Instance);
        var cleanup = await svc.CleanupOrphanVersionsAsync();

        cleanup.Success.Should().BeTrue();
        cleanup.CleanedCount.Should().Be(2);
        cleanup.DeletedVersionIds.Should().Contain(new[] { "1.20.6-orphan1", "1.21.0-orphan2" });

        // Version đang dùng vẫn còn nguyên
        Directory.Exists(Path.Combine(_versionsDir, "1.20.1-used")).Should().BeTrue();
        // 2 versions orphan đã bị xóa
        Directory.Exists(Path.Combine(_versionsDir, "1.20.6-orphan1")).Should().BeFalse();
        Directory.Exists(Path.Combine(_versionsDir, "1.21.0-orphan2")).Should().BeFalse();
    }
}
