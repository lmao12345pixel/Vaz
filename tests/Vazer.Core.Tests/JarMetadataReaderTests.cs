using System.IO.Compression;
using System.Text;
using FluentAssertions;
using Vazer.Core.Mods;

namespace Vazer.Core.Tests;

public sealed class JarMetadataReaderTests : IDisposable
{
    private readonly string _tempRoot;

    public JarMetadataReaderTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "vazer-jarmeta-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
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
            // Ignore cleanup failure
        }
    }

    [Fact]
    public void Read_FabricJar_ReturnsBasicMetadata()
    {
        var json = """
            {
              "schemaVersion": 1,
              "id": "ironfurnaces",
              "version": "1.2.3",
              "name": "Iron Furnaces",
              "description": "Furnaces all the way",
              "authors": [ "Foo", { "name": "Bar" } ]
            }
            """;
        var jarPath = CreateJar(("fabric.mod.json", json));

        var meta = JarMetadataReader.Read(jarPath);

        meta.Should().NotBeNull();
        meta!.ModId.Should().Be("ironfurnaces");
        meta.Name.Should().Be("Iron Furnaces");
        meta.Version.Should().Be("1.2.3");
        meta.Loader.Should().Be("fabric");
        meta.Description.Should().Be("Furnaces all the way");
        meta.Authors.Should().BeEquivalentTo(new[] { "Foo", "Bar" });
    }

    [Fact]
    public void Read_FabricJarWithoutName_FallsBackToModId()
    {
        var json = """{ "id": "plainmod", "version": "0.1" }""";
        var jarPath = CreateJar(("fabric.mod.json", json));

        var meta = JarMetadataReader.Read(jarPath);

        meta.Should().NotBeNull();
        meta!.ModId.Should().Be("plainmod");
        meta.Name.Should().Be("plainmod");
        meta.Loader.Should().Be("fabric");
    }

    [Fact]
    public void Read_QuiltJar_UsesQuiltLoaderAndContributors()
    {
        var json = """
            {
              "schema_version": 1,
              "quilt_loader": {
                "id": "qmod",
                "version": "2.1.0",
                "metadata": {
                  "name": "Quilted Ones",
                  "description": "Runs on quilt",
                  "contributors": {
                    "Ann": "Owner",
                    "Bob": "Author"
                  }
                }
              }
            }
            """;
        var jarPath = CreateJar(("quilt.mod.json", json));

        var meta = JarMetadataReader.Read(jarPath);

        meta.Should().NotBeNull();
        meta!.ModId.Should().Be("qmod");
        meta.Name.Should().Be("Quilted Ones");
        meta.Version.Should().Be("2.1.0");
        meta.Loader.Should().Be("quilt");
        meta.Description.Should().Be("Runs on quilt");
        meta.Authors.Should().BeEquivalentTo(new[] { "Ann", "Bob" });
    }

    [Fact]
    public void Read_ForgeToml_TakesFirstModsBlock()
    {
        var toml = """
            modLoader="javafml"
            loaderVersion="[44,)"

            [[mods]]
            modId="ironfurnaces"
            version="1.2.3"
            displayName="Iron Furnaces"
            authors="Curs3d"
            description="Furnaces all the way"

            [[mods]]
            modId="secondmod"
            version="9.9.9"
            displayName="Should be ignored"
            """;
        var jarPath = CreateJar(("META-INF/mods.toml", toml));

        var meta = JarMetadataReader.Read(jarPath);

        meta.Should().NotBeNull();
        meta!.ModId.Should().Be("ironfurnaces");
        meta.Name.Should().Be("Iron Furnaces");
        meta.Version.Should().Be("1.2.3");
        meta.Loader.Should().Be("forge");
        meta.Description.Should().Be("Furnaces all the way");
        string.Join(",", meta.Authors).Should().Contain("Curs3d");
    }

    [Fact]
    public void Read_NeoForgeToml_ReturnsNeoForgeLoader()
    {
        var toml = """
            modLoader="javafml"

            [[mods]]
            modId="nemod"
            version="5.0.1"
            displayName="Neo Mod"
            """;
        var jarPath = CreateJar(("META-INF/neoforge.mods.toml", toml));

        var meta = JarMetadataReader.Read(jarPath);

        meta.Should().NotBeNull();
        meta!.ModId.Should().Be("nemod");
        meta.Version.Should().Be("5.0.1");
        meta.Loader.Should().Be("neoforge");
        meta.Name.Should().Be("Neo Mod");
    }

    [Fact]
    public void Read_FabricAuthorsObjectArray_PicksNameField()
    {
        var json = """
            {
              "id": "objauthors",
              "version": "1.0",
              "authors": [ { "name": "Alpha" }, { "name": "Beta" }, {} ]
            }
            """;
        var jarPath = CreateJar(("fabric.mod.json", json));

        var meta = JarMetadataReader.Read(jarPath);

        meta!.Authors.Should().BeEquivalentTo(new[] { "Alpha", "Beta" });
    }

    [Fact]
    public void Read_JarWithoutMetadata_ReturnsNull()
    {
        var jarPath = CreateJar(("some/random.txt", "nothing useful in here"));

        var meta = JarMetadataReader.Read(jarPath);

        meta.Should().BeNull();
    }

    [Fact]
    public void Read_CorruptZipBytes_ReturnsNull()
    {
        var path = Path.Combine(_tempRoot, "corrupt.jar");
        File.WriteAllBytes(path, Enumerable.Repeat((byte)'!', 256).ToArray());

        var meta = JarMetadataReader.Read(path);

        meta.Should().BeNull();
    }

    [Fact]
    public void Read_FromStream_MatchesFileRead()
    {
        var json = """{ "id": "streammod", "version": "3.3", "name": "Stream Mod" }""";
        var jarPath = CreateJar(("fabric.mod.json", json));

        using var stream = File.OpenRead(jarPath);
        var meta = JarMetadataReader.Read(stream);

        meta.Should().NotBeNull();
        meta!.ModId.Should().Be("streammod");
        meta.Version.Should().Be("3.3");
        meta.Name.Should().Be("Stream Mod");
    }

    private string CreateJar(params (string entryName, string content)[] entries)
    {
        var path = Path.Combine(_tempRoot, $"{Guid.NewGuid():N}.jar");
        using var zip = new ZipArchive(File.Create(path), ZipArchiveMode.Create);
        foreach (var (entryName, content) in entries)
        {
            var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write(content);
        }

        return path;
    }
}
