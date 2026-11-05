using Vazer.Core.Versions;

namespace Vazer.Core.Loaders;

/// <summary>Helper dùng chung cho các loader installer + InstanceCreationService.</summary>
internal static class LoaderHelpers
{
    /// <summary>ID/folder name an toàn từ tên instance (giống InstanceManager.SanitizeId).</summary>
    internal static string SanitizeId(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var id = string.Join("", name.Select(c => invalid.Contains(c) ? '_' : c));
        return id.ToLowerInvariant();
    }

    /// <summary>Loader version bắt buộc (fail nhanh với message rõ ràng).</summary>
    internal static string RequireLoaderVersion(string? loaderVersion, string minecraftVersion)
    {
        if (string.IsNullOrWhiteSpace(loaderVersion))
        {
            throw new InvalidOperationException($"Loader for Minecraft {minecraftVersion} has no version");
        }

        return loaderVersion;
    }

    /// <summary>
    /// Chọn loader version: nếu user chỉ định (exact match, case-insensitive) → trả về nó;
    /// nếu không → bản stable mới nhất, fallback bản bất kỳ mới nhất.
    /// </summary>
    /// <param name="loaders">(Version, Stable) của toàn bộ loader version tương thích.</param>
    /// <param name="requested">Loader version user chọn (null = tự chọn).</param>
    /// <returns>Version đã chọn, hoặc null nếu requested không tồn tại trong danh sách.</returns>
    internal static string? SelectLatestStable(
        IEnumerable<(string Version, bool Stable)> loaders,
        string? requested)
    {
        var list = loaders.ToList();
        if (list.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(requested))
        {
            return list.Any(l => string.Equals(l.Version, requested, StringComparison.OrdinalIgnoreCase))
                ? requested
                : null;
        }

        var stable = list
            .Where(l => l.Stable)
            .Select(l => l.Version)
            .OrderByDescending(v => v, new LoaderVersionComparer())
            .FirstOrDefault();

        return stable ?? list
            .Select(l => l.Version)
            .OrderByDescending(v => v, new LoaderVersionComparer())
            .FirstOrDefault();
    }
}
