using System.IO;
using System.Net.Http;
using System.Threading;
using CmlLib.Core;
using CmlLib.Core.Installers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Vazer.Core.Loaders;

/// <summary>
/// Vanilla (không loader): cài Minecraft qua CmlLib vào game root dùng chung
/// ({RootPath}/instances) — giữ nguyên hành vi hiện tại của InstanceManager.
/// </summary>
public sealed class VanillaInstaller : ILoaderInstaller
{
    private readonly MinecraftLauncher _launcher;
    private readonly ILogger<VanillaInstaller> _logger;

    public string LoaderType => "vanilla";

    public bool SupportsVersion(string mcVersion) => !string.IsNullOrWhiteSpace(mcVersion);

    public VanillaInstaller(IOptions<MinecraftOptions> options, ILogger<VanillaInstaller> logger)
    {
        _logger = logger;
        var root = Path.Combine(options.Value.RootPath, "instances");
        _launcher = new MinecraftLauncher(new MinecraftPath(root));
    }

    public async Task InstallAsync(
        string instancePath,
        string mcVersion,
        string? loaderVersion,
        IProgress<InstallerProgress>? progress,
        CancellationToken ct)
    {
        progress?.Report(new InstallerProgress
        {
            Stage = InstallerStage.Preparing,
            Percent = -1,
            Message = $"Chuẩn bị cài Minecraft {mcVersion}..."
        });

        Directory.CreateDirectory(instancePath);

        var fileProgress = new Progress<InstallerProgressChangedEventArgs>(e =>
        {
            var percent = e.TotalTasks > 0 ? (double)e.ProgressedTasks / e.TotalTasks * 100.0 : -1;
            progress?.Report(new InstallerProgress
            {
                Stage = InstallerStage.Downloading,
                Percent = percent,
                Message = string.IsNullOrWhiteSpace(e.Name)
                    ? $"Đang tải Minecraft {mcVersion} ({e.ProgressedTasks}/{e.TotalTasks})..."
                    : $"Đang tải {e.Name} ({e.ProgressedTasks}/{e.TotalTasks})..."
            });
        });
        var byteProgress = new Progress<CmlLib.Core.ByteProgress>(); // no-op — tránh null

        progress?.Report(new InstallerProgress
        {
            Stage = InstallerStage.Downloading,
            Percent = -1,
            Message = $"Đang tải Minecraft {mcVersion}..."
        });

        await _launcher.InstallAsync(mcVersion, fileProgress, byteProgress, ct);

        progress?.Report(new InstallerProgress
        {
            Stage = InstallerStage.Done,
            Percent = 100,
            Message = "Hoàn tất cài đặt"
        });
        _logger.LogInformation("Installed vanilla {Version}", mcVersion);
    }
}
