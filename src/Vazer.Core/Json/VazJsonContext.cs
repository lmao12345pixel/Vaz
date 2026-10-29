using System.Text.Json;
using System.Text.Json.Serialization;
using Vazer.Core.Abstractions;
using Vazer.Core.Auth;
using Vazer.Core.Modpacks;
using Vazer.Core.Modrinth;
using Vazer.Core.Mods;
using Vazer.Core.Settings;
using Vazer.Core.Store;

namespace Vazer.Core.Json;

/// <summary>
/// Source-generated JSON metadata for every type V.A.Z persists or receives.
/// Using <see cref="JsonTypeInfo{T}"/> overloads keeps the Core library safe
/// for the trimmed publish configured in Vazer.Core.csproj.
/// </summary>
[JsonSerializable(typeof(MinecraftInstance))]
[JsonSerializable(typeof(MinecraftAccount))]
[JsonSerializable(typeof(List<MinecraftAccount>))]
[JsonSerializable(typeof(AccountsFile))]
[JsonSerializable(typeof(ModsManifest))]
[JsonSerializable(typeof(InstalledMod))]
[JsonSerializable(typeof(MrpackIndex))]
[JsonSerializable(typeof(MrpackFile))]
[JsonSerializable(typeof(MrpackEnv))]
[JsonSerializable(typeof(SearchResult))]
[JsonSerializable(typeof(SearchHit))]
[JsonSerializable(typeof(ModrinthProject))]
[JsonSerializable(typeof(ProjectVersion))]
[JsonSerializable(typeof(ProjectVersion[]))]
[JsonSerializable(typeof(VersionDependency))]
[JsonSerializable(typeof(VersionFile))]
[JsonSerializable(typeof(ModrinthGalleryImage))]
[JsonSerializable(typeof(ModrinthLicense))]
[JsonSerializable(typeof(FileHashes))]
[JsonSerializable(typeof(XboxAuthRequest))]
[JsonSerializable(typeof(XstsAuthRequest))]
[JsonSerializable(typeof(MinecraftLoginRequest))]
[JsonSerializable(typeof(GlobalSettings))]
internal sealed partial class VazJsonContext : JsonSerializerContext
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Instance metadata, mods manifest, OptiFine manifest, MSAL payloads.</summary>
    internal static readonly VazJsonContext CamelCase = new(CamelCaseOptions);

    private static readonly JsonSerializerOptions AccountOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Account file: camelCase and nulls omitted, matching the previous store format.</summary>
    internal static readonly VazJsonContext Accounts = new(AccountOptions);

    private static readonly JsonSerializerOptions SettingsOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Global launcher settings (settings.json) — camelCase như web state.</summary>
    internal static readonly VazJsonContext Settings = new(SettingsOptions);

    private static readonly JsonSerializerOptions PlainOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Modrinth and mrpack payloads, whose properties use explicit names.</summary>
    internal static readonly VazJsonContext Plain = new(PlainOptions);
}
