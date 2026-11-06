using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Vazer.Core.Abstractions;
using Vazer.Core.Instances;
using Vazer.Core.Json;
using Vazer.Core.Loaders;
using Vazer.Core.Mods;
using Xunit;

namespace Vazer.Core.Tests;

public sealed class InstanceMigrationServiceTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "vaz-mig-tests", Guid.NewGuid().ToString("N"));
    private readonly Mock<IInstanceManager> _instanceManagerMock = new();
    private readonly Mock<IInstanceCreationService> _creationServiceMock = new();

    public InstanceMigrationServiceTests()
    {
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, true); } catch { }
    }

    private (string InstancePath, MinecraftInstance Instance) SeedInstance(
        string id = "test-inst",
        string name = "Test Instance",
        string mcVersion = "1.21.1",
        string loader = "fabric",
        string? loaderVersion = "0.20.1")
    {
        var dir = Path.Combine(_tempRoot, id);
        Directory.CreateDirectory(dir);
        var modsDir = Path.Combine(dir, "mods");
        Directory.CreateDirectory(modsDir);

        File.WriteAllText(Path.Combine(modsDir, "mod1.jar"), "dummy jar 1");
        File.WriteAllText(Path.Combine(modsDir, "mod2.jar"), "dummy jar 2");

        var modsManifest = new ModsManifest
        {
            Mods = new List<InstalledMod>
            {
                new() { FileName = "mod1.jar", ProjectId = "mod1", VersionId = "1.0", Enabled = true },
                new() { FileName = "mod2.jar", ProjectId = "mod2", VersionId = "1.0", Enabled = true }
            }
        };
        var modsJson = JsonSerializer.Serialize(modsManifest, VazJsonContext.CamelCase.ModsManifest);
        File.WriteAllText(Path.Combine(dir, "mods.json"), modsJson);

        var inst = new MinecraftInstance
        {
            Id = id,
            Name = name,
            MinecraftVersion = mcVersion,
            LoaderType = loader,
            LoaderVersion = loaderVersion,
            LaunchVersionName = $"{loader}-loader-{loaderVersion}-{mcVersion}",
            Path = dir,
            CreatedAt = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero),
            LastPlayedAt = new DateTimeOffset(2026, 11, 5, 0, 0, 0, TimeSpan.Zero),
            IconPath = "custom-icon.png"
        };

        var instJson = JsonSerializer.Serialize(inst, VazJsonContext.CamelCase.MinecraftInstance);
        File.WriteAllText(Path.Combine(dir, "instance.json"), instJson);

        _instanceManagerMock
            .Setup(m => m.GetAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inst);

        return (dir, inst);
    }

    [Fact]
    public async Task ChangeVersionAsync_Success_KeepMods_PreservesModsAndCreatesBackups()
    {
        // Case 1: Success keep mods -> verify instance.json mới + mods còn + .bak tồn tại.
        var (dir, inst) = SeedInstance();
        _creationServiceMock
            .Setup(c => c.CreateAsync(
                inst.Name,
                "1.20.6",
                "fabric",
                It.IsAny<string?>(),
                It.IsAny<IProgress<InstallerProgress>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InstanceInfo
            {
                Id = inst.Id,
                Name = inst.Name,
                MinecraftVersion = "1.20.6",
                LoaderType = "fabric",
                LoaderVersion = "0.20.1",
                LaunchVersionName = "fabric-loader-0.20.1-1.20.6",
                Path = dir,
                CreatedAt = DateTimeOffset.UtcNow
            });

        var svc = new InstanceMigrationService(_instanceManagerMock.Object, _creationServiceMock.Object, NullLogger<InstanceMigrationService>.Instance);
        var result = await svc.ChangeVersionAsync(inst.Id, "1.20.6", "fabric", keepMods: true);

        result.Success.Should().BeTrue();
        result.Instance.Should().NotBeNull();
        result.Instance!.MinecraftVersion.Should().Be("1.20.6");
        result.Instance!.CreatedAt.Should().Be(inst.CreatedAt);
        result.Instance!.LastPlayedAt.Should().Be(inst.LastPlayedAt);
        result.Instance!.IconPath.Should().Be(inst.IconPath);

        // instance.json mới trên đĩa
        var diskJson = await File.ReadAllTextAsync(Path.Combine(dir, "instance.json"));
        diskJson.Should().Contain("1.20.6");

        // .bak tồn tại
        File.Exists(Path.Combine(dir, "instance.json.bak")).Should().BeTrue();
        (await File.ReadAllTextAsync(Path.Combine(dir, "instance.json.bak"))).Should().Contain("1.21.1");
        File.Exists(Path.Combine(dir, "mods.json.bak")).Should().BeTrue();

        // mods còn nguyên
        Directory.GetFiles(Path.Combine(dir, "mods")).Should().HaveCount(2);
        File.Exists(Path.Combine(dir, "mods.json")).Should().BeTrue();
    }

    [Fact]
    public async Task ChangeVersionAsync_Success_RemoveMods_DeletesModsDirectoryAndManifest()
    {
        // Case 2: Success remove mods -> verify mods/ rỗng/xóa + mods.json xóa.
        var (dir, inst) = SeedInstance();
        _creationServiceMock
            .Setup(c => c.CreateAsync(
                inst.Name,
                "1.20.6",
                "vanilla",
                null,
                It.IsAny<IProgress<InstallerProgress>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InstanceInfo
            {
                Id = inst.Id,
                Name = inst.Name,
                MinecraftVersion = "1.20.6",
                LoaderType = "vanilla",
                LoaderVersion = null,
                LaunchVersionName = "1.20.6",
                Path = dir,
                CreatedAt = DateTimeOffset.UtcNow
            });

        var svc = new InstanceMigrationService(_instanceManagerMock.Object, _creationServiceMock.Object, NullLogger<InstanceMigrationService>.Instance);
        var result = await svc.ChangeVersionAsync(inst.Id, "1.20.6", "vanilla", keepMods: false);

        result.Success.Should().BeTrue();
        result.Instance.Should().NotBeNull();
        result.Instance!.LoaderType.Should().Be("vanilla");

        // instance.json cập nhật vanilla
        var diskJson = await File.ReadAllTextAsync(Path.Combine(dir, "instance.json"));
        diskJson.Should().Contain("\"vanilla\"");

        // .bak tồn tại
        File.Exists(Path.Combine(dir, "instance.json.bak")).Should().BeTrue();

        // mods/ và mods.json đã bị xóa
        Directory.Exists(Path.Combine(dir, "mods")).Should().BeFalse();
        File.Exists(Path.Combine(dir, "mods.json")).Should().BeFalse();
    }

    [Fact]
    public async Task ChangeVersionAsync_FailDuringInstall_RestoresOriginalInstanceJsonAndPreservesMods()
    {
        // Case 3: Fail during install -> verify instance.json được restore từ .bak, mods không bị động.
        var (dir, inst) = SeedInstance();
        _creationServiceMock
            .Setup(c => c.CreateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<IProgress<InstallerProgress>?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated download/installer failure"));

        var svc = new InstanceMigrationService(_instanceManagerMock.Object, _creationServiceMock.Object, NullLogger<InstanceMigrationService>.Instance);

        var act = () => svc.ChangeVersionAsync(inst.Id, "1.20.6", "fabric", keepMods: true);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Đổi phiên bản thất bại*");

        // instance.json được restore về 1.21.1 ban đầu
        var diskJson = await File.ReadAllTextAsync(Path.Combine(dir, "instance.json"));
        diskJson.Should().Contain("1.21.1");

        // mods/ và mods.json không bị động
        Directory.GetFiles(Path.Combine(dir, "mods")).Should().HaveCount(2);
        File.Exists(Path.Combine(dir, "mods.json")).Should().BeTrue();
    }

    [Fact]
    public async Task ChangeVersionAsync_CancelMidInstall_RestoresBackupsAndRethrowsOperationCanceled()
    {
        // Case 4: Cancel mid-install -> verify rollback.
        var (dir, inst) = SeedInstance();
        using var cts = new CancellationTokenSource();

        _creationServiceMock
            .Setup(c => c.CreateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<IProgress<InstallerProgress>?>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (string n, string v, string l, string? lv, IProgress<InstallerProgress>? p, CancellationToken token) =>
            {
                // Giả lập instance.json bị ghi đè dở dang trong quá trình cài
                await File.WriteAllTextAsync(Path.Combine(dir, "instance.json"), "CORRUPTED_MID_INSTALL_DATA", CancellationToken.None);
                cts.Cancel();
                token.ThrowIfCancellationRequested();
                return null!;
            });

        var svc = new InstanceMigrationService(_instanceManagerMock.Object, _creationServiceMock.Object, NullLogger<InstanceMigrationService>.Instance);

        var act = () => svc.ChangeVersionAsync(inst.Id, "1.20.6", "fabric", keepMods: true, ct: cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();

        // instance.json đã được rollback từ .bak (không bị lưu chuỗi CORRUPTED)
        var diskJson = await File.ReadAllTextAsync(Path.Combine(dir, "instance.json"));
        diskJson.Should().Contain("1.21.1");
        diskJson.Should().NotContain("CORRUPTED");

        // mods/ vẫn còn nguyên
        Directory.GetFiles(Path.Combine(dir, "mods")).Should().HaveCount(2);
        File.Exists(Path.Combine(dir, "mods.json")).Should().BeTrue();
    }

    [Fact]
    public async Task ChangeVersionAsync_CancelBeforeStart_DoesNotCreateBackupsOrModifyFiles()
    {
        // Case 5: No backup when cancel before start -> verify không tạo .bak nếu user hủy trước khi confirm.
        var (dir, inst) = SeedInstance();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Đã hủy trước khi gọi ChangeVersionAsync

        var svc = new InstanceMigrationService(_instanceManagerMock.Object, _creationServiceMock.Object, NullLogger<InstanceMigrationService>.Instance);

        var act = () => svc.ChangeVersionAsync(inst.Id, "1.20.6", "fabric", keepMods: true, ct: cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Tuyệt đối không tạo file .bak
        File.Exists(Path.Combine(dir, "instance.json.bak")).Should().BeFalse();
        File.Exists(Path.Combine(dir, "mods.json.bak")).Should().BeFalse();

        // instance.json và mods không đổi
        var diskJson = await File.ReadAllTextAsync(Path.Combine(dir, "instance.json"));
        diskJson.Should().Contain("1.21.1");

        // CreateAsync không hề được gọi
        _creationServiceMock.Verify(c => c.CreateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<IProgress<InstallerProgress>?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
