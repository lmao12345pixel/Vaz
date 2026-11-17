using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Vazer.Core.Abstractions;
using Vazer.Core.Instances;
using Vazer.Core.Json;
using Vazer.Core.Launch;

namespace Vazer.Core.Tests;

/// <summary>
/// Kiểm tra quy tắc suy ra tên version CmlLib khi launch (lỗi
/// "Cannot find test fabric" khi bấm Chơi ở instance cũ).
/// </summary>
public class LaunchVersionResolverTests
{
    [Fact]
    public void Resolve_Vanilla_UsesMinecraftVersion()
    {
        var instance = new MinecraftInstance
        {
            Id = "van-1",
            Name = "Van 1",
            MinecraftVersion = "1.20.1",
            LoaderType = "vanilla",
            Path = "C:/tmp/van-1",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        LaunchVersionResolver.Resolve(instance).Should().Be("1.20.1");
    }

    [Fact]
    public void Resolve_FabricOldInstance_ComputesLoaderVersionName()
    {
        // instance cũ (snapshot 10/2026) không có LaunchVersionName:
        // loader "fabric" + LoaderVersion "0.19.5" + MC "1.21.1"
        var instance = new MinecraftInstance
        {
            Id = "test fabric",
            Name = "Test Fabric",
            MinecraftVersion = "1.21.1",
            LoaderType = "fabric",
            LoaderVersion = "0.19.5",
            Path = "C:/tmp/test fabric",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        LaunchVersionResolver.Resolve(instance).Should().Be("fabric-loader-0.19.5-1.21.1");
    }

    [Fact]
    public void Resolve_ExplicitLaunchVersionName_Wins()
    {
        var instance = new MinecraftInstance
        {
            Id = "nf-1",
            Name = "NF 1",
            MinecraftVersion = "1.21.1",
            LoaderType = "neoforge",
            LoaderVersion = "21.1.77",
            LaunchVersionName = "neoforge-21.1.77-custom-name",
            Path = "C:/tmp/nf-1",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        LaunchVersionResolver.Resolve(instance).Should().Be("neoforge-21.1.77-custom-name");
    }

    [Fact]
    public void Resolve_UnknownLoaderWithoutVersion_ReturnsNull()
    {
        var instance = new MinecraftInstance
        {
            Id = "nf-2",
            Name = "NF 2",
            MinecraftVersion = "1.21.1",
            LoaderType = "neoforge",
            LoaderVersion = null,
            Path = "C:/tmp/nf-2",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        LaunchVersionResolver.Resolve(instance).Should().BeNull();
    }

    [Fact]
    public void InstanceJson_RoundTrip_PreservesLaunchVersionName()
    {
        var instance = new MinecraftInstance
        {
            Id = "fx-1",
            Name = "FX",
            MinecraftVersion = "1.21.1",
            LoaderType = "fabric",
            LoaderVersion = "0.19.5",
            LaunchVersionName = "fabric-loader-0.19.5-1.21.1",
            Path = "C:/tmp/fx-1",
            CreatedAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            LastPlayedAt = null,
        };

        var json = JsonSerializer.Serialize(instance, VazJsonContext.CamelCase.MinecraftInstance);
        var back = JsonSerializer.Deserialize(json, VazJsonContext.CamelCase.MinecraftInstance);

        back.Should().NotBeNull();
        back!.LaunchVersionName.Should().Be("fabric-loader-0.19.5-1.21.1");
        back.LoaderVersion.Should().Be("0.19.5");
    }

    [Fact]
    public void InstanceJson_OldFileWithoutLaunchVersionName_StillDeserializes()
    {
        // JSON snapshot "test fabric" thật trước khi thêm field mới
        var oldJson = """
{
  "id": "test fabric",
  "name": "Test Fabric",
  "minecraftVersion": "1.21.1",
  "loaderType": "fabric",
  "loaderVersion": "0.19.5",
  "path": "C:\\Users\\TOSHIBA\\AppData\\Roaming\\Vazer\\Minecraft\\instances\\test fabric",
  "iconPath": null,
  "createdAt": "2026-10-18T06:22:35.8261271+00:00",
  "lastPlayedAt": null
}
""";
        var result = JsonSerializer.Deserialize(oldJson, VazJsonContext.CamelCase.MinecraftInstance);

        result.Should().NotBeNull();
        result!.Id.Should().Be("test fabric");
        result.LaunchVersionName.Should().BeNull();
    }

    [Fact]
    public void DiscoverVersionName_InstancesFolderWithVersionJson_FindsFolderName()
    {
        var root = Path.Combine(Path.GetTempPath(), "vaz-test-disc-" + Guid.NewGuid().ToString("N"));
        try
        {
            var versionDir = Path.Combine(root, "versions", "fabric-loader-0.19.5-1.21.1");
            Directory.CreateDirectory(versionDir);
            File.WriteAllText(Path.Combine(versionDir, "fabric-loader-0.19.5-1.21.1.json"), "{}");

            InstanceManager.DiscoverVersionName(root).Should().Be("fabric-loader-0.19.5-1.21.1");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void DiscoverVersionName_NoVersionsFolder_ReturnsNull()
    {
        var root = Path.Combine(Path.GetTempPath(), "vaz-test-disc-none-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);
            InstanceManager.DiscoverVersionName(root).Should().BeNull();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
