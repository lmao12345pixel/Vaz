using FluentAssertions;
using Vazer.Core.Packs;

namespace Vazer.Core.Tests;

public sealed class PackManifestServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PackManifestService _service;

    public PackManifestServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "vaz-manifest-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _service = new PackManifestService();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch { }
    }

    [Fact]
    public async Task SaveAndLoad_SingleEntry_MatchesFields()
    {
        // Arrange
        var entry = new PackManifestEntry
        {
            FileName = "BSL_v8.2.09.zip",
            ProjectId = "m0uik42B",
            Slug = "bsl-shaders",
            Title = "BSL Shaders",
            VersionId = "v8209",
            ThumbnailRemote = "https://cdn.modrinth.com/image.png",
            ThumbnailLocal = "thumbnails/shader-bsl.jpg",
            InstalledAt = DateTimeOffset.UtcNow,
            Loader = "iris",
            Author = "Capt Tatsu"
        };

        var manifest = new PacksManifest
        {
            SchemaVersion = 1,
            PackType = "shader",
            Packs = new List<PackManifestEntry> { entry }
        };

        // Act
        await _service.SaveAsync(_tempDir, "shader", manifest);
        var loaded = await _service.LoadAsync(_tempDir, "shader");

        // Assert
        loaded.Should().NotBeNull();
        loaded.PackType.Should().Be("shader");
        loaded.SchemaVersion.Should().Be(1);
        loaded.Packs.Should().HaveCount(1);

        var loadedEntry = loaded.Packs[0];
        loadedEntry.FileName.Should().Be(entry.FileName);
        loadedEntry.ProjectId.Should().Be(entry.ProjectId);
        loadedEntry.Slug.Should().Be(entry.Slug);
        loadedEntry.Title.Should().Be(entry.Title);
        loadedEntry.VersionId.Should().Be(entry.VersionId);
        loadedEntry.ThumbnailRemote.Should().Be(entry.ThumbnailRemote);
        loadedEntry.ThumbnailLocal.Should().Be(entry.ThumbnailLocal);
        loadedEntry.Loader.Should().Be(entry.Loader);
        loadedEntry.Author.Should().Be(entry.Author);
    }

    [Fact]
    public async Task SaveAndLoad_MultipleEntries_PreservesOrderAndCount()
    {
        // Arrange
        var entries = new List<PackManifestEntry>
        {
            new() { FileName = "PackA.zip", Title = "Pack A", Slug = "pack-a" },
            new() { FileName = "PackB.zip", Title = "Pack B", Slug = "pack-b" },
            new() { FileName = "PackC.zip", Title = "Pack C", Slug = "pack-c" }
        };

        var manifest = new PacksManifest
        {
            PackType = "resourcepack",
            Packs = entries
        };

        // Act
        await _service.SaveAsync(_tempDir, "resourcepack", manifest);
        var loaded = await _service.LoadAsync(_tempDir, "resourcepack");

        // Assert
        loaded.Packs.Should().HaveCount(3);
        loaded.Packs.Select(p => p.FileName).Should().ContainInOrder("PackA.zip", "PackB.zip", "PackC.zip");
    }

    [Fact]
    public async Task AddOrUpdateEntry_UpdatesExistingEntry_WhenFileNameMatches()
    {
        // Arrange
        var initial = new PackManifestEntry
        {
            FileName = "TestShader.zip",
            Title = "Old Title",
            VersionId = "v1"
        };
        await _service.AddOrUpdateEntryAsync(_tempDir, "shader", initial);

        var updated = new PackManifestEntry
        {
            FileName = "TestShader.zip",
            Title = "New Title",
            VersionId = "v2",
            Slug = "test-shader"
        };

        // Act
        await _service.AddOrUpdateEntryAsync(_tempDir, "shader", updated);
        var loaded = await _service.LoadAsync(_tempDir, "shader");

        // Assert
        loaded.Packs.Should().HaveCount(1);
        loaded.Packs[0].Title.Should().Be("New Title");
        loaded.Packs[0].VersionId.Should().Be("v2");
        loaded.Packs[0].Slug.Should().Be("test-shader");
    }
}
