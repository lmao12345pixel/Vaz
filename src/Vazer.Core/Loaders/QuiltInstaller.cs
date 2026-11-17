using System.IO;
using System.Net.Http;
using System.Threading;
using CmlLib.Core;
using CmlLibQuilt = CmlLib.Core.ModLoaders.QuiltMC.QuiltInstaller;
using Microsoft.Extensions.Logging;

namespace Vazer.Core.Loaders;

/// <summary>
/// Quilt loader: cài qua CmlLib.Core QuiltInstaller vào thư mục instance
/// (instance-local game folder) — giữ nguyên hành vi hiện tại của InstanceManager.
/// </summary>
public sealed class QuiltInstaller : ILoaderInstaller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<QuiltInstaller> _logger;

    public string LoaderType => "quilt";

    public bool SupportsVersion(string mcVersion) => !string.IsNullOrWhiteSpace(mcVersion);

    public QuiltInstaller(IHttpClientFactory httpClientFactory, ILogger<QuiltInstaller> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
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
            Message = "Đang lấy danh sách Quilt loader..."
        });

        Directory.CreateDirectory(instancePath);

        var httpClient = _httpClientFactory.CreateClient();
        var installer = new CmlLibQuilt(httpClient);
        var loaders = await installer.GetLoaders(mcVersion);

        var selected = LoaderHelpers.SelectLatestStable(
            loaders
                .Where(l => !string.IsNullOrEmpty(l.Version))
                .Select(l => (l.Version!, l.Stable)),
            loaderVersion);

        var resolved = LoaderHelpers.RequireLoaderVersion(selected, mcVersion);

        progress?.Report(new InstallerProgress
        {
            Stage = InstallerStage.Installing,
            Percent = -1,
            Message = $"Đang cài Quilt {resolved} cho Minecraft {mcVersion}..."
        });

        var versionName = await installer.Install(mcVersion, resolved, new MinecraftPath(instancePath));

        progress?.Report(new InstallerProgress
        {
            Stage = InstallerStage.Done,
            Percent = 100,
            Message = $"Hoàn tất cài Quilt {resolved}"
        });
        _logger.LogInformation("Quilt {Loader} installed as {VersionName}", resolved, versionName);
    }
}
