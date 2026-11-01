using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vaz.WinUI.Models;
using Vazer.Core.Abstractions;
using Vazer.Core.Http;
using Vazer.Core.Modrinth;
using Vazer.Core.Mods;

namespace Vaz.WinUI.Services;

/// <summary>
/// Triển khai IModDownloadManager: quản lý trạng thái tải (Progress, Cancel),
/// kiểm tra file thực tế trên đĩa và giao tiếp với ModInstallService / FileDownloader.
/// </summary>
public sealed class ModDownloadManager : IModDownloadManager
{
    private readonly IModInstallService _modInstallService;
    private readonly IInstanceManager _instanceManager;
    private readonly IModrinthClient _modrinthClient;
    private readonly IFileDownloader _fileDownloader;
    private readonly ConcurrentDictionary<string, bool> _activeInstalls = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource? _globalInstallCts;

    public ModDownloadManager(
        IModInstallService modInstallService,
        IInstanceManager instanceManager,
        IModrinthClient modrinthClient,
        IFileDownloader? fileDownloader = null)
    {
        _modInstallService = modInstallService ?? throw new ArgumentNullException(nameof(modInstallService));
        _instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
        _modrinthClient = modrinthClient ?? throw new ArgumentNullException(nameof(modrinthClient));
        _fileDownloader = fileDownloader ?? new FileDownloader();
    }

    public async Task<ModInstallState> CheckInstallStateAsync(string instanceId, string projectId, string slug, string projectType = "mod", CancellationToken ct = default)
    {
        if (_activeInstalls.ContainsKey(slug) || (!string.IsNullOrEmpty(projectId) && _activeInstalls.ContainsKey(projectId)))
        {
            return ModInstallState.Installing;
        }

        var isInstalled = await IsModInstalledOnDiskAsync(instanceId, projectId, slug, projectType, ct);
        return isInstalled ? ModInstallState.Installed : ModInstallState.Idle;
    }

    public async Task<bool> IsModInstalledOnDiskAsync(string instanceId, string projectId, string slug, string projectType = "mod", CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            return false;
        }

        var instance = await _instanceManager.GetAsync(instanceId, ct);
        if (instance == null || string.IsNullOrWhiteSpace(instance.Path))
        {
            return false;
        }

        if (string.Equals(projectType, "resourcepack", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(projectType, "shader", StringComparison.OrdinalIgnoreCase))
        {
            var targetFolder = string.Equals(projectType, "shader", StringComparison.OrdinalIgnoreCase)
                ? Path.Combine(instance.Path, "shaderpacks")
                : Path.Combine(instance.Path, "resourcepacks");

            if (!Directory.Exists(targetFolder))
            {
                return false;
            }

            try
            {
                var slugLower = slug.Trim().ToLowerInvariant();
                var projIdLower = projectId?.Trim().ToLowerInvariant();
                var files = Directory.GetFiles(targetFolder, "*.zip");
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file).ToLowerInvariant();
                    if (fileName.Contains(slugLower) || (!string.IsNullOrEmpty(projIdLower) && fileName.Contains(projIdLower)))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Bỏ qua lỗi IO khi quét thư mục
            }
            return false;
        }

        var modsDir = Path.Combine(instance.Path, "mods");
        if (!Directory.Exists(modsDir))
        {
            return false;
        }

        try
        {
            // 1. Kiểm tra qua manifest của ModInstallService
            var installedMods = await _modInstallService.ListAsync(instanceId, ct);
            foreach (var m in installedMods)
            {
                if ((!string.IsNullOrEmpty(projectId) && string.Equals(m.ProjectId, projectId, StringComparison.OrdinalIgnoreCase)) ||
                    string.Equals(m.ProjectId, slug, StringComparison.OrdinalIgnoreCase))
                {
                    var filePath = Path.Combine(modsDir, m.FileName);
                    if (File.Exists(filePath) || File.Exists(filePath + ".disabled"))
                    {
                        return true;
                    }
                }
            }

            // 2. Kiểm tra trực tiếp các file jar trong thư mục mods/
            var slugLower = slug.Trim().ToLowerInvariant();
            var files = Directory.GetFiles(modsDir, "*.jar*");
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file).ToLowerInvariant();
                if (fileName.Contains(slugLower))
                {
                    return true;
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi IO hoặc parse manifest khi quét
        }

