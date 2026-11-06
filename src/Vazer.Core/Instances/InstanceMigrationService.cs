using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Vazer.Core.Abstractions;
using Vazer.Core.Json;
using Vazer.Core.Loaders;

namespace Vazer.Core.Instances;

/// <summary>
/// Triển khai <see cref="IInstanceMigrationService"/>:
/// Đổi phiên bản Minecraft + Mod Loader cho instance in-place.
/// Backup instance.json và mods.json trước khi cài đặt.
/// Nếu cài đặt thất bại hoặc bị hủy, tự động rollback từ bản backup.
/// </summary>
public sealed class InstanceMigrationService : IInstanceMigrationService
{
    private readonly IInstanceManager _instanceManager;
    private readonly IInstanceCreationService _creationService;
    private readonly ILogger<InstanceMigrationService> _logger;

    public InstanceMigrationService(
        IInstanceManager instanceManager,
        IInstanceCreationService creationService,
        ILogger<InstanceMigrationService>? logger = null)
    {
        _instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
        _creationService = creationService ?? throw new ArgumentNullException(nameof(creationService));
        _logger = logger ?? NullLogger<InstanceMigrationService>.Instance;
    }

    /// <inheritdoc />
    public Task<MigrationResult> ChangeVersionAsync(
        string instanceId,
        string newVersion,
        string newLoader,
        bool keepMods,
        CancellationToken ct = default)
    {
        return ChangeVersionAsync(instanceId, newVersion, newLoader, keepMods, loaderVersion: null, progress: null, ct);
    }

    /// <inheritdoc />
    public async Task<MigrationResult> ChangeVersionAsync(
        string instanceId,
        string newVersion,
        string newLoader,
        bool keepMods,
        string? loaderVersion,
        IProgress<InstallerProgress>? progress,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(newVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(newLoader);

        var instance = await _instanceManager.GetAsync(instanceId, ct)
            ?? throw new InvalidOperationException($"Instance '{instanceId}' không tồn tại.");

        ct.ThrowIfCancellationRequested();

        var instancePath = instance.Path;
        var metaPath = Path.Combine(instancePath, "instance.json");
        var metaBakPath = metaPath + ".bak";
        var modsDir = Path.Combine(instancePath, "mods");
        var modsJsonPath = Path.Combine(instancePath, "mods.json");
        var modsJsonBakPath = modsJsonPath + ".bak";

        var backupPaths = new List<string>();
        var modsBackedUp = false;

        // 1. Backup instance.json (và mods.json nếu keepMods) trước khi động vào bất kỳ file nào.
        try
        {
            if (File.Exists(metaPath))
            {
                File.Copy(metaPath, metaBakPath, overwrite: true);
                backupPaths.Add(metaBakPath);
            }
            if (keepMods && File.Exists(modsJsonPath))
            {
                File.Copy(modsJsonPath, modsJsonBakPath, overwrite: true);
                modsBackedUp = true;
                backupPaths.Add(modsJsonBakPath);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Không thể backup instance trước khi đổi phiên bản: {ex.Message}", ex);
        }

        try
        {
            // 2. Cài đặt phiên bản / loader mới qua facade tạo instance (in-place cùng tên).
            var info = await _creationService.CreateAsync(
                instance.Name,
                newVersion,
                newLoader,
                loaderVersion,
                progress,
                ct);

            // 3. Nếu thành công: cập nhật instance.json mới, giữ CreatedAt, LastPlayedAt, IconPath.
            var merged = info.ToMinecraftInstance() with
            {
                CreatedAt = instance.CreatedAt,
                LastPlayedAt = instance.LastPlayedAt,
                IconPath = instance.IconPath
            };

            var json = JsonSerializer.Serialize(merged, VazJsonContext.CamelCase.MinecraftInstance);
            await File.WriteAllTextAsync(metaPath, json, ct);

            // Xóa mods nếu !keepMods (chỉ thực hiện sau khi cài đặt thành công).
            if (!keepMods)
            {
                try
                {
                    if (Directory.Exists(modsDir))
                    {
                        Directory.Delete(modsDir, recursive: true);
                    }
                    if (File.Exists(modsJsonPath))
                    {
                        File.Delete(modsJsonPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể xóa thư mục mods sau khi đổi phiên bản instance {Id}", instanceId);
                }
            }

            return MigrationResult.Ok(merged, backupPaths);
        }
        catch (OperationCanceledException)
        {
            // Rollback khi user hủy giữa chừng
            RestoreBackups(metaPath, metaBakPath, modsBackedUp, modsJsonPath, modsJsonBakPath);
            throw;
        }
        catch (Exception ex)
        {
            // 4. Nếu fail: restore từ .bak và throw
            RestoreBackups(metaPath, metaBakPath, modsBackedUp, modsJsonPath, modsJsonBakPath);
            var fail = MigrationResult.Fail(ex.Message, backupPaths);
            throw new MigrationException($"Đổi phiên bản thất bại: {ex.Message}", fail, ex);
        }
    }

    private static void RestoreBackups(
        string metaPath,
        string metaBakPath,
        bool modsBackedUp,
        string modsJsonPath,
        string modsJsonBakPath)
    {
        try
        {
            if (File.Exists(metaBakPath))
            {
                File.Copy(metaBakPath, metaPath, overwrite: true);
            }
            if (modsBackedUp && File.Exists(modsJsonBakPath))
            {
                File.Copy(modsJsonBakPath, modsJsonPath, overwrite: true);
            }
        }
        catch
        {
            // Best effort restore
        }
    }
}
