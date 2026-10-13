using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Vazer.Core.Abstractions;
using Vazer.Core.Http;
using Vazer.Core.Mods;

namespace Vazer.Core.Modpacks;

public sealed class MrpackImporter : IMrpackImporter
{
    private const string DownloadClientName = "modrinth-download";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly IInstanceManager _instanceManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFileDownloader _fileDownloader;
    private readonly ILogger<MrpackImporter> _logger;

    public MrpackImporter(
        IInstanceManager instanceManager,
        IHttpClientFactory httpClientFactory,
        ILogger<MrpackImporter> logger,
        IFileDownloader? fileDownloader = null)
    {
        _instanceManager = instanceManager;
        _httpClientFactory = httpClientFactory;
        _fileDownloader = fileDownloader ?? new FileDownloader(httpClientFactory);
        _logger = logger;
    }

    public async Task<MrpackPreview> InspectAsync(string mrpackPath, CancellationToken ct = default)
    {
        using var archive = ZipFile.OpenRead(mrpackPath);
        var index = await ReadIndexAsync(archive, ct);

        if (!index.Dependencies.TryGetValue("minecraft", out var mcVer) || string.IsNullOrWhiteSpace(mcVer))
        {
            throw new InvalidDataException("Minecraft version dependency missing.");
        }

        var (loaderType, loaderVersion) = ResolveLoader(index.Dependencies);

        return new MrpackPreview
        {
            Name = index.Name,
            Summary = index.Summary,
            MinecraftVersion = mcVer,
            LoaderType = loaderType,
            LoaderVersion = loaderVersion,
            FileCount = index.Files.Count
        };
    }

