using System.Net;
using FluentAssertions;
using Vazer.Core.Packs;
using Vazer.Core.Tests.TestHelpers;

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

    [Fact]
    public async Task EnsureManifestWithMigration_CreatesManifestFromExistingZipFiles()
    {
        // Arrange
        var shaderDir = Path.Combine(_tempDir, "shaderpacks");
        Directory.CreateDirectory(shaderDir);
        File.WriteAllText(Path.Combine(shaderDir, "BSL_v8.2.09.zip"), "dummy-zip-data");
        File.WriteAllText(Path.Combine(shaderDir, "Complementary.zip"), "dummy-zip-data");

        // Act
        var manifest = await _service.EnsureManifestWithMigrationAsync(_tempDir, "shader");

        // Assert
        manifest.Should().NotBeNull();
        manifest.PackType.Should().Be("shader");
        manifest.Packs.Should().HaveCount(2);
        manifest.Packs.Select(p => p.FileName).Should().Contain("BSL_v8.2.09.zip");
        manifest.Packs.Select(p => p.FileName).Should().Contain("Complementary.zip");

        var manifestFile = Path.Combine(_tempDir, "shaderpacks.json");
        File.Exists(manifestFile).Should().BeTrue();
    }

    [Fact]
    public async Task Migration_SetsNullForProjectIdAndSlug_WhenNoManifestExisted()
    {
        // Arrange
        var rpDir = Path.Combine(_tempDir, "resourcepacks");
        Directory.CreateDirectory(rpDir);
        File.WriteAllText(Path.Combine(rpDir, "BareBones.zip"), "dummy-zip-data");

        // Act
        var manifest = await _service.EnsureManifestWithMigrationAsync(_tempDir, "resourcepack");

        // Assert
        manifest.Packs.Should().HaveCount(1);
        manifest.Packs[0].FileName.Should().Be("BareBones.zip");
        manifest.Packs[0].ProjectId.Should().BeNull();
        manifest.Packs[0].Slug.Should().BeNull();
        manifest.Packs[0].ThumbnailRemote.Should().BeNull();
        manifest.Packs[0].ThumbnailLocal.Should().BeNull();
    }

    [Fact]
    public async Task EnsureManifestWithMigration_PreservesExistingEntries_AndAppendsNewZipFiles()
    {
        // Arrange
        var shaderDir = Path.Combine(_tempDir, "shaderpacks");
        Directory.CreateDirectory(shaderDir);
        File.WriteAllText(Path.Combine(shaderDir, "Existing.zip"), "dummy");

        var initialEntry = new PackManifestEntry
        {
            FileName = "Existing.zip",
            ProjectId = "proj-123",
            Slug = "existing-slug",
            Title = "Existing Shader"
        };
        await _service.AddOrUpdateEntryAsync(_tempDir, "shader", initialEntry);

        // Add a new untracked zip to disk
        File.WriteAllText(Path.Combine(shaderDir, "NewDownloaded.zip"), "dummy");

        // Act
        var manifest = await _service.EnsureManifestWithMigrationAsync(_tempDir, "shader");

        // Assert
        manifest.Packs.Should().HaveCount(2);
        var existing = manifest.Packs.First(p => p.FileName == "Existing.zip");
        existing.ProjectId.Should().Be("proj-123");
        existing.Slug.Should().Be("existing-slug");

        var newlyFound = manifest.Packs.First(p => p.FileName == "NewDownloaded.zip");
        newlyFound.Title.Should().Be("NewDownloaded");
        newlyFound.ProjectId.Should().BeNull();
    }

    [Fact]
    public async Task RemoveEntry_RemovesSpecifiedFile_AndPersists()
    {
        // Arrange
        await _service.AddOrUpdateEntryAsync(_tempDir, "shader", new PackManifestEntry { FileName = "Shader1.zip", Title = "1" });
        await _service.AddOrUpdateEntryAsync(_tempDir, "shader", new PackManifestEntry { FileName = "Shader2.zip", Title = "2" });

        // Act
        await _service.RemoveEntryAsync(_tempDir, "shader", "Shader1.zip");
        var loaded = await _service.LoadAsync(_tempDir, "shader");

        // Assert
        loaded.Packs.Should().HaveCount(1);
        loaded.Packs[0].FileName.Should().Be("Shader2.zip");
    }

    [Fact]
    public async Task Save_ThenRestartService_PreservesInstalledPackData()
    {
        // Simulate restart with a new service instance
        var service1 = new PackManifestService();
        var entry = new PackManifestEntry
        {
            FileName = "BSL.zip",
            ProjectId = "m0uik42B",
            Slug = "bsl-shaders",
            Title = "BSL Shaders"
        };
        await service1.AddOrUpdateEntryAsync(_tempDir, "shader", entry);

        var service2 = new PackManifestService();
        var loaded = await service2.LoadAsync(_tempDir, "shader");

        loaded.Packs.Should().HaveCount(1);
        loaded.Packs[0].ProjectId.Should().Be("m0uik42B");
        loaded.Packs[0].Slug.Should().Be("bsl-shaders");
    }

    [Fact]
    public void ManifestEntry_PrefersLocalThumbnail_WhenAvailable()
    {
        var entry = new PackManifestEntry
        {
            FileName = "bsl.zip",
            ThumbnailLocal = "thumbnails/shader-bsl.jpg",
            ThumbnailRemote = "https://cdn.modrinth.com/remote.png"
        };

        var chosen = !string.IsNullOrEmpty(entry.ThumbnailLocal) ? entry.ThumbnailLocal : entry.ThumbnailRemote;
        chosen.Should().Be("thumbnails/shader-bsl.jpg");
    }

    [Fact]
    public void ManifestEntry_FallbacksToRemoteThumbnail_WhenLocalIsNull()
    {
        var entry = new PackManifestEntry
        {
            FileName = "bsl.zip",
            ThumbnailLocal = null,
            ThumbnailRemote = "https://cdn.modrinth.com/remote.png"
        };

        var chosen = !string.IsNullOrEmpty(entry.ThumbnailLocal) ? entry.ThumbnailLocal : entry.ThumbnailRemote;
        chosen.Should().Be("https://cdn.modrinth.com/remote.png");
    }

    [Fact]
    public async Task DownloadThumbnail_SavesLocalFile_WhenRemoteReturnsSuccess()
    {
        // Arrange
        var fakeHttp = new FakeHttpMessageHandler();
        fakeHttp.Map(HttpMethod.Get, "/test-image.jpg", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 })
        });

        var httpClient = new HttpClient(fakeHttp) { BaseAddress = new Uri("https://cdn.modrinth.com") };
        var serviceWithHttp = new PackManifestService(httpClient);

        // Act
        var localRelPath = await serviceWithHttp.DownloadThumbnailAsync(_tempDir, "shader", "bsl-shaders", "https://cdn.modrinth.com/test-image.jpg");

        // Assert
        localRelPath.Should().NotBeNull();
        localRelPath.Should().Be("thumbnails/shader-bsl-shaders.jpg");

        var fullPath = Path.Combine(_tempDir, localRelPath!);
        File.Exists(fullPath).Should().BeTrue();
        (await File.ReadAllBytesAsync(fullPath)).Should().HaveCount(4);
    }

    [Fact]
    public async Task DownloadThumbnail_ReturnsNull_WhenRemoteFails_WithoutThrowing()
    {
        // Arrange
        var fakeHttp = new FakeHttpMessageHandler();
        fakeHttp.Map(HttpMethod.Get, "/not-found.jpg", _ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var httpClient = new HttpClient(fakeHttp) { BaseAddress = new Uri("https://cdn.modrinth.com") };
        var serviceWithHttp = new PackManifestService(httpClient);

        // Act
        var localRelPath = await serviceWithHttp.DownloadThumbnailAsync(_tempDir, "shader", "bsl-shaders", "https://cdn.modrinth.com/not-found.jpg");

        // Assert
        localRelPath.Should().BeNull();
    }
}
