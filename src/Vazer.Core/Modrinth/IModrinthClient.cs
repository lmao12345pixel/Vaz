namespace Vazer.Core.Modrinth;

public interface IModrinthClient
{
    Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken ct = default);

    /// <summary>
    /// Duyệt danh sách mod phổ biến/nổi bật ban đầu khi query rỗng (GET /v2/search?query=&...).
    /// Mặc định sắp xếp theo lượt tải (downloads).
    /// </summary>
    Task<SearchResult> GetFeaturedModsAsync(SearchQuery query, CancellationToken ct = default);
    Task<ModrinthProject> GetProjectAsync(string slugOrId, CancellationToken ct = default);
    Task<IReadOnlyList<ProjectVersion>> GetVersionsAsync(string slugOrId, string? loader = null, string? gameVersion = null, CancellationToken ct = default);
    Task<ProjectVersion?> GetVersionAsync(string versionId, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string fileUrl, CancellationToken ct = default);

    /// <summary>
    /// Tra cứu hàng loạt phiên bản theo sha512 (POST /version_files).
    /// Trả về bản đồ hash (lowercase) → phiên bản; hash không khớp sẽ không có trong bản đồ.
    /// </summary>
    Task<IReadOnlyDictionary<string, ProjectVersion>> GetVersionsFromHashesAsync(
        IEnumerable<string> sha512Hashes, CancellationToken ct = default);

    /// <summary>
    /// Tra cứu phiên bản mới nhất theo sha512 và tương thích loader/game_version (POST /version_files/update).
    /// Trả về bản đồ hash (lowercase) → phiên bản mới nhất tương thích; hash không khớp sẽ không có trong bản đồ.
    /// </summary>
    Task<IReadOnlyDictionary<string, ProjectVersion>> GetLatestVersionsFromHashesAsync(
        IEnumerable<string> sha512Hashes,
        IEnumerable<string>? loaders = null,
        IEnumerable<string>? gameVersions = null,
        CancellationToken ct = default);
}
