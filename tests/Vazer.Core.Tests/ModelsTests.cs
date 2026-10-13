using FluentAssertions;
using Vazer.Core.Abstractions;
using Vazer.Core.Loaders.Optifine;
using Vazer.Core.Modpacks;
using Vazer.Core.Modrinth;
using Vazer.Core.Mods;

namespace Vazer.Core.Tests;

/// <summary>
/// Item 5: Models DTO - defaults, equality, JSON round-trip hinh dang co ban.
/// </summary>
public sealed class ModelsTests
{
    [Fact]
    public void SearchQuery_DefaultLimit_Is20()
    {
        new SearchQuery().Limit.Should().Be(20);
    }

    [Fact]
    public void SearchResult_DefaultHits_IsEmpty()
    {
        var r = new SearchResult();
        r.Hits.Should().BeEmpty();
    }

    [Fact]
    public void VersionFile_DefaultHashes_NotNull()
    {
        new VersionFile().Hashes.Should().NotBeNull();
    }

    [Fact]
    public void MrpackFile_DefaultCollections_NotNull()
    {
        var f = new MrpackFile();
        f.Downloads.Should().NotBeNull();
        f.Hashes.Should().NotBeNull();
    }

    [Fact]
    public void MrpackPreview_Defaults_AreEmpty()
    {
        var p = new MrpackPreview();
        p.Name.Should().Be("");
        p.MinecraftVersion.Should().Be("");
        p.FileCount.Should().Be(0);
    }

    [Fact]
    public void OptifineVersionNotFoundException_CarriesMcVersion()
    {
        var ex = new OptifineVersionNotFoundException("1.20.1");
        ex.McVersion.Should().Be("1.20.1");
        ex.Message.Should().Contain("1.20.1");
    }

    [Fact]
    public void InstalledMod_SerializationRoundTrip()
    {
        var mod = new InstalledMod
        {
            FileName = "sodium.jar",
            ProjectId = "proj-1",
            VersionId = "ver-9",
            Source = "modrinth",
            Enabled = true,
            InstalledAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            Sha512 = "deadbeef"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(mod);
        var back = System.Text.Json.JsonSerializer.Deserialize<InstalledMod>(json);

        back.Should().Be(mod);
    }

    [Fact]
    public void InstallModResult_DefaultDependencies_IsEmpty()
    {
        new InstallModResult().InstalledDependencies.Should().BeEmpty();
    }

    [Fact]
    public void MinecraftAccount_WithTokens_UpdatesTokens()
    {
        var account = new MinecraftAccount
        {
            Id = "id-1",
            DisplayName = "Steve",
            Kind = AccountKind.Offline,
            AccessToken = "old",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        account.IsTokenValid.Should().BeFalse();

        var fresh = account.WithTokens("new", "refresh", DateTimeOffset.UtcNow.AddHours(1));
        fresh.AccessToken.Should().Be("new");
        fresh.RefreshToken.Should().Be("refresh");
        fresh.IsTokenValid.Should().BeTrue();
    }

    [Fact]
    public void MinecraftAccount_ExpiresWithin5Minutes_CountsAsInvalid()
    {
        var account = new MinecraftAccount
        {
            Id = "id-1",
            DisplayName = "Steve",
            Kind = AccountKind.Offline,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(4)
        };

        account.IsTokenValid.Should().BeFalse();
    }

    [Fact]
    public void LaunchParameters_MinecraftVersion_DefaultsNull()
    {
        var account = new MinecraftAccount
        {
            Id = "id-1",
            DisplayName = "Steve",
            Kind = AccountKind.Offline
        };
        var java = new JavaInstallation
        {
            Path = "java.exe",
            MajorVersion = 21,
            Version = "21.0.4",
            Vendor = "Eclipse",
            Architecture = "x64",
            IsJdk = true,
            Source = "test"
        };

        var p = new LaunchParameters
        {
            InstanceId = "inst",
            InstancePath = "C:\\inst",
            Account = account,
            Java = java
        };

        p.MinecraftVersion.Should().BeNull();
        p.MaxRamMb.Should().Be(4096);
    }
}
