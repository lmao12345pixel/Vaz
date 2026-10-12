using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vazer.Core.Modrinth;

public sealed record SearchQuery
{
    public string? Query { get; init; }
    public string[]? Loaders { get; init; }
    public string[]? GameVersions { get; init; }
    public string[]? Categories { get; init; }
    public string? SortIndex { get; init; } // relevance, downloads, follows, newest, updated
    public int Offset { get; init; }
    public int Limit { get; init; } = 20; // max 100
}

public sealed record SearchResult
{
    [JsonPropertyName("hits")]
    public SearchHit[] Hits { get; init; } = [];

    [JsonPropertyName("total_hits")]
    public int TotalHits { get; init; }

    [JsonPropertyName("offset")]
    public int Offset { get; init; }

    [JsonPropertyName("limit")]
    public int Limit { get; init; }
}

public sealed record SearchHit
{
    [JsonPropertyName("project_id")]
    public string ProjectId { get; init; } = "";

    [JsonPropertyName("slug")]
    public string Slug { get; init; } = "";

    [JsonPropertyName("title")]
    public string Title { get; init; } = "";

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("author")]
    public string Author { get; init; } = "";

    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; init; }

    [JsonPropertyName("downloads")]
    public long Downloads { get; init; }

    [JsonPropertyName("follows")]
    public int Follows { get; init; }

    [JsonPropertyName("categories")]
    public string[] Categories { get; init; } = [];

    [JsonPropertyName("versions")]
    public string[] Versions { get; init; } = [];

    [JsonPropertyName("project_type")]
    public string ProjectType { get; init; } = "";

    [JsonPropertyName("latest_version")]
    public string? LatestVersion { get; init; }

    [JsonPropertyName("date_modified")]
    public DateTimeOffset? DateModified { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed record ModrinthProject
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("slug")]
    public string Slug { get; init; } = "";

    [JsonPropertyName("title")]
    public string Title { get; init; } = "";

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("body")]
    public string Body { get; init; } = "";

    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; init; }

    [JsonPropertyName("gallery")]
    public string[]? Gallery { get; init; }

    [JsonPropertyName("downloads")]
    public long Downloads { get; init; }

    [JsonPropertyName("followers")]
    public int Followers { get; init; }

    [JsonPropertyName("categories")]
    public string[] Categories { get; init; } = [];

    [JsonPropertyName("loaders")]
    public string[] Loaders { get; init; } = [];

    [JsonPropertyName("game_versions")]
    public string[] GameVersions { get; init; } = [];

    [JsonPropertyName("versions")]
    public string[] Versions { get; init; } = [];

    [JsonPropertyName("project_type")]
    public string ProjectType { get; init; } = "";

    [JsonPropertyName("team")]
    public string? Team { get; init; }

    [JsonPropertyName("published")]
    public DateTimeOffset? Published { get; init; }

    [JsonPropertyName("updated")]
    public DateTimeOffset? Updated { get; init; }

    [JsonPropertyName("license")]
    public ModrinthLicense? License { get; init; }

    [JsonPropertyName("issues_url")]
    public string? IssuesUrl { get; init; }

    [JsonPropertyName("source_url")]
    public string? SourceUrl { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed record ModrinthLicense
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("url")]
    public string? Url { get; init; }
}

public sealed record ProjectVersion
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("project_id")]
    public string ProjectId { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("version_number")]
    public string VersionNumber { get; init; } = "";

    [JsonPropertyName("version_type")]
    public string VersionType { get; init; } = ""; // release, beta, alpha

    [JsonPropertyName("game_versions")]
    public string[] GameVersions { get; init; } = [];

    [JsonPropertyName("loaders")]
    public string[] Loaders { get; init; } = [];

    [JsonPropertyName("dependencies")]
    public VersionDependency[] Dependencies { get; init; } = [];

    [JsonPropertyName("files")]
    public VersionFile[] Files { get; init; } = [];

    [JsonPropertyName("date_published")]
    public DateTimeOffset? DatePublished { get; init; }

    [JsonPropertyName("changelog")]
    public string? Changelog { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed record VersionDependency
{
    [JsonPropertyName("version_id")]
    public string? VersionId { get; init; }

    [JsonPropertyName("project_id")]
    public string? ProjectId { get; init; }

    [JsonPropertyName("file_name")]
    public string? FileName { get; init; }

    [JsonPropertyName("dependency_type")]
    public string DependencyType { get; init; } = ""; // required, optional, incompatible, embedded
}

public sealed record VersionFile
{
    [JsonPropertyName("hashes")]
    public FileHashes Hashes { get; init; } = new();

    [JsonPropertyName("url")]
    public string Url { get; init; } = "";

    [JsonPropertyName("filename")]
    public string Filename { get; init; } = "";

    [JsonPropertyName("primary")]
    public bool Primary { get; init; }

    [JsonPropertyName("size")]
    public long Size { get; init; }

    [JsonPropertyName("file_type")]
    public string? FileType { get; init; }
}

public sealed record FileHashes
{
    [JsonPropertyName("sha1")]
    public string? Sha1 { get; init; }

    [JsonPropertyName("sha512")]
    public string? Sha512 { get; init; }
}
