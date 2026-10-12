namespace Vazer.Core.Modrinth;

public interface IModrinthClient
{
    Task<SearchResult> SearchAsync(SearchQuery query, CancellationToken ct = default);
    Task<ModrinthProject> GetProjectAsync(string slugOrId, CancellationToken ct = default);
    Task<IReadOnlyList<ProjectVersion>> GetVersionsAsync(string slugOrId, string? loader = null, string? gameVersion = null, CancellationToken ct = default);
    Task<ProjectVersion?> GetVersionAsync(string versionId, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string fileUrl, CancellationToken ct = default);
}
