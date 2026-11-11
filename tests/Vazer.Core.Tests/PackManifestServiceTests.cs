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

    [Fact]
    public async Task LoadAsync_CorruptJson_ReturnsEmptyManifest_WithoutCrashing()
    {
        // Arrange
        var manifestPath = Path.Combine(_tempDir, "shaderpacks.json");
        await File.WriteAllTextAsync(manifestPath, "{ corrupt json content not valid !!! [");

        // Act
        var manifest = await _service.LoadAsync(_tempDir, "shader");

        // Assert
        manifest.Should().NotBeNull();
        manifest.PackType.Should().Be("shader");
        manifest.Packs.Should().BeEmpty();
    }

    [Fact]
    public async Task EnsureManifestWithMigration_ExistingZipWithoutManifest_CanBeVerifiedAsInstalled()
    {
        // Arrange
        var shaderDir = Path.Combine(_tempDir, "shaderpacks");
        Directory.CreateDirectory(shaderDir);
        var zipPath = Path.Combine(shaderDir, "ExistingShader_v1.zip");
        await File.WriteAllTextAsync(zipPath, "dummy-zip");

        // Act: migration chạy khi manifest chưa tồn tại
        var manifest = await _service.EnsureManifestWithMigrationAsync(_tempDir, "shader");

        // Assert: kiểm tra manifest có entry và file tồn tại trên đĩa -> check installed true
        manifest.Packs.Should().ContainSingle(p => p.FileName == "ExistingShader_v1.zip");
        var isInstalled = manifest.Packs.Any(p => p.FileName == "ExistingShader_v1.zip") && File.Exists(zipPath);
        isInstalled.Should().BeTrue();
    }

    [Fact]
    public async Task DownloadThumbnail_HandlesNullSlug_AndSanitizesInvalidChars()
    {
        // Arrange
        var fakeHttp = new FakeHttpMessageHandler();
        fakeHttp.Map(HttpMethod.Get, "/img.jpg", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[] { 0x01, 0x02 })
        });
        var httpClient = new HttpClient(fakeHttp) { BaseAddress = new Uri("https://cdn.modrinth.com") };
        var serviceWithHttp = new PackManifestService(httpClient);

        // Act 1: slug null -> fallback "pack"
        var pathNullSlug = await serviceWithHttp.DownloadThumbnailAsync(_tempDir, "shader", null!, "https://cdn.modrinth.com/img.jpg");

        // Act 2: slug có ký tự cấm: : / \ * ? " < > |
        var badSlug = "test:slug/with\\invalid*chars?in\"name<yes>pipe|";
        var pathBadSlug = await serviceWithHttp.DownloadThumbnailAsync(_tempDir, "shader", badSlug, "https://cdn.modrinth.com/img.jpg");

        // Assert
        pathNullSlug.Should().Be("thumbnails/shader-pack.jpg");
        File.Exists(Path.Combine(_tempDir, pathNullSlug!)).Should().BeTrue();

        pathBadSlug.Should().NotBeNull();
        pathBadSlug.Should().NotContainAny(":", "*", "?", "\"", "<", ">", "|");
        File.Exists(Path.Combine(_tempDir, pathBadSlug!)).Should().BeTrue();
    }

    // ===== P0.5b: chống mất metadata khi re-migration =====

    [Fact]
    public async Task ReMigration_PreservesSlugAndThumbnail_OfExistingEntries()
    {
        // Arrange: entry cài qua Modrinth (đủ metadata) + zip trên đĩa
        var rpDir = Path.Combine(_tempDir, "resourcepacks");
        Directory.CreateDirectory(rpDir);
        var zipPath = Path.Combine(rpDir, "LowOnFire.zip");
        await File.WriteAllTextAsync(zipPath, "dummy-zip");

        await _service.AddOrUpdateEntryAsync(_tempDir, "resourcepack", new PackManifestEntry
        {
            FileName = "LowOnFire.zip",
            Slug = "low-on-fire",
            Title = "LowOnFire",
            ProjectId = "abc123",
            ThumbnailLocal = "thumbnails/resourcepack-low-on-fire.jpg",
            ThumbnailRemote = "https://cdn.modrinth.com/x.jpg"
        });

        // Act: chạy lại migration nhiều lần (mô phỏng mở tab RP nhiều lần)
        var m1 = await _service.EnsureManifestWithMigrationAsync(_tempDir, "resourcepack");
        var m2 = await _service.EnsureManifestWithMigrationAsync(_tempDir, "resourcepack");

        // Assert: metadata nguyên vẹn sau re-migrate
        m1.Packs.Should().ContainSingle();
        m2.Packs.Should().ContainSingle();
        var e = m2.Packs[0];
        e.Slug.Should().Be("low-on-fire");
        e.ProjectId.Should().Be("abc123");
        e.ThumbnailLocal.Should().Be("thumbnails/resourcepack-low-on-fire.jpg");
        e.ThumbnailRemote.Should().Be("https://cdn.modrinth.com/x.jpg");
    }

    [Fact]
    public async Task ReMigration_AfterManifestLoss_RecoversSlugFromOrphanThumbnail()
    {
        // Arrange: tái hiện đúng bug LowOnFire — thumbnail mồ côi trên đĩa
        // (slug "low-on-fire" không thể suy ra từ tên file "LowOnFire v26.2.zip"),
        // manifest bị mất/hỏng nên migration tạo lại entry từ zip.
        var rpDir = Path.Combine(_tempDir, "resourcepacks");
        Directory.CreateDirectory(rpDir);
        var zipPath = Path.Combine(rpDir, "LowOnFire v26.2.zip");
        await File.WriteAllTextAsync(zipPath, "dummy-zip");

        var thumbDir = Path.Combine(_tempDir, "thumbnails");
        Directory.CreateDirectory(thumbDir);
        var thumbPath = Path.Combine(thumbDir, "resourcepack-low-on-fire.jpg");
        await File.WriteAllBytesAsync(thumbPath, new byte[] { 0xFF, 0xD8, 0xFF });

        // Thumbnail ghi ngay sau zip (đúng thứ tự luồng cài Modrinth)
        File.SetLastWriteTimeUtc(thumbPath, File.GetLastWriteTimeUtc(zipPath).AddSeconds(2));

        // Act
        var manifest = await _service.EnsureManifestWithMigrationAsync(_tempDir, "resourcepack");

        // Assert: slug + thumbnail được khôi phục từ thumbnail mồ côi
        manifest.Packs.Should().ContainSingle();
        var e = manifest.Packs[0];
        e.FileName.Should().Be("LowOnFire v26.2.zip");
        e.Slug.Should().Be("low-on-fire");
        e.ThumbnailLocal.Should().Be("thumbnails/resourcepack-low-on-fire.jpg");
    }

    [Fact]
    public async Task ReMigration_CorruptManifest_BackupsFile_AndRecoversFromThumbnail()
    {
        // Arrange: manifest hỏng hoàn toàn + thumbnail mồ côi
        var rpDir = Path.Combine(_tempDir, "resourcepacks");
        Directory.CreateDirectory(rpDir);
        var zipPath = Path.Combine(rpDir, "Solas.zip");
        await File.WriteAllTextAsync(zipPath, "dummy-zip");

        var thumbDir = Path.Combine(_tempDir, "thumbnails");
        Directory.CreateDirectory(thumbDir);
        var thumbPath = Path.Combine(thumbDir, "resourcepack-solas-shader.jpg");
        await File.WriteAllBytesAsync(thumbPath, new byte[] { 0xFF, 0xD8 });
        File.SetLastWriteTimeUtc(thumbPath, File.GetLastWriteTimeUtc(zipPath).AddSeconds(1));

        await File.WriteAllTextAsync(Path.Combine(_tempDir, "resourcepacks.json"), "{ not valid json [[");

        // Act
        var manifest = await _service.EnsureManifestWithMigrationAsync(_tempDir, "resourcepack");

        // Assert: có file backup .corrupt-*.bak và entry khôi phục được slug
        Directory.GetFiles(_tempDir, "resourcepacks.json.corrupt-*.bak").Should().HaveCount(1);
        manifest.Packs.Should().ContainSingle();
        manifest.Packs[0].Slug.Should().Be("solas-shader");
        manifest.Packs[0].ThumbnailLocal.Should().Be("thumbnails/resourcepack-solas-shader.jpg");
    }

    [Fact]
    public async Task ReMigration_DoesNotStealThumbnail_AlreadyUsedByAnotherEntry()
    {
        // Arrange: 2 zip, chỉ 1 entry có metadata trỏ tới thumbnail
        var rpDir = Path.Combine(_tempDir, "resourcepacks");
        Directory.CreateDirectory(rpDir);
        await File.WriteAllTextAsync(Path.Combine(rpDir, "PackA.zip"), "a");
        await File.WriteAllTextAsync(Path.Combine(rpDir, "PackB.zip"), "b");

        var thumbDir = Path.Combine(_tempDir, "thumbnails");
        Directory.CreateDirectory(thumbDir);
        var thumbPath = Path.Combine(thumbDir, "resourcepack-pack-a.jpg");
        await File.WriteAllBytesAsync(thumbPath, new byte[] { 0xFF });
        File.SetLastWriteTimeUtc(thumbPath, File.GetLastWriteTimeUtc(Path.Combine(rpDir, "PackA.zip")).AddSeconds(1));

        await _service.AddOrUpdateEntryAsync(_tempDir, "resourcepack", new PackManifestEntry
        {
            FileName = "PackA.zip",
            Slug = "pack-a",
            Title = "Pack A",
            ThumbnailLocal = "thumbnails/resourcepack-pack-a.jpg"
        });

        // Act: migration thêm PackB (chưa có trong manifest)
        var manifest = await _service.EnsureManifestWithMigrationAsync(_tempDir, "resourcepack");

        // Assert: PackB không được "gán nhầm" thumbnail của PackA
        manifest.Packs.Should().HaveCount(2);
        var a = manifest.Packs.First(p => p.FileName == "PackA.zip");
        var b = manifest.Packs.First(p => p.FileName == "PackB.zip");
        a.Slug.Should().Be("pack-a");
        a.ThumbnailLocal.Should().Be("thumbnails/resourcepack-pack-a.jpg");
        b.Slug.Should().BeNull();
        b.ThumbnailLocal.Should().BeNull();
    }

    [Fact]
    public async Task ReMigration_OldThumbnail_BeyondTolerance_NotAttached()
    {
        // Arrange: thumbnail cũ (30 ngày trước zip) không được ghép cho entry mới
        var rpDir = Path.Combine(_tempDir, "resourcepacks");
        Directory.CreateDirectory(rpDir);
        var zipPath = Path.Combine(rpDir, "NewPack.zip");
        await File.WriteAllTextAsync(zipPath, "dummy-zip");

        var thumbDir = Path.Combine(_tempDir, "thumbnails");
        Directory.CreateDirectory(thumbDir);
        var thumbPath = Path.Combine(thumbDir, "resourcepack-some-old-pack.jpg");
        await File.WriteAllBytesAsync(thumbPath, new byte[] { 0xFF });
        File.SetLastWriteTimeUtc(thumbPath, File.GetLastWriteTimeUtc(zipPath).AddDays(-30));

        // Act
        var manifest = await _service.EnsureManifestWithMigrationAsync(_tempDir, "resourcepack");

        // Assert
        manifest.Packs.Should().ContainSingle();
        manifest.Packs[0].Slug.Should().BeNull();
        manifest.Packs[0].ThumbnailLocal.Should().BeNull();
    }
}
