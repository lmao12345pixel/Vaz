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
}

public class PackManifestService : IPackManifestService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

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
            var manifest = await JsonSerializer.DeserializeAsync(stream, VazJsonContext.CamelCase.PacksManifest, ct);
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
            var existing = await LoadAsync(instancePath, packType, ct);
            if (Directory.Exists(targetFolder))
            {
                var files = Directory.GetFiles(targetFolder, "*.zip");
                var changed = false;
                foreach (var file in files)
                {
                    var fn = Path.GetFileName(file);
                    if (!existing.Packs.Any(p => string.Equals(p.FileName, fn, StringComparison.OrdinalIgnoreCase)))
                    {
                        var fi = new FileInfo(file);
                        existing.Packs.Add(new PackManifestEntry
                        {
                            FileName = fn,
                            Title = Path.GetFileNameWithoutExtension(fn),
                            InstalledAt = fi.LastWriteTimeUtc
                        });
                        changed = true;
                    }
                }
                if (changed)
                {
                    await SaveAsync(instancePath, packType, existing, ct);
                }
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
                manifest.Packs.Add(new PackManifestEntry
                {
                    FileName = fn,
                    Title = Path.GetFileNameWithoutExtension(fn),
                    ProjectId = null,
                    Slug = null,
                    ThumbnailRemote = null,
                    ThumbnailLocal = null,
                    InstalledAt = fi.LastWriteTimeUtc
                });
            }
        }

        await SaveAsync(instancePath, packType, manifest, ct);
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
                await JsonSerializer.SerializeAsync(stream, updatedManifest, VazJsonContext.CamelCase.PacksManifest, ct);
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

        var manifest = await LoadAsync(instancePath, packType, ct);
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

        await SaveAsync(instancePath, packType, manifest, ct);
    }

    public async Task RemoveEntryAsync(string instancePath, string packType, string fileName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return;

        var manifest = await LoadAsync(instancePath, packType, ct);
        var removed = manifest.Packs.RemoveAll(p => string.Equals(p.FileName, fileName, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            await SaveAsync(instancePath, packType, manifest, ct);
        }
    }

    private static string NormalizePackType(string packType)
    {
        var isShader = string.Equals(packType, "shader", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(packType, "shaders", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(packType, "shaderpacks", StringComparison.OrdinalIgnoreCase);

        return isShader ? "shader" : "resourcepack";
    }
}
