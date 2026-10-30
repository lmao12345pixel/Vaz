namespace Vazer.Core.Modrinth;

public interface IModrinthClient
{
    Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken ct = default);
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
}
