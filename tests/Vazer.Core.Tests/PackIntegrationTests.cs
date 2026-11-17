using System.Net;
using FluentAssertions;
using Vazer.Core.Packs;
using Vazer.Core.Tests.TestHelpers;

namespace Vazer.Core.Tests;

public sealed class PackIntegrationTests : IDisposable
{
    private readonly string _instanceDir;
    private readonly PackManifestService _service;

    public PackIntegrationTests()
    {
        _instanceDir = Path.Combine(Path.GetTempPath(), "vaz-pack-integration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_instanceDir);
        _service = new PackManifestService();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_instanceDir))
            {
                Directory.Delete(_instanceDir, recursive: true);
            }
        }
        catch { }
    }

    [Fact]
    public async Task IntegrationTest_Migration_OldInstance_NoManifest_CreatesManifestAndInstalledTrue()
    {
        // 1. Chuẩn bị folder cũ không có manifest
        var rpDir = Path.Combine(_instanceDir, "resourcepacks");
        var shaderDir = Path.Combine(_instanceDir, "shaderpacks");
        Directory.CreateDirectory(rpDir);
        Directory.CreateDirectory(shaderDir);

        var rpFiles = new[] { "Pack1.zip", "Pack2.zip", "Pack3.zip" };
        foreach (var rp in rpFiles)
        {
            await File.WriteAllTextAsync(Path.Combine(rpDir, rp), "dummy-rp-content");
        }

        var shaderFiles = new[] { "ShaderA.zip", "ShaderB.zip" };
        foreach (var sh in shaderFiles)
        {
            await File.WriteAllTextAsync(Path.Combine(shaderDir, sh), "dummy-shader-content");
        }

        // 2. Gọi Migration
        var rpManifest = await _service.EnsureManifestWithMigrationAsync(_instanceDir, "resourcepack");
        var shaderManifest = await _service.EnsureManifestWithMigrationAsync(_instanceDir, "shader");

        // 3. Assert manifest file tồn tại trên đĩa và đủ entry
        File.Exists(Path.Combine(_instanceDir, "resourcepacks.json")).Should().BeTrue();
        File.Exists(Path.Combine(_instanceDir, "shaderpacks.json")).Should().BeTrue();

        rpManifest.Packs.Should().HaveCount(3);
        shaderManifest.Packs.Should().HaveCount(2);

        // 4. Assert check installed từng file đều true
        foreach (var rp in rpFiles)
        {
            bool isInstalled = rpManifest.Packs.Any(p => string.Equals(p.FileName, rp, StringComparison.OrdinalIgnoreCase)) &&
                               File.Exists(Path.Combine(rpDir, rp));
            isInstalled.Should().BeTrue();
        }

        foreach (var sh in shaderFiles)
        {
            bool isInstalled = shaderManifest.Packs.Any(p => string.Equals(p.FileName, sh, StringComparison.OrdinalIgnoreCase)) &&
                               File.Exists(Path.Combine(shaderDir, sh));
            isInstalled.Should().BeTrue();
        }
    }

    [Fact]
    public async Task IntegrationTest_Restart_ManifestPersists_InstalledStateSurvives()
    {
        // 1. Cài 1 RP (ghi manifest)
        var entry = new PackManifestEntry
        {
            FileName = "Faithful_32x.zip",
            ProjectId = "faithful-id",
            Slug = "faithful-32x",
            Title = "Faithful 32x",
            VersionId = "v1"
        };
        await _service.AddOrUpdateEntryAsync(_instanceDir, "resourcepack", entry);

        var rpFile = Path.Combine(_instanceDir, "resourcepacks", "Faithful_32x.zip");
        Directory.CreateDirectory(Path.GetDirectoryName(rpFile)!);
        await File.WriteAllTextAsync(rpFile, "dummy");

        // 2. Simulate restart bằng new service instance
        var restartedService = new PackManifestService();
        var loaded = await restartedService.LoadAsync(_instanceDir, "resourcepack");

        // 3. Assert trạng thái installed tồn tại nguyên vẹn
        loaded.Packs.Should().ContainSingle(p => p.ProjectId == "faithful-id");
        var isInstalled = loaded.Packs.Any(p => p.ProjectId == "faithful-id") && File.Exists(rpFile);
        isInstalled.Should().BeTrue();
    }

    [Fact]
    public async Task IntegrationTest_Restart_ShaderPreview_LoadsFromLocalCache()
    {
        // 1. Tạo shader manifest entry có thumbnailLocal
        var localRelPath = "thumbnails/shader-bsl.jpg";
        var fullLocalPath = Path.Combine(_instanceDir, localRelPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullLocalPath)!);
        await File.WriteAllBytesAsync(fullLocalPath, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        var entry = new PackManifestEntry
        {
            FileName = "BSL.zip",
            ProjectId = "bsl-id",
            Slug = "bsl-shaders",
            Title = "BSL Shaders",
            ThumbnailLocal = localRelPath,
            ThumbnailRemote = "https://cdn.modrinth.com/bsl-remote.jpg"
        };
        await _service.AddOrUpdateEntryAsync(_instanceDir, "shader", entry);

        // 2. Simulate restart
        var restartedService = new PackManifestService();
        var manifest = await restartedService.LoadAsync(_instanceDir, "shader");

        // 3. Resolve ImageUri
        var loadedEntry = manifest.Packs.Single(p => p.FileName == "BSL.zip");
        var resolvedImageUri = !string.IsNullOrEmpty(loadedEntry.ThumbnailLocal)
            ? Path.Combine(_instanceDir, loadedEntry.ThumbnailLocal)
            : loadedEntry.ThumbnailRemote;

        // 4. Assert
        resolvedImageUri.Should().Be(fullLocalPath);
        File.Exists(resolvedImageUri).Should().BeTrue();
    }

    [Fact]
    public async Task IntegrationTest_Offline_ThumbnailFallback_RemoteUrlUsed()
    {
        // 1. Mock HttpClient ném exception (offline)
        var fakeHttp = new FakeHttpMessageHandler();
        fakeHttp.Map(HttpMethod.Get, "/image.png", _ => throw new HttpRequestException("Network offline"));
        var httpClient = new HttpClient(fakeHttp) { BaseAddress = new Uri("https://cdn.modrinth.com") };
        var serviceWithOfflineHttp = new PackManifestService(httpClient);

        var remoteUrl = "https://cdn.modrinth.com/image.png";

        // 2. Thử download thumbnail
        var localThumb = await serviceWithOfflineHttp.DownloadThumbnailAsync(_instanceDir, "shader", "offline-shader", remoteUrl);

        // 3. Assert download trả về null (không crash)
        localThumb.Should().BeNull();

        // 4. Lưu manifest với remote fallback
        var entry = new PackManifestEntry
        {
            FileName = "OfflineShader.zip",
            Slug = "offline-shader",
            Title = "Offline Shader",
            ThumbnailLocal = localThumb,
            ThumbnailRemote = remoteUrl
        };
        await serviceWithOfflineHttp.AddOrUpdateEntryAsync(_instanceDir, "shader", entry);

        // 5. Kiểm tra preview resolve
        var manifest = await serviceWithOfflineHttp.LoadAsync(_instanceDir, "shader");
        var item = manifest.Packs.Single(p => p.FileName == "OfflineShader.zip");

        var resolvedImage = !string.IsNullOrEmpty(item.ThumbnailLocal)
            ? Path.Combine(_instanceDir, item.ThumbnailLocal)
            : item.ThumbnailRemote;

        resolvedImage.Should().Be(remoteUrl);
    }

    [Fact]
    public async Task IntegrationTest_CorruptManifest_MigratesFromZip()
    {
        // 1. Tạo file manifest corrupt JSON
        var manifestPath = Path.Combine(_instanceDir, "shaderpacks.json");
        await File.WriteAllTextAsync(manifestPath, "INVALID_CORRUPT_JSON_DATA{{{[[");

        // 2. Tạo 2 zip trong thư mục
        var shaderDir = Path.Combine(_instanceDir, "shaderpacks");
        Directory.CreateDirectory(shaderDir);
        await File.WriteAllTextAsync(Path.Combine(shaderDir, "Shader1.zip"), "zip1");
        await File.WriteAllTextAsync(Path.Combine(shaderDir, "Shader2.zip"), "zip2");

        // 3. Gọi EnsureManifestWithMigrationAsync
        var manifest = await _service.EnsureManifestWithMigrationAsync(_instanceDir, "shader");

        // 4. Assert: không crash, manifest mới hợp lệ được ghi đè, có 2 entry từ zip
        manifest.Should().NotBeNull();
        manifest.Packs.Should().HaveCount(2);
        manifest.Packs.Select(p => p.FileName).Should().Contain("Shader1.zip");
        manifest.Packs.Select(p => p.FileName).Should().Contain("Shader2.zip");

        // 5. Assert: file manifest trên đĩa đã được sửa thành JSON chuẩn
        var rawOnDisk = await File.ReadAllTextAsync(manifestPath);
        rawOnDisk.Should().NotContain("INVALID_CORRUPT");
        rawOnDisk.Should().Contain("Shader1.zip");

        // Check installed true
        manifest.Packs.All(p => File.Exists(Path.Combine(shaderDir, p.FileName))).Should().BeTrue();
    }
}
