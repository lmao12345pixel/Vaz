using System.Collections.Generic;
using System.Text.Json.Serialization;
using Vazer.Core.Modrinth;

namespace Vazer.Core.Modpacks;

public sealed record MrpackPreview
{
    public string Name { get; init; } = "";
    public string? Summary { get; init; }
    public string MinecraftVersion { get; init; } = "";
    public string LoaderType { get; init; } = "";
    public string? LoaderVersion { get; init; }
    public int FileCount { get; init; }
}

public sealed record MrpackIndex
{
    [JsonPropertyName("formatVersion")]
    public int FormatVersion { get; init; }

    [JsonPropertyName("game")]
    public string Game { get; init; } = "";

    [JsonPropertyName("versionId")]
    public string VersionId { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("files")]
    public List<MrpackFile> Files { get; init; } = new();

    [JsonPropertyName("dependencies")]
    public Dictionary<string, string> Dependencies { get; init; } = new();
}

public sealed record MrpackFile
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = "";

    [JsonPropertyName("hashes")]
    public FileHashes Hashes { get; init; } = new();

    [JsonPropertyName("env")]
    public MrpackEnv? Env { get; init; }

    [JsonPropertyName("downloads")]
    public List<string> Downloads { get; init; } = new();

    [JsonPropertyName("fileSize")]
    public long FileSize { get; init; }
}

public sealed record MrpackEnv
{
    [JsonPropertyName("client")]
    public string? Client { get; init; }

    [JsonPropertyName("server")]
    public string? Server { get; init; }
}