    public async Task ImportAsync(
        string mrpackPath,
        string instanceName,
        IProgress<InstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        using var archive = ZipFile.OpenRead(mrpackPath);
        var index = await ReadIndexAsync(archive, ct);

        if (!index.Dependencies.TryGetValue("minecraft", out var mcVer) || string.IsNullOrWhiteSpace(mcVer))
        {
            throw new InvalidDataException("Minecraft version dependency missing.");
        }

        if (index.Dependencies.ContainsKey("forge"))
        {
            throw new NotSupportedException("Forge modpacks are not supported. Use Fabric or NeoForge.");
        }

        string loaderType = "vanilla";
        string? loaderVer = null;
        if (index.Dependencies.TryGetValue("fabric-loader", out var fabricVer))
        {
            loaderType = "fabric";
            loaderVer = fabricVer;
        }
        else if (index.Dependencies.TryGetValue("neoforge", out var neoforgeVer))
        {
            loaderType = "neoforge";
            loaderVer = neoforgeVer;
        }

        MinecraftInstance? createdInstance = null;
        try
        {
            if (loaderType == "fabric")
            {
                createdInstance = await _instanceManager.CreateFabricAsync(instanceName, mcVer, loaderVer, ct);
            }
            else if (loaderType == "neoforge")
            {
                createdInstance = await _instanceManager.CreateNeoForgeAsync(instanceName, mcVer, loaderVer, ct);
            }
            else
            {
                createdInstance = await _instanceManager.CreateVanillaAsync(instanceName, mcVer, ct);
            }

            var filesToDownload = index.Files
                .Where(f => !(f.Env != null &&
                              string.Equals(f.Env.Server, "required", StringComparison.OrdinalIgnoreCase) &&
                              string.Equals(f.Env.Client, "unsupported", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var downloadedFiles = new List<MrpackFile>();

            // ponytail: sequential file downloads using primary mirror; upgrade to concurrent downloads and mirror failover if large packs need acceleration
            for (var i = 0; i < filesToDownload.Count; i++)
            {
                var file = filesToDownload[i];
                var targetPath = SafeResolvePath(createdInstance.Path, file.Path);

                if (file.Downloads.Count == 0)
                {
                    continue;
                }

                var downloadUrl = file.Downloads[0];
                var partialPath = targetPath + ".partial";
                var targetDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                await _fileDownloader.DownloadFileAsync(
                    downloadUrl,
                    partialPath,
                    new FileDownloadOptions { HttpClientName = DownloadClientName },
                    progress: null,
                    ct);

                if (!string.IsNullOrWhiteSpace(file.Hashes?.Sha512))
                {
                    using var sha512 = SHA512.Create();
                    byte[] hashBytes;
                    await using (var fs = File.OpenRead(partialPath))
                    {
                        hashBytes = await sha512.ComputeHashAsync(fs, ct);
                    }

                    var hashHex = Convert.ToHexString(hashBytes);
                    if (!string.Equals(hashHex, file.Hashes.Sha512.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        if (File.Exists(partialPath))
                        {
                            File.Delete(partialPath);
                        }
                        throw new InvalidOperationException($"Checksum mismatch: SHA512 mismatch for {file.Path}");
                    }
                }

                File.Move(partialPath, targetPath, overwrite: true);
                downloadedFiles.Add(file);

                progress?.Report(new InstallProgress
                {
                    TotalBytes = filesToDownload.Count,
                    DownloadedBytes = i + 1,
                    Status = $"Downloaded {Path.GetFileName(file.Path)}"
                });
            }

            // Overrides
            ExtractOverrides(archive, "overrides/", createdInstance.Path);

            // Client-overrides
            ExtractOverrides(archive, "client-overrides/", createdInstance.Path);

            // Write / update mods.json for downloaded mods
            await SyncModsManifestAsync(createdInstance.Path, downloadedFiles, ct);
        }
        catch
        {
            if (createdInstance != null)
            {
                await _instanceManager.DeleteAsync(createdInstance.Id, deleteFiles: true, CancellationToken.None);
            }
            throw;
        }
    }

    private static async Task<MrpackIndex> ReadIndexAsync(ZipArchive archive, CancellationToken ct)
    {
        var indexEntry = archive.GetEntry("modrinth.index.json")
            ?? archive.Entries.FirstOrDefault(e => string.Equals(e.FullName, "modrinth.index.json", StringComparison.OrdinalIgnoreCase));

        if (indexEntry == null)
        {
            throw new InvalidDataException("modrinth.index.json not found in pack.");
        }

        await using var stream = indexEntry.Open();
        var index = await JsonSerializer.DeserializeAsync<MrpackIndex>(stream, JsonOptions, ct)
            ?? throw new InvalidDataException("modrinth.index.json could not be deserialized.");

        if (index.FormatVersion != 1)
        {
            throw new InvalidDataException($"Unsupported format version: {index.FormatVersion}");
        }

        if (!string.Equals(index.Game, "minecraft", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Unsupported game: {index.Game}");
        }

        return index;
    }

    private static (string loaderType, string? loaderVersion) ResolveLoader(Dictionary<string, string> dependencies)
    {
        if (dependencies.TryGetValue("fabric-loader", out var fabricVer))
        {
            return ("fabric", fabricVer);
        }
        if (dependencies.TryGetValue("neoforge", out var neoforgeVer))
        {
            return ("neoforge", neoforgeVer);
        }
        if (dependencies.TryGetValue("forge", out var forgeVer))
        {
            return ("forge", forgeVer);
        }
        if (dependencies.TryGetValue("quilt-loader", out var quiltVer))
        {
            return ("quilt", quiltVer);
        }
        return ("vanilla", null);
    }

    private static void ExtractOverrides(ZipArchive archive, string prefix, string destinationPath)
    {
        var altPrefix = prefix.Replace('/', '\\');
        foreach (var entry in archive.Entries)
        {
            string relPath;
            if (entry.FullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                relPath = entry.FullName.Substring(prefix.Length);
            }
            else if (entry.FullName.StartsWith(altPrefix, StringComparison.OrdinalIgnoreCase))
            {
                relPath = entry.FullName.Substring(altPrefix.Length);
            }
            else
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(relPath))
            {
                continue;
            }

            var targetPath = SafeResolvePath(destinationPath, relPath);

            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
            {
                Directory.CreateDirectory(targetPath);
            }
            else
            {
                var dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                entry.ExtractToFile(targetPath, overwrite: true);
            }
        }
    }

    private static async Task SyncModsManifestAsync(string instancePath, List<MrpackFile> downloadedFiles, CancellationToken ct)
    {
        var modsDir = Path.Combine(instancePath, "mods");
        var manifestPath = Path.Combine(instancePath, "mods.json");

        var manifest = new ModsManifest();
        if (File.Exists(manifestPath))
        {
            try
            {
                var existingJson = await File.ReadAllTextAsync(manifestPath, ct);
                manifest = JsonSerializer.Deserialize<ModsManifest>(existingJson, JsonOptions) ?? new ModsManifest();
            }
            catch
            {
                manifest = new ModsManifest();
            }
        }

        var downloadedByFile = downloadedFiles
            .Where(f => !string.IsNullOrEmpty(f.Path))
            .ToDictionary(f => Path.GetFileName(f.Path), f => f, StringComparer.OrdinalIgnoreCase);

        var existingMap = manifest.Mods.ToDictionary(m => m.FileName, m => m, StringComparer.OrdinalIgnoreCase);

        if (Directory.Exists(modsDir))
        {
            foreach (var filePath in Directory.GetFiles(modsDir))
            {
                var fileName = Path.GetFileName(filePath);
                if (existingMap.ContainsKey(fileName))
                {
                    continue;
                }

                string? sha512 = null;
                if (downloadedByFile.TryGetValue(fileName, out var dlFile))
                {
                    sha512 = dlFile.Hashes?.Sha512;
                }

                var mod = new InstalledMod
                {
                    FileName = fileName,
                    ProjectId = "",
                    VersionId = "",
                    Source = "mrpack",
                    Enabled = !fileName.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase),
                    InstalledAt = DateTimeOffset.UtcNow,
                    Sha512 = sha512
                };

                manifest.Mods.Add(mod);
                existingMap[fileName] = mod;
            }
        }

        if (manifest.Mods.Count > 0 || Directory.Exists(modsDir) || File.Exists(manifestPath))
        {
            Directory.CreateDirectory(instancePath);
            var json = JsonSerializer.Serialize(manifest, JsonOptions);
            await File.WriteAllTextAsync(manifestPath, json, ct);
        }
    }

    private static string SafeResolvePath(string basePath, string relativePath)
    {
        var normalizedBase = Path.GetFullPath(basePath);
        var cleanRelative = relativePath.TrimStart('/', '\\');
        var targetPath = Path.GetFullPath(Path.Combine(normalizedBase, cleanRelative));
        var baseWithSep = normalizedBase.EndsWith(Path.DirectorySeparatorChar)
            ? normalizedBase
            : normalizedBase + Path.DirectorySeparatorChar;

        if (!targetPath.StartsWith(baseWithSep, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(targetPath, normalizedBase, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Potential zip slip vulnerability detected in path: {relativePath}");
        }
        return targetPath;
    }
}
