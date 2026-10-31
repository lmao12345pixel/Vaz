using Vazer.Core.Abstractions;

namespace Vazer.Core.Launch;

/// <summary>
/// Xác định tên version CmlLib cần tra khi launch instance.
/// CmlLib tra version theo TÊN VERSION (không phải instance id), vì vậy
/// instance phải lưu LaunchVersionName khi tạo. Với instance cũ không có
/// field này (snapshot trước 11/2026), suy ra theo quy ước đặt tên:
/// - vanilla: "1.21.1"
/// - fabric:  "fabric-loader-{loaderVersion}-{mcVersion}"
/// - quilt:   "quilt-loader-{loaderVersion}-{mcVersion}"
/// - neoforge: CmlLib không quản — dùng LaunchVersionName đã lưu, còn thiếu
///   lại trả về null để launcher quét versions/ trên đĩa (migrate-on-launch).
/// </summary>
public static class LaunchVersionResolver
{
    /// <summary>
    /// Trả về tên version CmlLib cho instance, hoặc null nếu không suy ra được
    /// (khi đó launcher phải quét đĩa hoặc báo lỗi thân thiện).
    /// </summary>
    public static string? Resolve(MinecraftInstance instance)
    {
        if (!string.IsNullOrEmpty(instance.LaunchVersionName))
        {
            return instance.LaunchVersionName;
        }

        return instance.LoaderType.ToLowerInvariant() switch
        {
            "vanilla" => instance.MinecraftVersion,
            "fabric" => string.IsNullOrEmpty(instance.LoaderVersion)
                ? null
                : $"fabric-loader-{instance.LoaderVersion}-{instance.MinecraftVersion}",
            "quilt" => string.IsNullOrEmpty(instance.LoaderVersion)
                ? null
                : $"quilt-loader-{instance.LoaderVersion}-{instance.MinecraftVersion}",
            _ => null
        };
    }
}
