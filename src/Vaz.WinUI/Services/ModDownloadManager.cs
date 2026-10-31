using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Vaz.WinUI.Models;
using Vazer.Core.Abstractions;
using Vazer.Core.Mods;

namespace Vaz.WinUI.Services;

/// <summary>
/// Triển khai IModDownloadManager: quản lý trạng thái tải (Progress, Cancel),
/// kiểm tra file thực tế trên đĩa và giao tiếp với ModInstallService.
/// </summary>
public sealed class ModDownloadManager : IModDownloadManager
{
    private readonly IModInstallService _modInstallService;
    private readonly IInstanceManager _instanceManager;
    private readonly ConcurrentDictionary<string, bool> _activeInstalls = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource? _globalInstallCts;

    public ModDownloadManager(
        IModInstallService modInstallService,
        IInstanceManager instanceManager)
    {
        _modInstallService = modInstallService ?? throw new ArgumentNullException(nameof(modInstallService));
        _instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
    }

    public async Task<ModInstallState> CheckInstallStateAsync(string instanceId, string projectId, string slug, CancellationToken ct = default)
    {
        if (_activeInstalls.ContainsKey(slug) || (!string.IsNullOrEmpty(projectId) && _activeInstalls.ContainsKey(projectId)))
        {
            return ModInstallState.Installing;
        }

        var isInstalled = await IsModInstalledOnDiskAsync(instanceId, projectId, slug, ct);
        return isInstalled ? ModInstallState.Installed : ModInstallState.Idle;
    }

    public async Task<bool> IsModInstalledOnDiskAsync(string instanceId, string projectId, string slug, CancellationToken ct = default)
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
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new InvalidOperationException("Chưa chọn instance để cài đặt mod.");
        }

        if (string.IsNullOrWhiteSpace(projectSlug))
        {
            throw new ArgumentException("Project slug không được để trống.", nameof(projectSlug));
        }

        _activeInstalls[projectSlug] = true;
        _globalInstallCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
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

    public void CancelInstall()
    {
        _globalInstallCts?.Cancel();
    }
}
