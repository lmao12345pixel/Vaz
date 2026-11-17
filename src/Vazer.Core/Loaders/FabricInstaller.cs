using System.IO;
using System.Net.Http;
using System.Threading;
using CmlLib.Core;
using CmlLibFabric = CmlLib.Core.ModLoaders.FabricMC.FabricInstaller;
using Microsoft.Extensions.Logging;

namespace Vazer.Core.Loaders;

/// <summary>
/// Fabric loader: cài qua CmlLib.Core FabricInstaller vào thư mục instance
/// (instance-local game folder) — giữ nguyên hành vi hiện tại của InstanceManager.
/// </summary>
public sealed class FabricInstaller : ILoaderInstaller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FabricInstaller> _logger;

    public string LoaderType => "fabric";

    public bool SupportsVersion(string mcVersion) => !string.IsNullOrWhiteSpace(mcVersion);

    public FabricInstaller(IHttpClientFactory httpClientFactory, ILogger<FabricInstaller> logger)
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
            Message = "Đang lấy danh sách Fabric loader..."
        });

        Directory.CreateDirectory(instancePath);

        var httpClient = _httpClientFactory.CreateClient();
        var installer = new CmlLibFabric(httpClient);
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
            Message = $"Đang cài Fabric {resolved} cho Minecraft {mcVersion}..."
        });

        var versionName = await installer.Install(mcVersion, resolved, new MinecraftPath(instancePath));

        progress?.Report(new InstallerProgress
        {
            Stage = InstallerStage.Done,
            Percent = 100,
            Message = $"Hoàn tất cài Fabric {resolved}"
        });
        _logger.LogInformation("Fabric {Loader} installed as {VersionName}", resolved, versionName);
    }
}
