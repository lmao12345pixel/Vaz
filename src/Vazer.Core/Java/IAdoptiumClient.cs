using System.Threading;
using Vazer.Core.Abstractions;

namespace Vazer.Core.Java;

/// <summary>
/// Adoptium Temurin release metadata + file download.
/// Extracted seam so <see cref="JavaResolver"/> I/O is unit-testable
/// without network. Production implementation is <see cref="AdoptiumClient"/>.
/// </summary>
public interface IAdoptiumClient
{
    /// <summary>
    /// Queries the Adoptium API for the latest JDK asset of a major version.
    /// </summary>
    Task<AdoptiumAssetInfo> GetLatestJdkAsync(int major, CancellationToken ct = default);

    /// <summary>
    /// Downloads a file with progress reporting.
    /// </summary>
    Task DownloadFileAsync(string url, string destination, IProgress<InstallProgress>? progress, CancellationToken ct = default);
}

/// <summary>Adoptium release asset (download link + integrity metadata).</summary>
public sealed record AdoptiumAssetInfo
{
    /// <summary>Direct download URL of the archive.</summary>
    public required string DownloadUrl { get; init; }

    /// <summary>SHA256 checksum (hex) published by the API.</summary>
    public required string Sha256Checksum { get; init; }

    /// <summary>Archive size in bytes (0 when unknown).</summary>
    public required long SizeBytes { get; init; }
}
