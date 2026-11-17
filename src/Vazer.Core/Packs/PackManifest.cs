using System.Text.Json;
using System.Text.Json.Serialization;
using Vazer.Core.Json;

namespace Vazer.Core.Packs;

public sealed record PackManifestEntry
{
    [JsonPropertyName("fileName")]
    public string FileName { get; init; } = "";

    [JsonPropertyName("projectId")]
    public string? ProjectId { get; init; }

    [JsonPropertyName("slug")]
    public string? Slug { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = "";

    [JsonPropertyName("versionId")]
    public string? VersionId { get; init; }

    [JsonPropertyName("thumbnailLocal")]
    public string? ThumbnailLocal { get; init; }

    [JsonPropertyName("thumbnailRemote")]
    public string? ThumbnailRemote { get; init; }

    [JsonPropertyName("installedAt")]
    public DateTimeOffset InstalledAt { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("loader")]
    public string? Loader { get; init; }

    [JsonPropertyName("author")]
    public string? Author { get; init; }
}

public sealed record PacksManifest
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = 1;

    [JsonPropertyName("packType")]
    public string PackType { get; init; } = "";

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("packs")]
    public List<PackManifestEntry> Packs { get; init; } = new();
}

public interface IPackManifestService
{
    string GetManifestPath(string instancePath, string packType);
    Task<PacksManifest> LoadAsync(string instancePath, string packType, CancellationToken ct = default);
    Task<PacksManifest> EnsureManifestWithMigrationAsync(string instancePath, string packType, CancellationToken ct = default);
    Task SaveAsync(string instancePath, string packType, PacksManifest manifest, CancellationToken ct = default);
    Task AddOrUpdateEntryAsync(string instancePath, string packType, PackManifestEntry entry, CancellationToken ct = default);
    Task RemoveEntryAsync(string instancePath, string packType, string fileName, CancellationToken ct = default);
    Task<string?> DownloadThumbnailAsync(string instancePath, string packType, string slugOrId, string? remoteUrl, CancellationToken ct = default);
}

