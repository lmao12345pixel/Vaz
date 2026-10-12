using System.Text.Json.Serialization;

namespace Vazer.Core.Mods;

public sealed record InstalledMod
{
    [JsonPropertyName("fileName")]
    public string FileName { get; init; } = "";

    [JsonPropertyName("projectId")]
    public string ProjectId { get; init; } = "";

    [JsonPropertyName("versionId")]
    public string VersionId { get; init; } = "";

    [JsonPropertyName("source")]
    public string Source { get; init; } = "";

    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    [JsonPropertyName("installedAt")]
    public DateTimeOffset InstalledAt { get; init; }

    [JsonPropertyName("sha512")]
    public string? Sha512 { get; init; }
}

public sealed record ModsManifest
{
    [JsonPropertyName("mods")]
    public List<InstalledMod> Mods { get; init; } = new();
}

public sealed record InstallModResult
{
    public bool Success { get; init; }
    public string? Warning { get; init; }
    public string? ModName { get; init; }
    public string? FileName { get; init; }
    public string? VersionId { get; init; }
    public IReadOnlyList<string> InstalledDependencies { get; init; } = Array.Empty<string>();
}

public sealed record UpdateCheckResult
{
    public IReadOnlyList<ModUpdateInfo> Updates { get; init; } = Array.Empty<ModUpdateInfo>();
}

public sealed record ModUpdateInfo
{
    public InstalledMod CurrentMod { get; init; } = null!;
    public string LatestVersionId { get; init; } = "";
    public string LatestVersionNumber { get; init; } = "";
    public bool HasUpdate { get; init; }
}
