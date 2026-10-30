using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Vazer.Core.GameOptions;
using Xunit;

namespace Vazer.Core.Tests;

public sealed class ResourcePackOptionsHelperTests
{
    [Fact]
    public void ParseResourcePacksFromLine_ParsesJsonArrayProperly()
    {
        var line = "resourcePacks:[\"vanilla\",\"mod_assets\",\"file/Faithful 32x.zip\"]";
        var result = ResourcePackOptionsHelper.ParseResourcePacksFromLine(line);

        result.Should().HaveCount(3);
        result[0].Should().Be("vanilla");
        result[1].Should().Be("mod_assets");
        result[2].Should().Be("file/Faithful 32x.zip");
    }

    [Fact]
    public void ParseResourcePacksFromLine_ParsesLegacyFormatProperly()
    {
        var line = "resourcePacks:[vanilla, file/Bare Bones.zip]";
        var result = ResourcePackOptionsHelper.ParseResourcePacksFromLine(line);

        result.Should().HaveCount(2);
        result[0].Should().Be("vanilla");
        result[1].Should().Be("file/Bare Bones.zip");
    }

    [Fact]
    public void UpdateResourcePacksLine_PreservesVanillaAndModEntries()
    {
        var initial = "fov:0.0\nresourcePacks:[\"vanilla\",\"mod_builtin\",\"file/OldPack.zip\"]\nrenderDistance:12";
        var activePacks = new List<string> { "Faithful 32x.zip" };

        var updated = ResourcePackOptionsHelper.UpdateResourcePacksLine(initial, activePacks);

        updated.Should().Contain("resourcePacks:[\"vanilla\",\"mod_builtin\",\"file/Faithful 32x.zip\"]");
        updated.Should().NotContain("OldPack.zip");
        updated.Should().Contain("renderDistance:12");
    }

    [Fact]
    public void UpdateResourcePacksLine_InvertsUiOrderForMinecraftPriority()
    {
        // UI has Pack 1 (Priority 0, Highest), Pack 2 (Priority 1, Lowest).
        // In Minecraft options.txt, Pack 1 must be placed AFTER Pack 2 so it overrides Pack 2.
        var initial = "";
        var activePacks = new List<string> { "PackOne.zip", "PackTwo.zip" };

        var updated = ResourcePackOptionsHelper.UpdateResourcePacksLine(initial, activePacks);

        // Expected in options.txt: ["vanilla", "file/PackTwo.zip", "file/PackOne.zip"]
        updated.Should().Contain("resourcePacks:[\"vanilla\",\"file/PackTwo.zip\",\"file/PackOne.zip\"]");
    }

    [Fact]
    public void SyncToOptionsFile_WritesAtomically_AndPreservesContent()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "vazer_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var optionsPath = Path.Combine(tempDir, "options.txt");

        try
        {
            File.WriteAllText(optionsPath, "graphicsMode:1\nresourcePacks:[\"vanilla\",\"custom_mod\"]\n");

            var success = ResourcePackOptionsHelper.SyncToOptionsFile(optionsPath, new[] { "Bare Bones.zip" });
            success.Should().BeTrue();

            var text = File.ReadAllText(optionsPath);
            text.Should().Contain("graphicsMode:1");
            text.Should().Contain("resourcePacks:[\"vanilla\",\"custom_mod\",\"file/Bare Bones.zip\"]");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    [Fact]
    public void SyncToOptionsFile_ReturnsFalse_WhenPathIsInvalid_WithoutCrashing()
    {
        var invalidPath = "Z:\\non_existent_drive_12345\\options.txt";
        var success = ResourcePackOptionsHelper.SyncToOptionsFile(invalidPath, new[] { "Pack.zip" });
        success.Should().BeFalse();
    }

    [Fact]
    public void ReadActivePacksFromOptionsFile_ReturnsUiOrderedPacks_ReversingMinecraftOrder()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "vazer_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var optionsPath = Path.Combine(tempDir, "options.txt");

        try
        {
            // In options.txt: PackTwo is at the end (highest priority in game)
            File.WriteAllText(optionsPath, "resourcePacks:[\"vanilla\",\"file/PackOne.zip\",\"file/PackTwo.zip\"]\n");

            var result = ResourcePackOptionsHelper.ReadActivePacksFromOptionsFile(optionsPath);
            result.Should().HaveCount(2);
            result[0].Should().Be("PackTwo.zip"); // UI Index 0 (Priority #1)
            result[1].Should().Be("PackOne.zip"); // UI Index 1 (Priority #2)
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    [Fact]
    public void EnableNewPack_PlacesPackAtTopPriority_AndUpdatesOptionsCorrectly()
    {
        // Simulate: UI currently has BareBones (#1) and StayTrue (#2).
        // User enables Faithful32x.zip -> it must become #1 in UI.
        var activePacks = new List<string> { "Bare Bones.zip", "Stay True.zip" };
        var newlyEnabled = "Faithful 32x.zip";

        // Insert at index 0 (Top priority #1)
        activePacks.Insert(0, newlyEnabled);

        var tempDir = Path.Combine(Path.GetTempPath(), "vazer_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var optionsPath = Path.Combine(tempDir, "options.txt");

        try
        {
            File.WriteAllText(optionsPath, "resourcePacks:[\"vanilla\",\"file/Stay True.zip\",\"file/Bare Bones.zip\"]\n");
            var success = ResourcePackOptionsHelper.SyncToOptionsFile(optionsPath, activePacks);
            success.Should().BeTrue();

            var text = File.ReadAllText(optionsPath);
            // In options.txt, Faithful 32x.zip must be at the very end (highest override)
            text.Should().Contain("resourcePacks:[\"vanilla\",\"file/Stay True.zip\",\"file/Bare Bones.zip\",\"file/Faithful 32x.zip\"]");

            // Re-reading from options.txt restores Faithful 32x.zip as #1
            var restored = ResourcePackOptionsHelper.ReadActivePacksFromOptionsFile(optionsPath);
            restored.First().Should().Be("Faithful 32x.zip");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }
}
