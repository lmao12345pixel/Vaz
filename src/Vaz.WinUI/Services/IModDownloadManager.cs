using System;
using System.Threading;
using System.Threading.Tasks;
using Vaz.WinUI.Models;
using Vazer.Core.Abstractions;
using Vazer.Core.Mods;

namespace Vaz.WinUI.Services;

/// <summary>
/// Quản lý trạng thái cài đặt, kiểm tra file thực tế trên đĩa và tiến trình tải mod.
/// </summary>
public interface IModDownloadManager
{
    /// <summary>
    /// Kiểm tra trạng thái cài đặt (Idle, Installing, Installed) cho một mod / resource pack / shader.
    /// </summary>
    Task<ModInstallState> CheckInstallStateAsync(string instanceId, string projectId, string slug, string projectType = "mod", CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra xem item đã tồn tại file thực tế trên đĩa (mods/, resourcepacks/, shaderpacks/) hay chưa.
    /// </summary>
    Task<bool> IsModInstalledOnDiskAsync(string instanceId, string projectId, string slug, string projectType = "mod", CancellationToken ct = default);

    /// <summary>
    /// Bắt đầu cài đặt mod / resource pack / shader từ Modrinth.
    /// </summary>
    Task<InstallModResult> InstallModAsync(
        string instanceId,
        string projectSlug,
        bool includeDependencies = true,
        IProgress<InstallProgress>? progress = null,
        string projectType = "mod",
        CancellationToken ct = default);

    /// <summary>
    /// Hủy tác vụ cài đặt đang chạy.
    /// </summary>
    void CancelInstall();
}