public class PackManifestService : IPackManifestService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    /// <summary>Số lần thử lại khi file manifest bị lock thoáng qua (IO).</summary>
    private const int IoRetryCount = 3;
    private const int IoRetryDelayMs = 60;

    /// <summary>
    /// Cửa sổ thời gian để ghép thumbnail mồ côi với file zip tương ứng.
    /// Luồng cài Modrinth ghi zip rồi tải thumbnail ngay sau đó (thực tế &lt; 5s),
    /// để rộng 15 phút cho máy chậm.
    /// </summary>
    private static readonly TimeSpan OrphanThumbnailTolerance = TimeSpan.FromMinutes(15);

    public PackManifestService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public string GetManifestPath(string instancePath, string packType)
    {
        if (string.IsNullOrWhiteSpace(instancePath))
        {
            throw new ArgumentException("Instance path must not be empty.", nameof(instancePath));
        }

        var isShader = string.Equals(packType, "shader", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(packType, "shaders", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(packType, "shaderpacks", StringComparison.OrdinalIgnoreCase);

        var manifestFileName = isShader ? "shaderpacks.json" : "resourcepacks.json";
        return Path.Combine(instancePath, manifestFileName);
    }

    public async Task<PacksManifest> LoadAsync(string instancePath, string packType, CancellationToken ct = default)
    {
        var path = GetManifestPath(instancePath, packType);
        if (!File.Exists(path))
        {
            return new PacksManifest { PackType = NormalizePackType(packType) };
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var manifest = await JsonSerializer.DeserializeAsync(stream, VazJsonContext.CamelCase.PacksManifest, ct).ConfigureAwait(false);
            return manifest ?? new PacksManifest { PackType = NormalizePackType(packType) };
        }
        catch
        {
            return new PacksManifest { PackType = NormalizePackType(packType) };
        }
    }

    public async Task<PacksManifest> EnsureManifestWithMigrationAsync(string instancePath, string packType, CancellationToken ct = default)
    {
        var manifestPath = GetManifestPath(instancePath, packType);
        var normType = NormalizePackType(packType);
        var targetFolder = Path.Combine(instancePath, normType == "shader" ? "shaderpacks" : "resourcepacks");

        if (File.Exists(manifestPath))
        {
            // P0.5b: đọc có retry — một lần lock thoáng qua (File.Move của tiến trình/tab
            // khác) KHÔNG được coi là "manifest hỏng", nếu không toàn bộ slug/thumbnail
            // của instance sẽ bị viết đè bằng entry rỗng.
            var (existing, isCorrupt) = await ReadManifestWithRetryAsync(manifestPath, normType, ct).ConfigureAwait(false);

            if (isCorrupt)
            {
                // Giữ lại bản hỏng làm bằng chứng thay vì để SaveAsync đè mất.
                BackupCorruptManifest(manifestPath);
            }

            if (Directory.Exists(targetFolder))
            {
                var files = Directory.GetFiles(targetFolder, "*.zip");
                var changed = isCorrupt;
                foreach (var file in files)
                {
                    var fn = Path.GetFileName(file);
                    if (!existing.Packs.Any(p => string.Equals(p.FileName, fn, StringComparison.OrdinalIgnoreCase)))
                    {
                        var fi = new FileInfo(file);
                        var (recoveredSlug, recoveredThumb) =
                            TryRecoverFromOrphanThumbnail(instancePath, normType, fi, existing.Packs);
                        existing.Packs.Add(new PackManifestEntry
                        {
                            FileName = fn,
                            Title = Path.GetFileNameWithoutExtension(fn),
                            Slug = recoveredSlug,
                            ThumbnailLocal = recoveredThumb,
                            InstalledAt = fi.LastWriteTimeUtc
                        });
                        changed = true;
                    }
                }
                if (changed)
                {
                    await SaveAsync(instancePath, packType, existing, ct).ConfigureAwait(false);
                }
            }
            else if (isCorrupt)
            {
                await SaveAsync(instancePath, packType, existing, ct).ConfigureAwait(false);
            }
            return existing;
        }

        // Migration khi chưa có file manifest
        var manifest = new PacksManifest
        {
            SchemaVersion = 1,
            PackType = normType,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        if (Directory.Exists(targetFolder))
        {
            var files = Directory.GetFiles(targetFolder, "*.zip");
            foreach (var file in files)
            {
                var fn = Path.GetFileName(file);
                var fi = new FileInfo(file);
                var (recoveredSlug, recoveredThumb) =
                    TryRecoverFromOrphanThumbnail(instancePath, normType, fi, manifest.Packs);
                manifest.Packs.Add(new PackManifestEntry
                {
                    FileName = fn,
                    Title = Path.GetFileNameWithoutExtension(fn),
                    ProjectId = null,
                    Slug = recoveredSlug,
                    ThumbnailRemote = null,
                    ThumbnailLocal = recoveredThumb,
                    InstalledAt = fi.LastWriteTimeUtc
                });
            }
        }

        await SaveAsync(instancePath, packType, manifest, ct).ConfigureAwait(false);
        return manifest;
    }

    public async Task SaveAsync(string instancePath, string packType, PacksManifest manifest, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var path = GetManifestPath(instancePath, packType);
        Directory.CreateDirectory(instancePath);

        var updatedManifest = manifest with
        {
            PackType = NormalizePackType(packType),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var tempPath = path + ".tmp";
        try
        {
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, updatedManifest, VazJsonContext.CamelCase.PacksManifest, ct).ConfigureAwait(false);
            }

            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
            throw;
        }
    }

    public async Task AddOrUpdateEntryAsync(string instancePath, string packType, PackManifestEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (string.IsNullOrWhiteSpace(entry.FileName))
        {
            throw new ArgumentException("Entry FileName must not be empty.", nameof(entry));
        }

        var manifest = await LoadAsync(instancePath, packType, ct).ConfigureAwait(false);
        var existingIndex = manifest.Packs.FindIndex(p =>
            string.Equals(p.FileName, entry.FileName, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(entry.ProjectId) && string.Equals(p.ProjectId, entry.ProjectId, StringComparison.OrdinalIgnoreCase)));

        if (existingIndex >= 0)
        {
            manifest.Packs[existingIndex] = entry;
        }
        else
        {
            manifest.Packs.Add(entry);
        }

        await SaveAsync(instancePath, packType, manifest, ct).ConfigureAwait(false);
    }

    public async Task RemoveEntryAsync(string instancePath, string packType, string fileName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return;

        var manifest = await LoadAsync(instancePath, packType, ct).ConfigureAwait(false);
        var removed = manifest.Packs.RemoveAll(p => string.Equals(p.FileName, fileName, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            await SaveAsync(instancePath, packType, manifest, ct).ConfigureAwait(false);
        }
    }

    public async Task<string?> DownloadThumbnailAsync(string instancePath, string packType, string slugOrId, string? remoteUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(instancePath) || string.IsNullOrWhiteSpace(remoteUrl))
        {
            return null;
        }

        var normType = NormalizePackType(packType);
        var thumbnailsDir = Path.Combine(instancePath, "thumbnails");
        Directory.CreateDirectory(thumbnailsDir);

        var baseSlug = string.IsNullOrWhiteSpace(slugOrId) ? "pack" : slugOrId;
        var safeSlug = string.Join("_", baseSlug.Split(Path.GetInvalidFileNameChars()));
        if (string.IsNullOrWhiteSpace(safeSlug))
        {
            safeSlug = "pack";
        }
        var fileName = $"{normType}-{safeSlug}.jpg";
        var localFilePath = Path.Combine(thumbnailsDir, fileName);

        try
        {
            using var response = await _httpClient.GetAsync(remoteUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var srcStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using var destStream = File.Create(localFilePath);
            await srcStream.CopyToAsync(destStream, ct).ConfigureAwait(false);

            return Path.Combine("thumbnails", fileName).Replace('\\', '/');
        }
        catch
        {
            if (File.Exists(localFilePath))
            {
                try { File.Delete(localFilePath); } catch { }
            }
            return null;
        }
    }

    private static string NormalizePackType(string packType)
    {
        var isShader = string.Equals(packType, "shader", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(packType, "shaders", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(packType, "shaderpacks", StringComparison.OrdinalIgnoreCase);

        return isShader ? "shader" : "resourcepack";
    }

    /// <summary>
    /// Đọc manifest với retry cho các lỗi IO thoáng qua (file bị lock bởi File.Move
    /// của tiến trình/tab khác). Chỉ coi là "corrupt" khi JSON thực sự không hợp lệ,
    /// tránh mất toàn bộ slug/thumbnail chỉ vì một lần lock ngắn.
    /// </summary>
    private static async Task<(PacksManifest manifest, bool isCorrupt)> ReadManifestWithRetryAsync(
        string manifestPath, string normType, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await using var stream = File.OpenRead(manifestPath);
                var result = await JsonSerializer
                    .DeserializeAsync(stream, VazJsonContext.CamelCase.PacksManifest, ct)
                    .ConfigureAwait(false);
                return (result ?? new PacksManifest { PackType = normType }, false);
            }
            catch (IOException) when (attempt < IoRetryCount)
            {
                await Task.Delay(IoRetryDelayMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // JSON hỏng hoặc lỗi không phục hồi được — trả về manifest rỗng,
                // đánh dấu corrupt để caller biết cần backup trước khi ghi đè.
                return (new PacksManifest { PackType = normType }, true);
            }
        }
    }

    /// <summary>
    /// Sao lưu manifest hỏng trước khi ghi đè, để có thể phục hồi dữ liệu
    /// (slug/thumbnail) bằng tay nếu cần. Không ném ngoại lệ — backup thất bại
    /// không được chặn luồng migration.
    /// </summary>
    private static void BackupCorruptManifest(string manifestPath)
    {
        try
        {
            var backupPath = manifestPath + $".corrupt-{DateTime.UtcNow:yyyyMMdd-HHmmss}.bak";
            File.Copy(manifestPath, backupPath, overwrite: false);
        }
        catch
        {
            // Không quan trọng — bỏ qua lỗi backup.
        }
    }

    /// <summary>
    /// Khi migration gặp file zip chưa có trong manifest, thử tìm thumbnail mồ côi
    /// (do DownloadThumbnailAsync ghi nhưng metadata manifest đã mất) để khôi phục
    /// slug và đường dẫn thumbnail. Chỉ ghép khi thời gian gần khớp và slug không
    /// trùng với entry đã có.
    /// </summary>
    private static (string? slug, string? thumbnailLocal) TryRecoverFromOrphanThumbnail(
        string instancePath, string normType, FileInfo zipFile,
        IReadOnlyList<PackManifestEntry>? existingEntries = null)
    {
        var thumbDir = Path.Combine(instancePath, "thumbnails");
        if (!Directory.Exists(thumbDir)) return (null, null);

        var zipMtime = zipFile.LastWriteTimeUtc;
        var prefix = normType + "-";

        // Thu thập các slug đã được entry khác sử dụng để tránh ghép nhầm.
        var usedSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (existingEntries != null)
        {
            foreach (var e in existingEntries)
            {
                if (!string.IsNullOrEmpty(e.ThumbnailLocal))
                {
                    var name = Path.GetFileName(e.ThumbnailLocal);
                    if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                        name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                    {
                        usedSlugs.Add(name.Substring(prefix.Length, name.Length - prefix.Length - 4));
                    }
                }
            }
        }

        string[] candidates;
        try
        {
            candidates = Directory.GetFiles(thumbDir, prefix + "*.jpg");
        }
        catch
        {
            return (null, null);
        }

        foreach (var thumbPath in candidates)
        {
            var thumbName = Path.GetFileName(thumbPath);
            var slug = thumbName.Substring(prefix.Length, thumbName.Length - prefix.Length - 4);
            if (usedSlugs.Contains(slug)) continue;

            var thumbMtime = File.GetLastWriteTimeUtc(thumbPath);
            // Thumbnail phải được ghi sau zip (trong luồng cài) hoặc gần cùng thời điểm.
            if (thumbMtime < zipMtime.AddSeconds(-5)) continue;
            if (thumbMtime > zipMtime.Add(OrphanThumbnailTolerance)) continue;

            // Trả về slug và đường dẫn tương đối chuẩn (giống DownloadThumbnailAsync).
            return (slug, Path.Combine("thumbnails", thumbName).Replace('\\', '/'));
        }

        return (null, null);
    }
}
