using Vazer.Core.Abstractions;

namespace Vazer.Core.Loaders.Optifine;

public sealed class OptifineVersionNotFoundException : Exception
{
    public string McVersion { get; }
    public OptifineVersionNotFoundException(string mcVersion) 
        : base($"Minecraft version {mcVersion} not found in OptiFine catalog.")
    {
        McVersion = mcVersion;
    }
}

public sealed record OptifineCatalog
{
    public required string McVersion { get; init; }
    public required IReadOnlyList<OptifineRelease> Releases { get; init; }
}

public sealed record OptifineRelease
{
    public required string Version { get; init; } // e.g. "HD U J1" or "HD U J4 pre2"
    public required string Edition { get; init; } // e.g. "HD_U"
    public required string Type { get; init; } // "stable" or "preview"
    public required string DownloadPageUrl { get; init; } // adloadx URL
    public required string Filename { get; init; }
    public string? ChangelogUrl { get; init; }
    public string? ForgeVersion { get; init; }
    public string? Date { get; init; }
}

public sealed record InstallOptifineResult
{
    public required bool Success { get; init; }
    public string? Warning { get; init; }
    public string? OptifabricFork { get; init; }
    public string? InstalledFilePath { get; init; }
}
