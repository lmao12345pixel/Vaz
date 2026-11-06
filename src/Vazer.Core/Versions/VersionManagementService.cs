using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vazer.Core.Abstractions;

namespace Vazer.Core.Versions;

/// <summary>
/// Triển khai <see cref="IVersionManagementService"/>:
/// quét thư mục versions/, thống kê dung lượng và xóa các phiên bản không sử dụng.
/// </summary>
public sealed class VersionManagementService : IVersionManagementService
{
    private readonly IInstanceManager _instanceManager;
    private readonly MinecraftOptions _options;
    private readonly ILogger<VersionManagementService> _logger;

    public VersionManagementService(
        IInstanceManager instanceManager,
        IOptions<MinecraftOptions> options,
        ILogger<VersionManagementService>? logger = null)
    {
        _instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? NullLogger<VersionManagementService>.Instance;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InstalledVersionInfo>> ListInstalledVersionsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var versionsDir = GetVersionsDirectory();
        if (!Directory.Exists(versionsDir))
        {
            return Array.Empty<InstalledVersionInfo>();
        }

        IReadOnlyList<MinecraftInstance> instances;
        try
        {
            instances = await _instanceManager.ListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to list instances while scanning versions; assuming no active references");
            instances = Array.Empty<MinecraftInstance>();
        }

        var results = new List<InstalledVersionInfo>();
        var subdirs = Directory.GetDirectories(versionsDir);

        foreach (var dir in subdirs)
        {
            ct.ThrowIfCancellationRequested();
            var versionId = Path.GetFileName(dir);
            if (string.IsNullOrWhiteSpace(versionId)) continue;

            // Kiểm tra các instance đang dùng phiên bản này (theo LaunchVersionName hoặc MinecraftVersion).
            var usedBy = instances
                .Where(inst =>
                    string.Equals(inst.LaunchVersionName, versionId, StringComparison.OrdinalIgnoreCase) ||
                    (string.IsNullOrEmpty(inst.LaunchVersionName) && string.Equals(inst.MinecraftVersion, versionId, StringComparison.OrdinalIgnoreCase)) ||
                    string.Equals(inst.MinecraftVersion, versionId, StringComparison.OrdinalIgnoreCase))
                .Select(inst => inst.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var size = GetDirectorySize(dir);
            var installedAt = Directory.GetCreationTimeUtc(dir);

            // Đọc type từ file {versionId}.json nếu có
            var jsonPath = Path.Combine(dir, $"{versionId}.json");
            string? rawType = null;
            if (File.Exists(jsonPath))
            {
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
                    if (doc.RootElement.TryGetProperty("type", out var typeProp))
                    {
                        rawType = typeProp.GetString();
                    }
                }
                catch
                {
                    // Ignore JSON parsing failure
                }
            }

            var type = ResolveVersionType(versionId, rawType);

            results.Add(new InstalledVersionInfo
            {
                VersionId = versionId,
                Type = type,
                SizeBytes = size,
                InstalledAt = installedAt,
                UsedByInstances = usedBy
            });
        }

        return results
            .OrderByDescending(v => v.InstalledAt)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<DeleteResult> DeleteVersionAsync(string versionId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(versionId);

        var list = await ListInstalledVersionsAsync(ct);
        var target = list.FirstOrDefault(v => string.Equals(v.VersionId, versionId, StringComparison.OrdinalIgnoreCase));

        if (target != null && target.IsInUse)
        {
            return DeleteResult.Block(
                versionId,
                $"Phiên bản '{versionId}' đang được sử dụng bởi {target.UsedByInstances.Count} instance ({string.Join(", ", target.UsedByInstances)}).");
        }

        var versionsDir = GetVersionsDirectory();
        var dir = Path.Combine(versionsDir, versionId);

        if (!Directory.Exists(dir))
        {
            return DeleteResult.Failed(versionId, $"Thư mục phiên bản '{versionId}' không tồn tại.");
        }

        try
        {
            var freedBytes = GetDirectorySize(dir);
            Directory.Delete(dir, recursive: true);
            _logger.LogInformation("Deleted installed version {VersionId}, freed {FreedBytes} bytes", versionId, freedBytes);
            return DeleteResult.Succeeded(versionId, freedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete version directory {Dir}", dir);
            return DeleteResult.Failed(versionId, $"Không thể xóa thư mục: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Task<DiskUsageSummary> GetDiskUsageAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var versionsDir = GetVersionsDirectory();
        var librariesDir = GetLibrariesDirectory();
        var assetsDir = GetAssetsDirectory();

        var versionsBytes = Directory.Exists(versionsDir) ? GetDirectorySize(versionsDir) : 0L;
        var librariesBytes = Directory.Exists(librariesDir) ? GetDirectorySize(librariesDir) : 0L;
        var assetsBytes = Directory.Exists(assetsDir) ? GetDirectorySize(assetsDir) : 0L;
        var count = Directory.Exists(versionsDir) ? Directory.GetDirectories(versionsDir).Length : 0;

        return Task.FromResult(new DiskUsageSummary
        {
            VersionsBytes = versionsBytes,
            LibrariesBytes = librariesBytes,
            AssetsBytes = assetsBytes,
            InstalledVersionsCount = count
        });
    }

    /// <inheritdoc />
    public async Task<CleanupResult> CleanupOrphanVersionsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var list = await ListInstalledVersionsAsync(ct);
        var orphans = list.Where(v => !v.IsInUse).ToList();

        var deletedIds = new List<string>();
        var totalFreed = 0L;
        var cleanedCount = 0;

        foreach (var orphan in orphans)
        {
            ct.ThrowIfCancellationRequested();
            var del = await DeleteVersionAsync(orphan.VersionId, ct);
            if (del.Success)
            {
                cleanedCount++;
                totalFreed += del.FreedBytes;
                deletedIds.Add(orphan.VersionId);
            }
        }

        // Dọn thêm các file tạm .tmp / .partial nếu có trong thư mục versions
        var versionsDir = GetVersionsDirectory();
        if (Directory.Exists(versionsDir))
        {
            try
            {
                var tempFiles = Directory.EnumerateFiles(versionsDir, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => f.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".partial", StringComparison.OrdinalIgnoreCase));
                foreach (var f in tempFiles)
                {
                    try
                    {
                        var len = new FileInfo(f).Length;
                        File.Delete(f);
                        totalFreed += len;
                    }
                    catch
                    {
                        // Ignore temp file deletion error
                    }
                }
            }
            catch
            {
                // Ignore enumeration failure
            }
        }

        return new CleanupResult
        {
            Success = true,
            CleanedCount = cleanedCount,
            FreedBytes = totalFreed,
            DeletedVersionIds = deletedIds,
            Message = cleanedCount > 0
                ? $"Đã dọn dẹp {cleanedCount} phiên bản không sử dụng, giải phóng {InstalledVersionInfo.FormatBytes(totalFreed)}."
                : "Không có phiên bản mồ côi nào cần dọn dẹp."
        };
    }

    private string GetVersionsDirectory()
    {
        var p1 = Path.Combine(_options.RootPath, "instances", "versions");
        if (Directory.Exists(p1)) return p1;
        var p2 = Path.Combine(_options.RootPath, "versions");
        return Directory.Exists(p2) ? p2 : p1;
    }

    private string GetLibrariesDirectory()
    {
        var p1 = Path.Combine(_options.RootPath, "instances", "libraries");
        if (Directory.Exists(p1)) return p1;
        var p2 = Path.Combine(_options.RootPath, "libraries");
        return Directory.Exists(p2) ? p2 : p1;
    }

    private string GetAssetsDirectory()
    {
        var p1 = Path.Combine(_options.RootPath, "instances", "assets");
        if (Directory.Exists(p1)) return p1;
        var p2 = Path.Combine(_options.RootPath, "assets");
        return Directory.Exists(p2) ? p2 : p1;
    }

    private static long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path)) return 0L;
        try
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                .Select(f =>
                {
                    try { return new FileInfo(f).Length; }
                    catch { return 0L; }
                })
                .Sum();
        }
        catch
        {
            return 0L;
        }
    }

    private static string ResolveVersionType(string versionId, string? rawType)
    {
        if (versionId.Contains("fabric", StringComparison.OrdinalIgnoreCase)) return "Fabric";
        if (versionId.Contains("quilt", StringComparison.OrdinalIgnoreCase)) return "Quilt";
        if (versionId.Contains("neoforge", StringComparison.OrdinalIgnoreCase)) return "NeoForge";
        if (versionId.Contains("forge", StringComparison.OrdinalIgnoreCase)) return "Forge";

        if (!string.IsNullOrWhiteSpace(rawType))
        {
            if (rawType.Equals("release", StringComparison.OrdinalIgnoreCase)) return "Vanilla";
            if (rawType.Equals("snapshot", StringComparison.OrdinalIgnoreCase)) return "Snapshot";
            return char.ToUpperInvariant(rawType[0]) + rawType[1..];
        }

        return "Vanilla";
    }
}
