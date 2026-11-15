using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Vazer.Core.Abstractions;
using Vazer.Core.Modpacks;
using Vazer.Core.Modrinth;
using Vazer.Core.Mods;
using Vazer.Core.Tests.TestHelpers;

namespace Vazer.Core.Tests;

public sealed class MrpackImporterTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly Mock<IInstanceManager> _instanceManagerMock;
    private readonly FakeHttpMessageHandler _http;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MrpackImporter _importer;

    public MrpackImporterTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "upco-mrpack-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        _instanceManagerMock = new Mock<IInstanceManager>();
        _http = new FakeHttpMessageHandler();
        _httpClientFactory = new FakeHttpClientFactory(_http);

        _importer = new MrpackImporter(
            _instanceManagerMock.Object,
            _httpClientFactory,
            NullLogger<MrpackImporter>.Instance);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public async Task Inspect_ValidMrpack_ReturnsMetadata()
    {
        var packPath = CreateMrpack(
            name: "My Cool Modpack",
            files: new List<MrpackFile>
            {
                new() { Path = "mods/mod1.jar" },
                new() { Path = "mods/mod2.jar" }
            });

        var preview = await _importer.InspectAsync(packPath);

        preview.Name.Should().Be("My Cool Modpack");
        preview.Summary.Should().Be("Test Summary");
        preview.MinecraftVersion.Should().Be("1.21.1");
        preview.LoaderType.Should().Be("fabric");
        preview.LoaderVersion.Should().Be("0.15.11");
        preview.FileCount.Should().Be(2);
    }

    [Fact]
    public async Task Inspect_InvalidFormatVersion_ThrowsInvalidDataException()
    {
        var packPath = CreateMrpack(formatVersion: 2);

        var act = () => _importer.InspectAsync(packPath);

        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*Unsupported format version*");
    }

    [Fact]
    public async Task Inspect_InvalidGame_ThrowsInvalidDataException()
    {
        var packPath = CreateMrpack(game: "other-game");

        var act = () => _importer.InspectAsync(packPath);

        await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*Unsupported game*");
    }

    [Fact]
    public async Task Import_ValidFabricMrpack_CreatesInstanceAndDownloadsFiles()
    {
        var instanceDir = Path.Combine(_tempRoot, "instances", "TestInst");
        Directory.CreateDirectory(instanceDir);

        var instance = new MinecraftInstance
        {
            Id = "inst-fabric-1",
            Name = "TestInst",
            MinecraftVersion = "1.21.1",
            LoaderType = "fabric",
            LoaderVersion = "0.15.11",
            Path = instanceDir,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _instanceManagerMock
            .Setup(m => m.CreateFabricAsync("TestInst", "1.21.1", "0.15.11", It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        var modBytes = "mod-binary-content"u8.ToArray();
        var sha512Hex = Convert.ToHexString(SHA512.HashData(modBytes));

        _http.Map(HttpMethod.Get, "/download/modA.jar", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(modBytes)
        });

        var packPath = CreateMrpack(
            files: new List<MrpackFile>
            {
                new()
                {
                    Path = "mods/modA.jar",
                    Downloads = new List<string> { "https://cdn.modrinth.com/download/modA.jar" },
                    Hashes = new FileHashes { Sha512 = sha512Hex },
                    FileSize = modBytes.Length
                }
            },
            overrides: new Dictionary<string, string>
            {
                ["config/server.properties"] = "motd=Hello"
            },
            clientOverrides: new Dictionary<string, string>
            {
                ["options.txt"] = "gamma:2.0"
            });

        var progressReports = new List<InstallProgress>();
        var progress = new Progress<InstallProgress>(p => progressReports.Add(p));

        await _importer.ImportAsync(packPath, "TestInst", progress);

        File.Exists(Path.Combine(instanceDir, "mods", "modA.jar")).Should().BeTrue();
        (await File.ReadAllBytesAsync(Path.Combine(instanceDir, "mods", "modA.jar"))).Should().Equal(modBytes);

        File.Exists(Path.Combine(instanceDir, "config", "server.properties")).Should().BeTrue();
        (await File.ReadAllTextAsync(Path.Combine(instanceDir, "config", "server.properties"))).Should().Be("motd=Hello");

        File.Exists(Path.Combine(instanceDir, "options.txt")).Should().BeTrue();
        (await File.ReadAllTextAsync(Path.Combine(instanceDir, "options.txt"))).Should().Be("gamma:2.0");

        var manifestPath = Path.Combine(instanceDir, "mods.json");
        File.Exists(manifestPath).Should().BeTrue();
        var manifestJson = await File.ReadAllTextAsync(manifestPath);
        var manifest = JsonSerializer.Deserialize<ModsManifest>(manifestJson);
        manifest.Should().NotBeNull();
        manifest!.Mods.Should().ContainSingle(m => m.FileName == "modA.jar" && m.Source == "mrpack");
    }

    [Fact]
    public async Task Import_ZipSlipInFiles_ThrowsAndRollsBack()
    {
        var instanceDir = Path.Combine(_tempRoot, "instances", "SlipFilesInst");
        Directory.CreateDirectory(instanceDir);

        var instance = new MinecraftInstance
        {
            Id = "inst-slip-files",
            Name = "SlipFilesInst",
            MinecraftVersion = "1.21.1",
            LoaderType = "fabric",
            Path = instanceDir,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _instanceManagerMock
            .Setup(m => m.CreateFabricAsync("SlipFilesInst", "1.21.1", "0.15.11", It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        var packPath = CreateMrpack(
            files: new List<MrpackFile>
            {
                new()
                {
                    Path = "../../evil.txt",
                    Downloads = new List<string> { "https://cdn.modrinth.com/download/evil.txt" }
                }
            });

        var act = () => _importer.ImportAsync(packPath, "SlipFilesInst");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*zip slip*");

        _instanceManagerMock.Verify(m => m.DeleteAsync("inst-slip-files", true, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Import_ZipSlipInOverrides_ThrowsAndRollsBack()
    {
        var instanceDir = Path.Combine(_tempRoot, "instances", "SlipOverridesInst");
        Directory.CreateDirectory(instanceDir);

        var instance = new MinecraftInstance
        {
            Id = "inst-slip-overrides",
            Name = "SlipOverridesInst",
            MinecraftVersion = "1.21.1",
            LoaderType = "fabric",
            Path = instanceDir,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _instanceManagerMock
            .Setup(m => m.CreateFabricAsync("SlipOverridesInst", "1.21.1", "0.15.11", It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        var packPath = CreateMrpack(
            overrides: new Dictionary<string, string>
            {
                ["../../evil.txt"] = "malicious payload"
            });

        var act = () => _importer.ImportAsync(packPath, "SlipOverridesInst");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*zip slip*");

        _instanceManagerMock.Verify(m => m.DeleteAsync("inst-slip-overrides", true, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Import_ForgeLoader_CreatesForgeInstance()
    {
        var instanceDir = Path.Combine(_tempRoot, "instances", "ForgeInst");
        Directory.CreateDirectory(instanceDir);

        var instance = new MinecraftInstance
        {
            Id = "forgeinst",
            Name = "ForgeInst",
            MinecraftVersion = "1.20.1",
            LoaderType = "forge",
            LoaderVersion = "47.1.0",
            Path = instanceDir,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _instanceManagerMock
            .Setup(m => m.CreateForgeAsync("ForgeInst", "1.20.1", "47.1.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        var packPath = CreateMrpack(
            dependencies: new Dictionary<string, string>
            {
                ["minecraft"] = "1.20.1",
                ["forge"] = "47.1.0"
            });

        await _importer.ImportAsync(packPath, "ForgeInst");

        _instanceManagerMock.Verify(m => m.CreateForgeAsync("ForgeInst", "1.20.1", "47.1.0", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Import_DownloadFailure_RollsBackCreatedInstance()
    {
        var instanceDir = Path.Combine(_tempRoot, "instances", "DlFailInst");
        Directory.CreateDirectory(instanceDir);

        var instance = new MinecraftInstance
        {
            Id = "inst-dl-fail",
            Name = "DlFailInst",
            MinecraftVersion = "1.21.1",
            LoaderType = "fabric",
            Path = instanceDir,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _instanceManagerMock
            .Setup(m => m.CreateFabricAsync("DlFailInst", "1.21.1", "0.15.11", It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        var mod1Bytes = "mod1-content"u8.ToArray();
        _http.Map(HttpMethod.Get, "/download/mod1.jar", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(mod1Bytes)
        });
        _http.Map(HttpMethod.Get, "/download/mod2.jar", _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var packPath = CreateMrpack(
            files: new List<MrpackFile>
            {
                new()
                {
                    Path = "mods/mod1.jar",
                    Downloads = new List<string> { "https://cdn.modrinth.com/download/mod1.jar" }
                },
                new()
                {
                    Path = "mods/mod2.jar",
                    Downloads = new List<string> { "https://cdn.modrinth.com/download/mod2.jar" }
                }
            });

        var act = () => _importer.ImportAsync(packPath, "DlFailInst");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*all mirrors*");

        _instanceManagerMock.Verify(m => m.DeleteAsync("inst-dl-fail", true, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Import_EnvServerRequiredClientUnsupported_FileSkipped()
    {
        var instanceDir = Path.Combine(_tempRoot, "instances", "EnvSkipInst");
        Directory.CreateDirectory(instanceDir);

        var instance = new MinecraftInstance
        {
            Id = "inst-env-skip",
            Name = "EnvSkipInst",
            MinecraftVersion = "1.21.1",
            LoaderType = "fabric",
            Path = instanceDir,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _instanceManagerMock
            .Setup(m => m.CreateFabricAsync("EnvSkipInst", "1.21.1", "0.15.11", It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        var skippedBytes = "skipped-content"u8.ToArray();
        var keptBytes = "kept-content"u8.ToArray();
        var keptSha = Convert.ToHexString(SHA512.HashData(keptBytes));

        _http.Map(HttpMethod.Get, "/download/skipped.jar", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(skippedBytes)
        });
        _http.Map(HttpMethod.Get, "/download/kept.jar", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(keptBytes)
        });

        var packPath = CreateMrpack(
            files: new List<MrpackFile>
            {
                new()
                {
                    Path = "mods/skipped.jar",
                    Downloads = new List<string> { "https://cdn.modrinth.com/download/skipped.jar" },
                    Env = new MrpackEnv { Client = "unsupported", Server = "required" }
                },
                new()
                {
                    Path = "mods/kept.jar",
                    Downloads = new List<string> { "https://cdn.modrinth.com/download/kept.jar" },
                    Hashes = new FileHashes { Sha512 = keptSha },
                    FileSize = keptBytes.Length
                }
            });

        await _importer.ImportAsync(packPath, "EnvSkipInst");

        File.Exists(Path.Combine(instanceDir, "mods", "skipped.jar")).Should().BeFalse();
        File.Exists(Path.Combine(instanceDir, "mods", "kept.jar")).Should().BeTrue();
    }

    [Fact]
    public async Task Import_Sha512Mismatch_ThrowsAndRollsBack()
    {
        var instanceDir = Path.Combine(_tempRoot, "instances", "ShaMismatchInst");
        Directory.CreateDirectory(instanceDir);

        var instance = new MinecraftInstance
        {
            Id = "inst-sha-mismatch",
            Name = "ShaMismatchInst",
            MinecraftVersion = "1.21.1",
            LoaderType = "fabric",
            Path = instanceDir,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _instanceManagerMock
            .Setup(m => m.CreateFabricAsync("ShaMismatchInst", "1.21.1", "0.15.11", It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        var wrongBytes = "wrong-content"u8.ToArray();
        var expectedSha = Convert.ToHexString(SHA512.HashData("expected-content"u8.ToArray()));

        _http.Map(HttpMethod.Get, "/download/bad.jar", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(wrongBytes)
        });

        var packPath = CreateMrpack(
            files: new List<MrpackFile>
            {
                new()
                {
                    Path = "mods/bad.jar",
                    Downloads = new List<string> { "https://cdn.modrinth.com/download/bad.jar" },
                    Hashes = new FileHashes { Sha512 = expectedSha },
                    FileSize = wrongBytes.Length
                }
            });

        var act = () => _importer.ImportAsync(packPath, "ShaMismatchInst");

        await act.Should().ThrowAsync<InvalidOperationException>();

        _instanceManagerMock.Verify(m => m.DeleteAsync("inst-sha-mismatch", true, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Import_NeoforgeDependencies_MapsToNeoForgeInstance()
    {
        var instanceDir = Path.Combine(_tempRoot, "instances", "NeoInst");
        Directory.CreateDirectory(instanceDir);

        var instance = new MinecraftInstance
        {
            Id = "inst-neo",
            Name = "NeoInst",
            MinecraftVersion = "1.21.1",
            LoaderType = "neoforge",
            LoaderVersion = "21.1.0",
            Path = instanceDir,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _instanceManagerMock
            .Setup(m => m.CreateNeoForgeAsync("NeoInst", "1.21.1", "21.1.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        var packPath = CreateMrpack(
            dependencies: new Dictionary<string, string>
            {
                ["minecraft"] = "1.21.1",
                ["neoforge"] = "21.1.0"
            });

        var act = () => _importer.ImportAsync(packPath, "NeoInst");

        await act.Should().NotThrowAsync();

        _instanceManagerMock.Verify(m => m.CreateNeoForgeAsync("NeoInst", "1.21.1", "21.1.0", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Import_NoLoaderKey_CreatesVanillaInstance()
    {
        var instanceDir = Path.Combine(_tempRoot, "instances", "VanillaInst");
        Directory.CreateDirectory(instanceDir);

        var instance = new MinecraftInstance
        {
            Id = "inst-vanilla",
            Name = "VanillaInst",
            MinecraftVersion = "1.21.1",
            LoaderType = "vanilla",
            Path = instanceDir,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _instanceManagerMock
            .Setup(m => m.CreateVanillaAsync("VanillaInst", "1.21.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        var packPath = CreateMrpack(
            dependencies: new Dictionary<string, string>
            {
                ["minecraft"] = "1.21.1"
            });

        var act = () => _importer.ImportAsync(packPath, "VanillaInst");

        await act.Should().NotThrowAsync();

        _instanceManagerMock.Verify(m => m.CreateVanillaAsync("VanillaInst", "1.21.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Import_MultipleFiles_DownloadsConcurrently()
    {
        var instanceDir = Path.Combine(_tempRoot, "instances", "ConcurrentInst");
        Directory.CreateDirectory(instanceDir);

        var instance = new MinecraftInstance
        {
            Id = "inst-concurrent",
            Name = "ConcurrentInst",
            MinecraftVersion = "1.21.1",
            LoaderType = "fabric",
            Path = instanceDir,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _instanceManagerMock
            .Setup(m => m.CreateFabricAsync("ConcurrentInst", "1.21.1", "0.15.11", It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        var modA = "mod-a-content"u8.ToArray();
        var modB = "mod-b-content"u8.ToArray();
        var modC = "mod-c-content"u8.ToArray();

        var shaA = Convert.ToHexString(SHA512.HashData(modA));
        var shaB = Convert.ToHexString(SHA512.HashData(modB));
        var shaC = Convert.ToHexString(SHA512.HashData(modC));

        var downloadOrder = new ConcurrentBag<string>();
        _http.Map(HttpMethod.Get, "/download/modA.jar", _ =>
        {
            downloadOrder.Add("modA");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(modA) };
        });
        _http.Map(HttpMethod.Get, "/download/modB.jar", _ =>
        {
            downloadOrder.Add("modB");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(modB) };
        });
        _http.Map(HttpMethod.Get, "/download/modC.jar", _ =>
        {
            downloadOrder.Add("modC");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(modC) };
        });

        var packPath = CreateMrpack(
            files: new List<MrpackFile>
            {
                new() { Path = "mods/modA.jar", Downloads = new List<string> { "https://cdn.modrinth.com/download/modA.jar" }, Hashes = new FileHashes { Sha512 = shaA }, FileSize = modA.Length },
                new() { Path = "mods/modB.jar", Downloads = new List<string> { "https://cdn.modrinth.com/download/modB.jar" }, Hashes = new FileHashes { Sha512 = shaB }, FileSize = modB.Length },
                new() { Path = "mods/modC.jar", Downloads = new List<string> { "https://cdn.modrinth.com/download/modC.jar" }, Hashes = new FileHashes { Sha512 = shaC }, FileSize = modC.Length },
            });

        await _importer.ImportAsync(packPath, "ConcurrentInst");

        File.Exists(Path.Combine(instanceDir, "mods", "modA.jar")).Should().BeTrue();
        File.Exists(Path.Combine(instanceDir, "mods", "modB.jar")).Should().BeTrue();
        File.Exists(Path.Combine(instanceDir, "mods", "modC.jar")).Should().BeTrue();

        downloadOrder.Should().HaveCount(3);
    }

    [Fact]
    public async Task Import_PrimaryMirrorFails_FailsOverToSecondaryMirror()
    {
        var instanceDir = Path.Combine(_tempRoot, "instances", "FailoverInst");
        Directory.CreateDirectory(instanceDir);

        var instance = new MinecraftInstance
        {
            Id = "inst-failover",
            Name = "FailoverInst",
            MinecraftVersion = "1.21.1",
            LoaderType = "fabric",
            Path = instanceDir,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _instanceManagerMock
            .Setup(m => m.CreateFabricAsync("FailoverInst", "1.21.1", "0.15.11", It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        var modBytes = "mod-content"u8.ToArray();
        var sha = Convert.ToHexString(SHA512.HashData(modBytes));

        var attemptCount = 0;
        _http.Map(HttpMethod.Get, "/download/bad.jar", _ =>
        {
            attemptCount++;
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        });
        _http.Map(HttpMethod.Get, "/download/good.jar", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(modBytes)
        });

        var packPath = CreateMrpack(
            files: new List<MrpackFile>
            {
                new()
                {
                    Path = "mods/mod.jar",
                    Downloads = new List<string>
                    {
                        "https://cdn.modrinth.com/download/bad.jar",
                        "https://cdn.modrinth.com/download/good.jar"
                    },
                    Hashes = new FileHashes { Sha512 = sha },
                    FileSize = modBytes.Length
                }
            });

        await _importer.ImportAsync(packPath, "FailoverInst");

        File.Exists(Path.Combine(instanceDir, "mods", "mod.jar")).Should().BeTrue();
        attemptCount.Should().Be(1);
    }

    [Fact]
    public async Task Import_AllMirrorsFail_ThrowsAndRollsBack()
    {
        var instanceDir = Path.Combine(_tempRoot, "instances", "AllFailInst");
        Directory.CreateDirectory(instanceDir);

        var instance = new MinecraftInstance
        {
            Id = "inst-all-fail",
            Name = "AllFailInst",
            MinecraftVersion = "1.21.1",
            LoaderType = "fabric",
            Path = instanceDir,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _instanceManagerMock
            .Setup(m => m.CreateFabricAsync("AllFailInst", "1.21.1", "0.15.11", It.IsAny<CancellationToken>()))
            .ReturnsAsync(instance);

        _http.Map(HttpMethod.Get, "/download/mod.jar", _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var packPath = CreateMrpack(
            files: new List<MrpackFile>
            {
                new()
                {
                    Path = "mods/mod.jar",
                    Downloads = new List<string>
                    {
                        "https://cdn.modrinth.com/download/mod.jar",
                        "https://cdn.modrinth.com/download/mod.jar"
                    }
                }
            });

        var act = () => _importer.ImportAsync(packPath, "AllFailInst");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*all mirrors*");

        _instanceManagerMock.Verify(m => m.DeleteAsync("inst-all-fail", true, CancellationToken.None), Times.Once);
    }

    private string CreateMrpack(
        string name = "Test Pack",
        int formatVersion = 1,
        string game = "minecraft",
        Dictionary<string, string>? dependencies = null,
        List<MrpackFile>? files = null,
        Dictionary<string, string>? overrides = null,
        Dictionary<string, string>? clientOverrides = null,
        string? fileName = null)
    {
        var zipPath = Path.Combine(_tempRoot, fileName ?? $"{Guid.NewGuid():N}.mrpack");
        using (var zipFile = File.Open(zipPath, FileMode.Create))
        using (var archive = new ZipArchive(zipFile, ZipArchiveMode.Create))
        {
            var index = new MrpackIndex
            {
                FormatVersion = formatVersion,
                Game = game,
                VersionId = "1.0.0",
                Name = name,
                Summary = "Test Summary",
                Files = files ?? new List<MrpackFile>(),
                Dependencies = dependencies ?? new Dictionary<string, string>
                {
                    ["minecraft"] = "1.21.1",
                    ["fabric-loader"] = "0.15.11"
                }
            };

            var indexEntry = archive.CreateEntry("modrinth.index.json");
            using (var writer = new StreamWriter(indexEntry.Open()))
            {
                writer.Write(JsonSerializer.Serialize(index));
            }

            if (overrides != null)
            {
                foreach (var (relPath, content) in overrides)
                {
                    var entry = archive.CreateEntry($"overrides/{relPath}");
                    using var writer = new StreamWriter(entry.Open());
                    writer.Write(content);
                }
            }

            if (clientOverrides != null)
            {
                foreach (var (relPath, content) in clientOverrides)
                {
                    var entry = archive.CreateEntry($"client-overrides/{relPath}");
                    using var writer = new StreamWriter(entry.Open());
                    writer.Write(content);
                }
            }
        }

        return zipPath;
    }
}