        return false;
    }

    public async Task<InstallModResult> InstallModAsync(
        string instanceId,
        string projectSlug,
        bool includeDependencies = true,
        IProgress<InstallProgress>? progress = null,
        string projectType = "mod",
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new InvalidOperationException("Chưa chọn instance để cài đặt.");
        }

        if (string.IsNullOrWhiteSpace(projectSlug))
        {
            throw new ArgumentException("Project slug không được để trống.", nameof(projectSlug));
        }

        _activeInstalls[projectSlug] = true;
        _globalInstallCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            if (string.Equals(projectType, "resourcepack", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(projectType, "shader", StringComparison.OrdinalIgnoreCase))
            {
                return await InstallPackOrShaderAsync(instanceId, projectSlug, projectType, progress, _globalInstallCts.Token);
            }

            var result = await _modInstallService.InstallFromModrinthAsync(
                instanceId,
                projectSlug,
                versionId: null,
                resolveDependencies: includeDependencies,
                progress: progress,
                ct: _globalInstallCts.Token);

            return result;
        }
        finally
        {
            _activeInstalls.TryRemove(projectSlug, out _);
        }
    }

    private async Task<InstallModResult> InstallPackOrShaderAsync(
        string instanceId,
        string projectSlug,
        string projectType,
        IProgress<InstallProgress>? progress,
        CancellationToken ct)
    {
        var instance = await _instanceManager.GetAsync(instanceId, ct)
            ?? throw new InvalidOperationException($"Không tìm thấy instance {instanceId}");

        var targetDir = string.Equals(projectType, "shader", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(instance.Path, "shaderpacks")
            : Path.Combine(instance.Path, "resourcepacks");

        Directory.CreateDirectory(targetDir);

        progress?.Report(new InstallProgress { Status = "Đang kiểm tra phiên bản trên Modrinth..." });

        var versions = await _modrinthClient.GetVersionsAsync(projectSlug, loader: null, gameVersion: instance.MinecraftVersion, ct: ct);
        if (versions == null || versions.Count == 0)
        {
            // Fallback không kèm gameVersion nếu phiên bản hiện tại chưa được tag cụ thể
            versions = await _modrinthClient.GetVersionsAsync(projectSlug, loader: null, gameVersion: null, ct: ct);
        }

        if (versions == null || versions.Count == 0)
        {
            throw new InvalidOperationException($"Không tìm thấy phiên bản tương thích cho {projectSlug}");
        }

        var version = versions.FirstOrDefault(v => string.Equals(v.VersionType, "release", StringComparison.OrdinalIgnoreCase)) ?? versions[0];
        if (version.Files == null || version.Files.Length == 0)
        {
            throw new InvalidOperationException($"Phiên bản {version.Id} không có file tải về.");
        }

        var file = version.Files.FirstOrDefault(f => f.Primary) ?? version.Files[0];

        // Sanitize file name
        var safeFileName = Path.GetFileName(file.Filename);
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            safeFileName = safeFileName.Replace(c, '_');
        }

        var destPath = Path.Combine(targetDir, safeFileName);
        var partialPath = destPath + ".partial";

        progress?.Report(new InstallProgress
        {
            Status = $"Đang tải {safeFileName}...",
            DownloadedBytes = 0,
            TotalBytes = file.Size
        });

        var options = new FileDownloadOptions
        {
            HttpClientName = "modrinth-download",
            KnownTotalBytes = file.Size,
            StatusMessage = $"Đang tải {safeFileName}"
        };

        await _fileDownloader.DownloadFileAsync(file.Url, partialPath, options, progress, ct);

        if (File.Exists(destPath))
        {
            File.Delete(destPath);
        }
        File.Move(partialPath, destPath);

        progress?.Report(new InstallProgress
        {
            Status = $"Đã hoàn tất cài đặt {safeFileName}",
            DownloadedBytes = file.Size,
            TotalBytes = file.Size
        });

        return new InstallModResult
        {
            Success = true,
            ModName = projectSlug,
            FileName = safeFileName,
            VersionId = version.Id,
            InstalledDependencies = Array.Empty<string>(),
            Warning = null
        };
    }

    public void CancelInstall()
    {
        _globalInstallCts?.Cancel();
    }
}
