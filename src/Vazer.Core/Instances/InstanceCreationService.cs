using System.IO;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vazer.Core;
using Vazer.Core.Abstractions;
using Vazer.Core.Json;
using Vazer.Core.Loaders;
using Vazer.Core.Versions;

namespace Vazer.Core.Instances;

/// <summary>
/// Facade "tạo instance": chọn <see cref="ILoaderInstaller"/> theo loại loader,
/// resolve loader version (nếu thiếu), ghi instance.json, trả về <see cref="InstanceInfo"/>.
/// </summary>
public sealed class InstanceCreationService : IInstanceCreationService
{
    private readonly IReadOnlyDictionary<string, ILoaderInstaller> _installers;
    private readonly IMinecraftVersionService _versionService;
    private readonly MinecraftOptions _options;
    private readonly ILogger<InstanceCreationService> _logger;

    public InstanceCreationService(
        IEnumerable<ILoaderInstaller> installers,
        IMinecraftVersionService versionService,
        IOptions<MinecraftOptions> options,
        ILogger<InstanceCreationService> logger)
    {
        _installers = installers
            .GroupBy(i => i.LoaderType, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key.ToLowerInvariant(), g => g.First(), StringComparer.OrdinalIgnoreCase);
        _versionService = versionService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<InstanceInfo> CreateAsync(
        string name,
        string mcVersion,
        string loaderType,
        string? loaderVersion,
        IProgress<InstallerProgress>? progress,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(mcVersion);

        var normalizedLoader = loaderType.Trim().ToLowerInvariant();
        if (!_installers.TryGetValue(normalizedLoader, out var installer))
        {
            throw new InvalidOperationException($"Loader '{loaderType}' không được hỗ trợ.");
        }
        if (!installer.SupportsVersion(mcVersion))
        {
            throw new InvalidOperationException($"Loader '{normalizedLoader}' không hỗ trợ Minecraft {mcVersion}.");
        }

        progress?.Report(new InstallerProgress
        {
            Stage = InstallerStage.Preparing,
            Percent = -1,
            Message = "Đang chuẩn bị..."
        });

        // Resolve loader version nếu null (vanilla bỏ qua; loader khác cần version cụ thể).
        string? resolved = loaderVersion;
        if (normalizedLoader != "vanilla" && string.IsNullOrWhiteSpace(resolved))
        {
            var versions = await _versionService.GetLoaderVersionsAsync(normalizedLoader, mcVersion, ct);
            resolved = versions
                .Where(v => v.Installable)
                .OrderByDescending(v => v.Stable)
                .ThenByDescending(v => v.Version, new LoaderVersionComparer())
                .Select(v => v.Version)
                .FirstOrDefault();

            if (resolved == null)
            {
                throw new InvalidOperationException(
                    $"Không tìm thấy loader version tương thích cho {normalizedLoader} / Minecraft {mcVersion}.");
            }
        }

        var id = LoaderHelpers.SanitizeId(name);
        var instancePath = Path.Combine(_options.RootPath, "instances", id);
        Directory.CreateDirectory(instancePath);

        await installer.InstallAsync(instancePath, mcVersion, resolved, progress, ct);

        progress?.Report(new InstallerProgress
        {
            Stage = InstallerStage.Finalizing,
            Percent = -1,
            Message = "Đang ghi thông tin instance..."
        });

        var launchVersionName = ComputeLaunchVersionName(normalizedLoader, mcVersion, resolved, instancePath);
        var info = new InstanceInfo
        {
            Id = id,
            Name = name,
            MinecraftVersion = mcVersion,
            LoaderType = normalizedLoader,
            LoaderVersion = resolved,
            LaunchVersionName = launchVersionName,
            Path = instancePath,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var metaPath = Path.Combine(instancePath, "instance.json");
        var json = JsonSerializer.Serialize(info.ToMinecraftInstance(), VazJsonContext.CamelCase.MinecraftInstance);
        await File.WriteAllTextAsync(metaPath, json, ct);

        progress?.Report(new InstallerProgress
        {
            Stage = InstallerStage.Done,
            Percent = 100,
            Message = "Hoàn tất!"
        });
        _logger.LogInformation("Created {Loader} instance {Id} (MC {Version}, loader {LoaderVersion})", normalizedLoader, id, mcVersion, resolved);
        return info;
    }

    private static string? ComputeLaunchVersionName(string normalizedLoader, string mcVersion, string? loaderVersion, string instancePath) => normalizedLoader switch
    {
        "vanilla" => mcVersion,
        "fabric" => loaderVersion != null ? $"fabric-loader-{loaderVersion}-{mcVersion}" : null,
        "quilt" => loaderVersion != null ? $"quilt-loader-{loaderVersion}-{mcVersion}" : null,
        "neoforge" or "forge" => InstanceManager.DiscoverVersionName(instancePath),
        _ => null
    };
}
