using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vazer.Core.Modrinth;

namespace Vaz.WinUI.Services;

/// <summary>
/// Tùy chọn tìm kiếm mod trên Modrinth.
/// </summary>
public sealed record SearchFilterOptions
{
    public string? Query { get; init; }
    public string[]? Loaders { get; init; }
    public string[]? GameVersions { get; init; }
    public string? Category { get; init; }
    public string? SortIndex { get; init; }
    public int Offset { get; init; }
    public int Limit { get; init; } = 20;
}

/// <summary>
/// Service chuyên trách tìm kiếm, phân trang và gợi ý bộ lọc động từ Modrinth.
/// </summary>
public interface IModrinthSearchService
{
    /// <summary>Tìm kiếm mod theo từ khóa và bộ lọc.</summary>
    Task<SearchResult> SearchAsync(SearchFilterOptions options, CancellationToken ct = default);

    /// <summary>Lấy danh sách mod nổi bật / phổ biến (mặc định khi ô tìm kiếm rỗng).</summary>
    Task<SearchResult> GetFeaturedModsAsync(SearchFilterOptions options, CancellationToken ct = default);

    /// <summary>Đề xuất danh sách Category (Filter Chips) động dựa theo từ khóa người dùng gõ.</summary>
    IReadOnlyList<string> GetSuggestedCategories(string? query);
}
