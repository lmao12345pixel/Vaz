using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vazer.Core.Abstractions;
using Vazer.Core.Http;
using Vazer.Core.Modrinth;

namespace Vazer.Core.Mods;

public class ModInstallService : IModInstallService
{
    private readonly IInstanceManager _instanceManager;
    private readonly IModrinthClient _modrinthClient;
    private readonly IFileDownloader _fileDownloader;
    private readonly IOptions<MinecraftOptions> _options;
    private readonly ILogger<ModInstallService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ModInstallService(
        IInstanceManager instanceManager,
        IModrinthClient modrinthClient,
        IOptions<MinecraftOptions> options,
        ILogger<ModInstallService> logger,
        IFileDownloader? fileDownloader = null)
    {
        _instanceManager = instanceManager;
        _modrinthClient = modrinthClient;
        _fileDownloader = fileDownloader ?? new FileDownloader();
        _options = options;
        _logger = logger;
    }

    public async Task<InstallModResult> InstallFromModrinthAsync(
        string instanceId,
        string projectSlug,
        string? versionId = null,
        bool resolveDependencies = true,
        IProgress<InstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return await InstallFromModrinthInternalAsync(instanceId, projectSlug, versionId, resolveDependencies, progress, visited, depth: 0, ct);
    }

    private async Task<InstallModResult> InstallFromModrinthInternalAsync(
        string instanceId,
        string projectSlug,
        string? versionId,
        bool resolveDependencies,
        IProgress<InstallProgress>? progress,
        HashSet<string> visited,
        int depth,
        CancellationToken ct)
    {
        visited.Add(projectSlug);

        var instance = await _instanceManager.GetAsync(instanceId, ct)
            ?? throw new InvalidOperationException($"Instance not found: {instanceId}");

        var project = await _modrinthClient.GetProjectAsync(projectSlug, ct);
        if (!string.Equals(project.ProjectType, "mod", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Project '{projectSlug}' is not a mod (type: {project.ProjectType})");
        }

        if (!string.IsNullOrEmpty(project.Id)) visited.Add(project.Id);
        if (!string.IsNullOrEmpty(project.Slug)) visited.Add(project.Slug);

        ProjectVersion version;
        string? warning = null;

        if (versionId != null)
        {
            version = await _modrinthClient.GetVersionAsync(versionId, ct)
                ?? throw new InvalidOperationException($"Version not found: {versionId}");
            if (!string.Equals(version.ProjectId, project.Id, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Version {versionId} belongs to project {version.ProjectId}, " +
                    $"not {project.Id} ({projectSlug}). Refusing to install.");
            }
        }
        else
        {
            var versions = await _modrinthClient.GetVersionsAsync(projectSlug, instance.LoaderType, instance.MinecraftVersion, ct);
            var releaseVersions = versions.Where(v => string.Equals(v.VersionType, "release", StringComparison.OrdinalIgnoreCase)).ToList();

            if (releaseVersions.Count > 0)
            {
                version = releaseVersions.OrderByDescending(v => v.DatePublished ?? DateTimeOffset.MinValue).First();
            }
            else
            {
                var betaVersions = versions.Where(v => string.Equals(v.VersionType, "beta", StringComparison.OrdinalIgnoreCase)).ToList();
                if (betaVersions.Count > 0)
                {
                    version = betaVersions.OrderByDescending(v => v.DatePublished ?? DateTimeOffset.MinValue).First();
                    warning = $"Selected beta version '{version.VersionNumber}' for {projectSlug} as no release was found";
                    _logger.LogWarning("{Warning}", warning);
                }
                else
                {
                    throw new InvalidOperationException($"No compatible version found for {projectSlug} ({instance.LoaderType} {instance.MinecraftVersion})");
                }
            }
        }

        var manifest = await ReadManifestAsync(instance.Path, ct);

        // Check incompatible dependencies
        foreach (var dep in version.Dependencies)
        {
            if (string.Equals(dep.DependencyType, "incompatible", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(dep.ProjectId))
            {
                if (manifest.Mods.Any(m => string.Equals(m.ProjectId, dep.ProjectId, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"Incompatible mod conflict between {project.Title} and {dep.ProjectId}");
                }
            }
        }

        if (version.Files == null || version.Files.Length == 0)
        {
            throw new InvalidOperationException($"No files found in version {version.Id} for {projectSlug}");
        }

        var file = version.Files.FirstOrDefault(f => f.Primary) ?? version.Files[0];

        var modsDir = Path.Combine(instance.Path, "mods");
        Directory.CreateDirectory(modsDir);

        var existingMod = manifest.Mods.FirstOrDefault(m =>
            string.Equals(m.ProjectId, project.Id, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(project.Slug) && string.Equals(m.ProjectId, project.Slug, StringComparison.OrdinalIgnoreCase)) ||
            string.Equals(m.ProjectId, projectSlug, StringComparison.OrdinalIgnoreCase));

        bool isEnabled = existingMod?.Enabled ?? true;

        var rawName = Path.GetFileNameWithoutExtension(file.Filename);
        var ext = Path.GetExtension(file.Filename);
        if (string.IsNullOrEmpty(ext)) ext = ".jar";

        string candidate = file.Filename;
        int suffixIndex = 1;

        while (manifest.Mods.Any(m => m != existingMod &&
                   (string.Equals(m.FileName, candidate, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(m.FileName, candidate + ".disabled", StringComparison.OrdinalIgnoreCase)))
               || (File.Exists(Path.Combine(modsDir, candidate)) &&
                   (existingMod == null || !string.Equals(existingMod.FileName, candidate, StringComparison.OrdinalIgnoreCase)))
               || (File.Exists(Path.Combine(modsDir, candidate + ".disabled")) &&
                   (existingMod == null || !string.Equals(existingMod.FileName, candidate + ".disabled", StringComparison.OrdinalIgnoreCase))))
        {
            candidate = $"{rawName}-{suffixIndex}{ext}";
            suffixIndex++;
        }

        string targetFilename = isEnabled ? candidate : candidate + ".disabled";
        var partialPath = Path.Combine(modsDir, targetFilename + ".partial");
        var finalPath = Path.Combine(modsDir, targetFilename);

        await using (var downloadStream = await _modrinthClient.DownloadAsync(file.Url, ct))
        {
            await _fileDownloader.CopyStreamAsync(
                downloadStream,
                partialPath,
                file.Size > 0 ? file.Size : null,
                $"Downloading {file.Filename}",
                progress,
                ct);
        }

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
                throw new InvalidOperationException($"SHA512 mismatch for {file.Filename}");
            }
        }

        if (existingMod != null)
        {
            var oldPath = Path.Combine(modsDir, existingMod.FileName);
            if (File.Exists(oldPath) && !string.Equals(oldPath, finalPath, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(oldPath);
            }

            var oldAlt = existingMod.FileName.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase)
                ? Path.Combine(modsDir, existingMod.FileName[..^".disabled".Length])
                : Path.Combine(modsDir, existingMod.FileName + ".disabled");
            if (File.Exists(oldAlt) && !string.Equals(oldAlt, finalPath, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(oldAlt);
            }
        }

        File.Move(partialPath, finalPath, overwrite: true);

        var newMod = new InstalledMod
        {
            FileName = targetFilename,
            ProjectId = !string.IsNullOrWhiteSpace(project.Id) ? project.Id : projectSlug,
            VersionId = version.Id,
            Source = "modrinth",
            Enabled = isEnabled,
            InstalledAt = DateTimeOffset.UtcNow,
            Sha512 = file.Hashes?.Sha512
        };

        manifest.Mods.RemoveAll(m =>
            string.Equals(m.ProjectId, project.Id, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(project.Slug) && string.Equals(m.ProjectId, project.Slug, StringComparison.OrdinalIgnoreCase)) ||
            string.Equals(m.ProjectId, projectSlug, StringComparison.OrdinalIgnoreCase));

        manifest.Mods.Add(newMod);
        await WriteManifestAsync(instance.Path, manifest, ct);

        var installedDeps = new List<string>();
        if (resolveDependencies && depth < 5)
        {
            foreach (var dep in version.Dependencies)
            {
                if (string.Equals(dep.DependencyType, "required", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(dep.ProjectId))
                {
                    bool alreadyVisited = visited.Contains(dep.ProjectId);
                    bool alreadyInstalled = manifest.Mods.Any(m => string.Equals(m.ProjectId, dep.ProjectId, StringComparison.OrdinalIgnoreCase));

                    if (!alreadyVisited && !alreadyInstalled)
                    {
                        var depResult = await InstallFromModrinthInternalAsync(instanceId, dep.ProjectId, dep.VersionId, true, progress, visited, depth + 1, ct);
                        installedDeps.Add(dep.ProjectId);
                        installedDeps.AddRange(depResult.InstalledDependencies);
                        manifest = await ReadManifestAsync(instance.Path, ct);
                    }
                }
                else if (string.Equals(dep.DependencyType, "incompatible", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(dep.ProjectId))
                {
                    if (manifest.Mods.Any(m => string.Equals(m.ProjectId, dep.ProjectId, StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException($"Incompatible mod conflict between {project.Title} and {dep.ProjectId}");
                    }
                }
                else if (string.Equals(dep.DependencyType, "optional", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Skipping optional dependency {DepProjectId} for {ModTitle}", dep.ProjectId, project.Title);
                }
            }
        }

        return new InstallModResult
        {
            Success = true,
            Warning = warning,
            ModName = !string.IsNullOrEmpty(project.Title) ? project.Title : projectSlug,
            FileName = targetFilename,
            VersionId = version.Id,
            InstalledDependencies = installedDeps.Distinct().ToList()
        };
    }

    public async Task<IReadOnlyList<InstalledMod>> ListAsync(string instanceId, CancellationToken ct = default)
    {
        var instance = await _instanceManager.GetAsync(instanceId, ct)
            ?? throw new InvalidOperationException($"Instance not found: {instanceId}");

        var manifest = await ReadManifestAsync(instance.Path, ct);
        return manifest.Mods.AsReadOnly();
    }

    public async Task SetEnabledAsync(string instanceId, string fileName, bool enabled, CancellationToken ct = default)
    {
        var instance = await _instanceManager.GetAsync(instanceId, ct)
            ?? throw new InvalidOperationException($"Instance not found: {instanceId}");

        var manifest = await ReadManifestAsync(instance.Path, ct);
        var mod = manifest.Mods.FirstOrDefault(m =>
            string.Equals(m.FileName, fileName, StringComparison.OrdinalIgnoreCase) ||
            (fileName.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase) &&
             string.Equals(m.FileName, fileName[..^".disabled".Length], StringComparison.OrdinalIgnoreCase)) ||
            (!fileName.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase) &&
             string.Equals(m.FileName, fileName + ".disabled", StringComparison.OrdinalIgnoreCase)));

        if (mod == null)
        {
            throw new InvalidOperationException($"Mod '{fileName}' not found in instance '{instanceId}'");
        }

        string currentFileName = mod.FileName;
        string newFileName = currentFileName;

        if (enabled)
        {
            if (newFileName.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
            {
                newFileName = newFileName[..^".disabled".Length];
            }
        }
        else
        {
            if (!newFileName.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
            {
                newFileName += ".disabled";
            }
        }

        var modsDir = Path.Combine(instance.Path, "mods");
        var currentPath = Path.Combine(modsDir, currentFileName);
        var newPath = Path.Combine(modsDir, newFileName);

        if (!string.Equals(currentFileName, newFileName, StringComparison.OrdinalIgnoreCase) && File.Exists(currentPath))
        {
            File.Move(currentPath, newPath, overwrite: true);
        }

        var index = manifest.Mods.IndexOf(mod);
        manifest.Mods[index] = mod with { Enabled = enabled, FileName = newFileName };
        await WriteManifestAsync(instance.Path, manifest, ct);
    }

    public async Task UninstallAsync(string instanceId, string fileName, CancellationToken ct = default)
    {
        var instance = await _instanceManager.GetAsync(instanceId, ct)
            ?? throw new InvalidOperationException($"Instance not found: {instanceId}");

        var manifest = await ReadManifestAsync(instance.Path, ct);
        var mod = manifest.Mods.FirstOrDefault(m =>
            string.Equals(m.FileName, fileName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(m.FileName + ".disabled", fileName, StringComparison.OrdinalIgnoreCase) ||
            (m.FileName.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase) &&
             string.Equals(m.FileName[..^".disabled".Length], fileName, StringComparison.OrdinalIgnoreCase)));

        if (mod != null)
        {
            manifest.Mods.Remove(mod);
            await WriteManifestAsync(instance.Path, manifest, ct);
        }

        var modsDir = Path.Combine(instance.Path, "mods");
        var targetPath = Path.Combine(modsDir, fileName);
        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }

        var altPath = fileName.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(modsDir, fileName[..^".disabled".Length])
            : Path.Combine(modsDir, fileName + ".disabled");
        if (File.Exists(altPath))
        {
            File.Delete(altPath);
        }

        if (mod != null && !string.IsNullOrEmpty(mod.FileName))
        {
            var modFilePath = Path.Combine(modsDir, mod.FileName);
            if (File.Exists(modFilePath))
            {
                File.Delete(modFilePath);
            }
        }
    }

    public async Task<UpdateCheckResult> CheckUpdatesAsync(string instanceId, CancellationToken ct = default)
    {
        var instance = await _instanceManager.GetAsync(instanceId, ct)
            ?? throw new InvalidOperationException($"Instance not found: {instanceId}");

        var manifest = await ReadManifestAsync(instance.Path, ct);
        var updates = new List<ModUpdateInfo>();

        foreach (var mod in manifest.Mods)
        {
            if (!string.Equals(mod.Source, "modrinth", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var versions = await _modrinthClient.GetVersionsAsync(mod.ProjectId, instance.LoaderType, instance.MinecraftVersion, ct);
            var latestRelease = versions
                .Where(v => string.Equals(v.VersionType, "release", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(v => v.DatePublished ?? DateTimeOffset.MinValue)
                .FirstOrDefault();

            if (latestRelease != null)
            {
                bool hasUpdate = !string.Equals(latestRelease.Id, mod.VersionId, StringComparison.OrdinalIgnoreCase);
                updates.Add(new ModUpdateInfo
                {
                    CurrentMod = mod,
                    LatestVersionId = latestRelease.Id,
                    LatestVersionNumber = latestRelease.VersionNumber,
                    HasUpdate = hasUpdate
                });
            }
            else
            {
                updates.Add(new ModUpdateInfo
                {
                    CurrentMod = mod,
                    LatestVersionId = mod.VersionId,
                    LatestVersionNumber = "",
                    HasUpdate = false
                });
            }
        }

        return new UpdateCheckResult { Updates = updates };
    }

    public async Task<int> UpdateAllAsync(string instanceId, IProgress<InstallProgress>? progress = null, CancellationToken ct = default)
    {
        var checkResult = await CheckUpdatesAsync(instanceId, ct);
        int successCount = 0;
        var updatesToApply = checkResult.Updates.Where(u => u.HasUpdate).ToList();

        for (int i = 0; i < updatesToApply.Count; i++)
        {
            var update = updatesToApply[i];
            try
            {
                await InstallFromModrinthAsync(
                    instanceId,
                    update.CurrentMod.ProjectId,
                    update.LatestVersionId,
                    resolveDependencies: true,
                    progress: progress,
                    ct: ct);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update mod {ProjectId} to version {VersionId}", update.CurrentMod.ProjectId, update.LatestVersionId);
            }
        }

        return successCount;
    }

    public async Task<InstallModResult> InstallPerformancePackAsync(
        string instanceId,
        IProgress<InstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        var instance = await _instanceManager.GetAsync(instanceId, ct)
            ?? throw new InvalidOperationException($"Instance not found: {instanceId}");

        if (!string.Equals(instance.LoaderType, "fabric", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(instance.LoaderType, "neoforge", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException("Performance pack requires Fabric or NeoForge.");
        }

        IProgress<InstallProgress>? sodiumProgress = progress == null ? null : new Progress<InstallProgress>(p =>
        {
            long downloaded = p.TotalBytes > 0 ? (long)(p.DownloadedBytes * 50.0 / p.TotalBytes) : 0;
            progress.Report(new InstallProgress
            {
                TotalBytes = 100,
                DownloadedBytes = downloaded,
                Status = p.Status ?? "Installing Sodium"
            });
        });

        IProgress<InstallProgress>? irisProgress = progress == null ? null : new Progress<InstallProgress>(p =>
        {
            long downloaded = p.TotalBytes > 0 ? (long)(p.DownloadedBytes * 50.0 / p.TotalBytes) : 0;
            progress.Report(new InstallProgress
            {
                TotalBytes = 100,
                DownloadedBytes = 50 + downloaded,
                Status = p.Status ?? "Installing Iris"
            });
        });

        var sodiumResult = await InstallFromModrinthAsync(instanceId, "sodium", null, true, sodiumProgress, ct);

        InstallModResult irisResult;
        try
        {
            irisResult = await InstallFromModrinthAsync(instanceId, "iris", null, true, irisProgress, ct);
        }
        catch (Exception irisEx)
        {
            return new InstallModResult
            {
                Success = true,
                Warning = $"Iris installation failed: {irisEx.Message}. Sodium was installed successfully.",
                ModName = "Performance Pack",
                FileName = sodiumResult.FileName,
                VersionId = sodiumResult.VersionId,
                InstalledDependencies = sodiumResult.InstalledDependencies
            };
        }

        var combinedDeps = new List<string>(sodiumResult.InstalledDependencies);
        combinedDeps.AddRange(irisResult.InstalledDependencies);

        return new InstallModResult
        {
            Success = true,
            ModName = "Performance Pack",
            FileName = irisResult.FileName,
            VersionId = irisResult.VersionId,
            InstalledDependencies = combinedDeps.Distinct().ToList()
        };
    }

    private async Task<ModsManifest> ReadManifestAsync(string instancePath, CancellationToken ct)
    {
        var manifestPath = Path.Combine(instancePath, "mods.json");
        if (!File.Exists(manifestPath))
        {
            return new ModsManifest();
        }

        try
        {
            var json = await File.ReadAllTextAsync(manifestPath, ct);
            return JsonSerializer.Deserialize<ModsManifest>(json, JsonOptions) ?? new ModsManifest();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read mods manifest at {Path}, returning empty manifest", manifestPath);
            return new ModsManifest();
        }
    }

    private async Task WriteManifestAsync(string instancePath, ModsManifest manifest, CancellationToken ct)
    {
        Directory.CreateDirectory(instancePath);
        var manifestPath = Path.Combine(instancePath, "mods.json");
        var tmpPath = Path.Combine(instancePath, "mods.json.tmp");
        var json = JsonSerializer.Serialize(manifest, JsonOptions);
        await File.WriteAllTextAsync(tmpPath, json, ct);
        File.Move(tmpPath, manifestPath, overwrite: true);
    }
}
