using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vazer.Core.Abstractions;
using Vazer.Core.Http;
using Vazer.Core.Modrinth;
using Vazer.Core.Mods;

namespace Vazer.Core.Loaders.Optifine;

public class OptifineInstallerService : IOptifineInstallerService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<MinecraftOptions> _options;
    private readonly ILogger<OptifineInstallerService> _logger;
    private readonly IModrinthClient? _modrinthClient;
    private readonly IFileDownloader _fileDownloader;

    private readonly ConcurrentDictionary<string, (DateTimeOffset CachedAt, OptifineCatalog Catalog)> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public OptifineInstallerService(
        IHttpClientFactory httpClientFactory,
        IOptions<MinecraftOptions> options,
        ILogger<OptifineInstallerService> logger,
        IModrinthClient? modrinthClient = null,
        IFileDownloader? fileDownloader = null)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
        _modrinthClient = modrinthClient;
        _fileDownloader = fileDownloader ?? new FileDownloader(httpClientFactory);
    }

    private TimeSpan CacheTtl => TimeSpan.FromMinutes(Math.Max(1, _options.Value.OptifineCatalogCacheTtlMinutes));

    public async Task<OptifineCatalog> GetCatalogAsync(string mcVersion, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mcVersion);

        if (_cache.TryGetValue(mcVersion, out var cached) && DateTimeOffset.UtcNow - cached.CachedAt < CacheTtl)
        {
            return cached.Catalog;
        }

        var client = _httpClientFactory.CreateClient("optifine");
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://optifine.net/downloads");
        if (!string.IsNullOrWhiteSpace(_options.Value.OptifineUserAgent))
        {
            request.Headers.TryAddWithoutValidation("User-Agent", _options.Value.OptifineUserAgent);
        }

        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(ct);

        var catalog = ParseCatalog(html, mcVersion);
        _cache[mcVersion] = (DateTimeOffset.UtcNow, catalog);
        return catalog;
    }

    public async Task<InstallOptifineResult> InstallFromCatalogAsync(
        MinecraftInstance instance,
        string optifineVersion,
        IProgress<InstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentException.ThrowIfNullOrWhiteSpace(optifineVersion);

        var catalog = await GetCatalogAsync(instance.MinecraftVersion, ct);
        var release = catalog.Releases.FirstOrDefault(r =>
            string.Equals(r.Version, optifineVersion, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r.Filename, optifineVersion, StringComparison.OrdinalIgnoreCase))
            ?? throw new OptifineVersionNotFoundException($"OptiFine version '{optifineVersion}' not found for Minecraft {instance.MinecraftVersion}.");

        return await DownloadAndInstallAsync(
            instance,
            release.DownloadPageUrl,
            release.Filename,
            release.Version,
            isAdloadx: true,
            progress,
            ct);
    }

    public async Task<InstallOptifineResult> InstallFromUrlAsync(
        MinecraftInstance instance,
        string manualUrl,
        IProgress<InstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentException.ThrowIfNullOrWhiteSpace(manualUrl);

        if (!Uri.TryCreate(manualUrl, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Invalid URL.", nameof(manualUrl));
        }

        if (!string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("URL must use HTTP or HTTPS.", nameof(manualUrl));
        }

        if (!string.Equals(uri.Host, "optifine.net", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Host, "www.optifine.net", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Host must be optifine.net or www.optifine.net.", nameof(manualUrl));
        }

        var fMatch = Regex.Match(uri.Query, @"[?&]f=([^&#]+)");
        if (!fMatch.Success || string.IsNullOrWhiteSpace(fMatch.Groups[1].Value))
        {
            throw new ArgumentException("URL must contain 'f' parameter with the filename.", nameof(manualUrl));
        }

        var filename = Uri.UnescapeDataString(fMatch.Groups[1].Value);
        if (!filename.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
        {
            filename += ".jar";
        }

        bool isAdloadx = uri.AbsolutePath.Contains("adloadx", StringComparison.OrdinalIgnoreCase);
        bool isDownloadx = uri.AbsolutePath.Contains("downloadx", StringComparison.OrdinalIgnoreCase);

        if (!isAdloadx && !isDownloadx)
        {
            throw new ArgumentException("URL must be an adloadx or downloadx link.", nameof(manualUrl));
        }

        var versionId = ExtractVersionFromFilename(filename, instance.MinecraftVersion);

        return await DownloadAndInstallAsync(
            instance,
            manualUrl,
            filename,
            versionId,
            isAdloadx,
            progress,
            ct);
    }

    internal static OptifineCatalog ParseCatalog(string html, string mcVersion)
    {
        var escapedVersion = Regex.Escape(mcVersion);
        var sectionMatch = Regex.Match(
            html,
            $@"<h2[^>]*>\s*Minecraft\s+{escapedVersion}\s*</h2>(?<content>.*?)(?=<h2[^>]*>\s*Minecraft|\Z)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (!sectionMatch.Success)
        {
            throw new OptifineVersionNotFoundException(mcVersion);
        }

        var sectionContent = sectionMatch.Groups["content"].Value;
        var rowMatches = Regex.Matches(
            sectionContent,
            @"<tr\s+class=[""'][^""']*downloadLine[^""']*[""'][^>]*>(?<row>.*?)</tr>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        var releases = new List<OptifineRelease>();

        foreach (Match rowMatch in rowMatches)
        {
            var rowHtml = rowMatch.Groups["row"].Value;
            bool isPreviewClass = rowMatch.Value.Contains("downloadLinePreview", StringComparison.OrdinalIgnoreCase);

            var colFileMatch = Regex.Match(rowHtml, @"<td[^>]*class=[""'][^""']*colFile[^""']*[""'][^>]*>(?<file>.*?)</td>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var rawFile = colFileMatch.Success
                ? WebUtility.HtmlDecode(Regex.Replace(colFileMatch.Groups["file"].Value, "<.*?>", "")).Trim()
                : "";

            var version = Regex.Replace(rawFile, @"^OptiFine\s*", "", RegexOptions.IgnoreCase).Trim();
            version = Regex.Replace(version, $@"^{escapedVersion}\s*", "", RegexOptions.IgnoreCase).Trim();

            var type = (isPreviewClass || rawFile.Contains("pre", StringComparison.OrdinalIgnoreCase) || version.Contains("pre", StringComparison.OrdinalIgnoreCase))
                ? "preview"
                : "stable";

            string? mirrorHref = null;
            var colMirrorMatch = Regex.Match(rowHtml, @"<td[^>]*class=[""'][^""']*colMirror[^""']*[""'][^>]*>(?<mirror>.*?)</td>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (colMirrorMatch.Success)
            {
                var aMatch = Regex.Match(colMirrorMatch.Groups["mirror"].Value, @"<a\s+[^>]*?href\s*=\s*['""](?<href>[^'""]+)['""]", RegexOptions.IgnoreCase);
                if (aMatch.Success)
                {
                    mirrorHref = aMatch.Groups["href"].Value;
                }
            }

            if (string.IsNullOrWhiteSpace(mirrorHref))
            {
                var colDownloadMatch = Regex.Match(rowHtml, @"<td[^>]*class=[""'][^""']*colDownload[^""']*[""'][^>]*>(?<down>.*?)</td>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (colDownloadMatch.Success)
                {
                    var aMatch = Regex.Match(colDownloadMatch.Groups["down"].Value, @"<a\s+[^>]*?href\s*=\s*['""](?<href>[^'""]+)['""]", RegexOptions.IgnoreCase);
                    if (aMatch.Success)
                    {
                        mirrorHref = aMatch.Groups["href"].Value;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(mirrorHref))
            {
                var anyAdloadMatch = Regex.Match(rowHtml, @"<a\s+[^>]*?href\s*=\s*['""](?<href>[^'""]*adloadx\?f=[^'""]+)['""]", RegexOptions.IgnoreCase);
                if (anyAdloadMatch.Success)
                {
                    mirrorHref = anyAdloadMatch.Groups["href"].Value;
                }
            }

            var downloadPageUrl = string.IsNullOrWhiteSpace(mirrorHref)
                ? ""
                : (mirrorHref.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || mirrorHref.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    ? mirrorHref
                    : new Uri(new Uri("https://optifine.net/"), mirrorHref).ToString());

            var fMatch = Regex.Match(downloadPageUrl, @"[?&]f=([^&#]+)");
            string edition = "HD_U";
            string filename;

            if (fMatch.Success)
            {
                filename = Uri.UnescapeDataString(fMatch.Groups[1].Value);
                var edMatch = Regex.Match(filename, @"(?:preview_)?OptiFine_[^_]+_([A-Za-z]+_[A-Za-z0-9]+)_");
                if (edMatch.Success)
                {
                    edition = edMatch.Groups[1].Value;
                }
            }
            else
            {
                var safeVer = version.Replace(" ", "_");
                filename = type == "preview"
                    ? $"preview_OptiFine_{mcVersion}_{edition}_{safeVer}.jar"
                    : $"OptiFine_{mcVersion}_{edition}_{safeVer}.jar";
            }

            var colDateMatch = Regex.Match(rowHtml, @"<td[^>]*class=[""'][^""']*colDate[^""']*[""'][^>]*>(?<date>.*?)</td>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var date = colDateMatch.Success
                ? WebUtility.HtmlDecode(Regex.Replace(colDateMatch.Groups["date"].Value, "<.*?>", "")).Trim()
                : null;
            if (string.IsNullOrWhiteSpace(date)) date = null;

            var colForgeMatch = Regex.Match(rowHtml, @"<td[^>]*class=[""'][^""']*colForge[^""']*[""'][^>]*>(?<forge>.*?)</td>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var forge = colForgeMatch.Success
                ? WebUtility.HtmlDecode(Regex.Replace(colForgeMatch.Groups["forge"].Value, "<.*?>", "")).Trim()
                : null;
            if (string.IsNullOrWhiteSpace(forge)) forge = null;

            var changelogMatch = Regex.Match(rowHtml, @"<a\s+[^>]*?href\s*=\s*['""](?<href>[^'""]*changelog[^'""]*)['""]", RegexOptions.IgnoreCase);
            string? changelogUrl = null;
            if (changelogMatch.Success)
            {
                var cl = changelogMatch.Groups["href"].Value;
                changelogUrl = cl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || cl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    ? cl
                    : new Uri(new Uri("https://optifine.net/"), cl).ToString();
            }

            releases.Add(new OptifineRelease
            {
                Version = version,
                Edition = edition,
                Type = type,
                DownloadPageUrl = downloadPageUrl,
                Filename = filename,
                ChangelogUrl = changelogUrl,
                ForgeVersion = forge,
                Date = date
            });
        }

        return new OptifineCatalog
        {
            McVersion = mcVersion,
            Releases = releases
        };
    }

    private async Task<InstallOptifineResult> DownloadAndInstallAsync(
        MinecraftInstance instance,
        string url,
        string filename,
        string versionId,
        bool isAdloadx,
        IProgress<InstallProgress>? progress,
        CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("optifine");
        string downloadUrl;

        if (isAdloadx)
        {
            using var adloadxRequest = new HttpRequestMessage(HttpMethod.Get, url);
            adloadxRequest.Headers.Referrer = new Uri("https://optifine.net/downloads");
            if (!string.IsNullOrWhiteSpace(_options.Value.OptifineUserAgent))
            {
                adloadxRequest.Headers.TryAddWithoutValidation("User-Agent", _options.Value.OptifineUserAgent);
            }

            using var adloadxResponse = await client.SendAsync(adloadxRequest, ct);
            adloadxResponse.EnsureSuccessStatusCode();
            var adloadxHtml = await adloadxResponse.Content.ReadAsStringAsync(ct);

            var downloadMatch = Regex.Match(
                adloadxHtml,
                @"<a\s+[^>]*?href\s*=\s*['""]((?:https?://(?:www\.)?optifine\.net/)?downloadx\?f=[^'""]+)['""]",
                RegexOptions.IgnoreCase);

            if (!downloadMatch.Success)
            {
                throw new InvalidOperationException("Failed to find download link on OptiFine adloadx page.");
            }

            var rawLink = downloadMatch.Groups[1].Value;
            downloadUrl = rawLink.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                          rawLink.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? rawLink
                : new Uri(new Uri("https://optifine.net/"), rawLink).ToString();
        }
        else
        {
            downloadUrl = url;
        }

        var modsDir = Path.Combine(instance.Path, "mods");
        Directory.CreateDirectory(modsDir);

        var partialPath = Path.Combine(modsDir, $"{filename}.partial");
        var jarPath = Path.Combine(modsDir, filename);

        if (File.Exists(partialPath))
        {
            File.Delete(partialPath);
        }

        try
        {
            await _fileDownloader.DownloadFileAsync(
                downloadUrl,
                partialPath,
                new FileDownloadOptions
                {
                    HttpClientName = "optifine",
                    StatusMessage = $"Downloading {filename}",
                    ConfigureRequest = downloadRequest =>
                    {
                        downloadRequest.Headers.Referrer = new Uri("https://optifine.net/downloads");
                        if (!string.IsNullOrWhiteSpace(_options.Value.OptifineUserAgent))
                        {
                            downloadRequest.Headers.TryAddWithoutValidation("User-Agent", _options.Value.OptifineUserAgent);
                        }
                    }
                },
                progress,
                ct);

            var fileInfo = new FileInfo(partialPath);
            if (!fileInfo.Exists || fileInfo.Length <= 102400)
            {
                if (File.Exists(partialPath)) File.Delete(partialPath);
                throw new InvalidOperationException("Invalid OptiFine jar file (magic bytes or size mismatch).");
            }

            byte[] magicBytes = new byte[4];
            using (var fs = File.OpenRead(partialPath))
            {
                _ = await fs.ReadAsync(magicBytes.AsMemory(0, 4), ct);
            }

            if (magicBytes[0] != 0x50 || magicBytes[1] != 0x4B || magicBytes[2] != 0x03 || magicBytes[3] != 0x04)
            {
                if (File.Exists(partialPath)) File.Delete(partialPath);
                throw new InvalidOperationException("Invalid OptiFine jar file (magic bytes or size mismatch).");
            }

            File.Move(partialPath, jarPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(partialPath))
            {
                try { File.Delete(partialPath); } catch (Exception ex) { _logger.LogDebug(ex, "Khong xoa duoc file partial '{Path}'", partialPath); }
            }
            throw;
        }

        await UpdateModsJsonAsync(instance.Path, filename, versionId, ct);

        string? warning = null;
        string? optifabricFork = null;

        if (string.Equals(instance.LoaderType, "fabric", StringComparison.OrdinalIgnoreCase))
        {
            warning = "OptiFabric không còn hoạt động trên MC hiện đại. Khuyến nghị: dùng Sodium + Iris (Performance Pack).";
            if (_modrinthClient != null)
            {
                try
                {
                    var searchResult = await _modrinthClient.SearchAsync(new SearchQuery
                    {
                        Query = "optifabric",
                        Loaders = ["fabric"],
                        GameVersions = [instance.MinecraftVersion]
                    }, ct);

                    if (searchResult.Hits is { Length: > 0 } && !string.IsNullOrWhiteSpace(searchResult.Hits[0].Slug))
                    {
                        optifabricFork = searchResult.Hits[0].Slug;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed opportunistic OptiFabric lookup on Modrinth.");
                }
            }
        }
        else if (string.Equals(instance.LoaderType, "neoforge", StringComparison.OrdinalIgnoreCase))
        {
            warning = "OptiFine trên NeoForge chưa test...";
        }
        else if (string.Equals(instance.LoaderType, "vanilla", StringComparison.OrdinalIgnoreCase))
        {
            warning = null;
        }

        return new InstallOptifineResult
        {
            Success = true,
            Warning = warning,
            OptifabricFork = optifabricFork,
            InstalledFilePath = jarPath
        };
    }

    private static string ExtractVersionFromFilename(string filename, string? mcVersion)
    {
        var name = Path.GetFileNameWithoutExtension(filename);
        if (name.StartsWith("preview_", StringComparison.OrdinalIgnoreCase))
        {
            name = name["preview_".Length..];
        }
        if (name.StartsWith("OptiFine_", StringComparison.OrdinalIgnoreCase))
        {
            name = name["OptiFine_".Length..];
        }
        if (!string.IsNullOrWhiteSpace(mcVersion) && name.StartsWith(mcVersion + "_", StringComparison.OrdinalIgnoreCase))
        {
            name = name[(mcVersion.Length + 1)..];
        }
        return name.Replace('_', ' ').Trim();
    }

    private async Task UpdateModsJsonAsync(string instancePath, string filename, string versionId, CancellationToken ct)
    {
        var manifestPath = Path.Combine(instancePath, "mods.json");
        ModsManifest manifest;
        if (File.Exists(manifestPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(manifestPath, ct);
                manifest = JsonSerializer.Deserialize<ModsManifest>(json, JsonOptions) ?? new ModsManifest();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read mods.json at {Path}, creating new manifest", manifestPath);
                manifest = new ModsManifest();
            }
        }
        else
        {
            manifest = new ModsManifest();
        }

        manifest.Mods.RemoveAll(m =>
            string.Equals(m.ProjectId, "optifine", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(m.FileName, filename, StringComparison.OrdinalIgnoreCase));

        manifest.Mods.Add(new InstalledMod
        {
            FileName = filename,
            ProjectId = "optifine",
            VersionId = versionId,
            Source = "optifine",
            Enabled = true,
            InstalledAt = DateTimeOffset.UtcNow,
            Sha512 = null
        });

        Directory.CreateDirectory(instancePath);
        var tmpPath = Path.Combine(instancePath, "mods.json.tmp");
        var serialized = JsonSerializer.Serialize(manifest, JsonOptions);
        await File.WriteAllTextAsync(tmpPath, serialized, ct);
        File.Move(tmpPath, manifestPath, overwrite: true);
    }
}
