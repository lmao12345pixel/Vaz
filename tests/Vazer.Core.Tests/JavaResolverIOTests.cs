using System.IO.Compression;
using System.Security.Cryptography;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vazer.Core;
using Vazer.Core.Abstractions;
using Vazer.Core.Java;

namespace Vazer.Core.Tests;

/// <summary>
/// Phase 1.7 TASK3: EnsureAsync I/O paths with a fake <see cref="IAdoptiumClient"/>.
/// Real network is never touched. Probing a downloaded JDK needs a real JVM,
/// so the happy path reuses a system java.exe when present and documents the
/// fallback (cleanup + throw) otherwise.
/// </summary>
public sealed class JavaResolverIOTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly MinecraftOptions _opts;
    private readonly Xunit.Abstractions.ITestOutputHelper _output;

    public JavaResolverIOTests(Xunit.Abstractions.ITestOutputHelper output)
    {
        _output = output;
        _tempRoot = Path.Combine(Path.GetTempPath(), "upco-mc-javaio-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _opts = new MinecraftOptions { RootPath = _tempRoot };
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, true); } catch { }
    }

    private JavaResolver CreateResolver(IAdoptiumClient adoptium)
    {
        return new JavaResolver(
            adoptium,
            NullLogger<JavaResolver>.Instance,
            Options.Create(_opts));
    }

    /// <summary>Locates a real java.exe on this machine (PATH + common dirs).</summary>
    private static string? FindSystemJava()
    {
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim(), "java.exe");
                if (File.Exists(candidate)) return candidate;
            }
            catch { }
        }

        foreach (var root in new[]
        {
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Eclipse Adoptium"),
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Java"),
        })
        {
            try
            {
                if (!Directory.Exists(root)) continue;
                var found = Directory.GetFiles(root, "java.exe", SearchOption.AllDirectories).FirstOrDefault();
                if (found != null) return found;
            }
            catch { }
        }

        return null;
    }

    /// <summary>
    /// java.exe alone cannot run without its JDK tree (jvm.dll, jimage in
    /// sibling lib/). So working fixtures copy the whole JDK root.
    /// </summary>
    private static string? FindJdkRoot()
    {
        var java = FindSystemJava();
        if (java == null) return null;
        var bin = Path.GetDirectoryName(java);
        if (bin == null) return null;
        var root = Path.GetDirectoryName(bin);
        return root != null && Directory.Exists(root) ? root : null;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        }
        foreach (var dir in Directory.GetDirectories(source))
        {
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
        }
    }

    /// <summary>
    /// Builds a fake Adoptium zip: jdk-21.0.0+7/ tree. With a real JDK root the
    /// bundled probe succeeds; otherwise a dummy java.exe exercises the
    /// probe-failure cleanup path.
    /// </summary>
    private static string BuildFakeJdkZip(string dir, string? jdkRoot)
    {
        var zipPath = Path.Combine(dir, "fake-jdk.zip");
        using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            if (jdkRoot != null)
            {
                foreach (var file in Directory.GetFiles(jdkRoot, "*", SearchOption.AllDirectories))
                {
                    var rel = Path.GetRelativePath(jdkRoot, file).Replace(Path.DirectorySeparatorChar, '/');
                    zip.CreateEntryFromFile(file, "jdk-21.0.0+7/" + rel);
                }
            }
            else
            {
                var entry = zip.CreateEntry("jdk-21.0.0+7/bin/java.exe");
                using var w = new StreamWriter(entry.Open());
                w.Write("not-a-real-java");
            }
        }
        return zipPath;
    }

    private static string Sha256Of(string path)
    {
        using var sha = SHA256.Create();
        using var s = File.OpenRead(path);
        return Convert.ToHexString(sha.ComputeHash(s)).ToLowerInvariant();
    }

    [Fact]
    public async Task EnsureAsync_BundledPresent_Returns_Without_Adoptium_Call()
    {
        var jdkRoot = FindJdkRoot();
        if (jdkRoot == null)
        {
            _output.WriteLine("SKIP real-JVM path: no java.exe on PATH.");
            return;
        }

        // Probe the system JDK first to learn its real major version.
        var probeResolver = CreateResolver(new ThrowingAdoptium());
        var sysJava = Path.Combine(jdkRoot, "bin", "java.exe");
        var probed = await probeResolver.TryProbeAsync(sysJava);
        if (probed == null)
        {
            _output.WriteLine("SKIP real-JVM path: system java.exe failed probe.");
            return;
        }

        CopyDirectory(jdkRoot, Path.Combine(_tempRoot, "java", $"temurin-{probed.MajorVersion}-x64"));

        var adoptium = new ThrowingAdoptium();
        var resolver = CreateResolver(adoptium);

        var result = await resolver.EnsureAsync(probed.MajorVersion);

        result.Source.Should().Be("bundled");
        result.MajorVersion.Should().Be(probed.MajorVersion);
        adoptium.Calls.Should().Be(0);
    }

    [Fact]
    public async Task EnsureAsync_FullFlow_Extracts_And_Cleans_Up()
    {
        var jdkRoot = FindJdkRoot();
        string? probedMajor = null;
        if (jdkRoot != null)
        {
            var probeResolver = CreateResolver(new ThrowingAdoptium());
            var probed = await probeResolver.TryProbeAsync(Path.Combine(jdkRoot, "bin", "java.exe"));
            // Fixture zip is labelled 21.x: only a real Java 21 can pass the final probe.
            if (probed?.MajorVersion == 21) probedMajor = "21";
        }

        var zipPath = BuildFakeJdkZip(_tempRoot, probedMajor != null ? jdkRoot : null);
        var adoptium = new FileAdoptium(zipPath, Sha256Of(zipPath));
        var resolver = CreateResolver(adoptium);

        if (probedMajor == null)
        {
            _output.WriteLine("No Java 21 on this machine: exercising probe-failure cleanup path.");
            // Without a Java 21 the final probe must fail: folder cleaned + clear throw.
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => resolver.EnsureAsync(21));
            ex.Message.Should().Contain("failed probe");
            Directory.Exists(Path.Combine(_tempRoot, "java", "temurin-21-x64")).Should().BeFalse();
            return;
        }

        var result = await resolver.EnsureAsync(21);

        result.Source.Should().Be("adoptium");
        result.MajorVersion.Should().Be(21);
        Directory.Exists(Path.Combine(_tempRoot, "java", "temurin-21-x64")).Should().BeTrue();

        // Atomic: no leftovers.
        var dlDir = Path.Combine(_tempRoot, "java", "downloads");
        if (Directory.Exists(dlDir))
        {
            Directory.GetFiles(dlDir, "*.partial").Should().BeEmpty();
            Directory.GetFiles(dlDir, "*.zip").Should().BeEmpty();
        }
        Directory.GetDirectories(Path.Combine(_tempRoot, "java"), "*.tmp").Should().BeEmpty();
    }

    [Fact]
    public async Task EnsureAsync_ChecksumMismatch_Throws_And_Deletes_Partial()
    {
        var zipPath = BuildFakeJdkZip(_tempRoot, jdkRoot: null);
        var adoptium = new FileAdoptium(zipPath, new string('0', 64)); // wrong checksum
        var resolver = CreateResolver(adoptium);

        await Assert.ThrowsAsync<InvalidDataException>(() => resolver.EnsureAsync(21));

        var partial = Path.Combine(_tempRoot, "java", "downloads", "temurin-21.zip.partial");
        File.Exists(partial).Should().BeFalse();
    }

    [Fact]
    public async Task EnsureAsync_DownloadFails_Partial_Remains_Documenting_Resume_Behavior()
    {
        var adoptium = new FailingDownloadAdoptium();
        var resolver = CreateResolver(adoptium);

        await Assert.ThrowsAsync<IOException>(() => resolver.EnsureAsync(21));

        // Current behavior: failed download leaves .partial in place (resume on retry).
        var partial = Path.Combine(_tempRoot, "java", "downloads", "temurin-21.zip.partial");
        File.Exists(partial).Should().BeTrue();
    }

    [Fact]
    public async Task EnsureAsync_CorruptZip_Throws_And_Cleans_Tmp()
    {
        var garbagePath = Path.Combine(_tempRoot, "garbage.zip");
        await File.WriteAllBytesAsync(garbagePath, new byte[] { 1, 2, 3, 4, 5 });
        var adoptium = new FileAdoptium(garbagePath, Sha256Of(garbagePath));
        var resolver = CreateResolver(adoptium);

        // ZipFile.ExtractToDirectory throws InvalidDataException on garbage.
        await Assert.ThrowsAsync<InvalidDataException>(() => resolver.EnsureAsync(21));

        Directory.GetDirectories(Path.Combine(_tempRoot, "java"), "*.tmp").Should().BeEmpty();
    }

    private sealed class ThrowingAdoptium : IAdoptiumClient
    {
        public int Calls { get; private set; }
        public Task<AdoptiumAssetInfo> GetLatestJdkAsync(int major, CancellationToken ct = default)
        {
            Calls++;
            throw new InvalidOperationException("Adoptium must not be contacted when bundled Java is present.");
        }
        public Task DownloadFileAsync(string url, string destination, IProgress<InstallProgress>? progress, CancellationToken ct = default)
        {
            Calls++;
            throw new InvalidOperationException("Adoptium must not be contacted when bundled Java is present.");
        }
    }

    private sealed class FileAdoptium : IAdoptiumClient
    {
        private readonly string _sourceFile;
        private readonly string _checksum;

        public FileAdoptium(string sourceFile, string checksum)
        {
            _sourceFile = sourceFile;
            _checksum = checksum;
        }

        public Task<AdoptiumAssetInfo> GetLatestJdkAsync(int major, CancellationToken ct = default)
        {
            return Task.FromResult(new AdoptiumAssetInfo
            {
                DownloadUrl = "https://test.local/temurin.zip",
                Sha256Checksum = _checksum,
                SizeBytes = new FileInfo(_sourceFile).Length
            });
        }

        public Task DownloadFileAsync(string url, string destination, IProgress<InstallProgress>? progress, CancellationToken ct = default)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(_sourceFile, destination, true);
            progress?.Report(new InstallProgress { TotalBytes = 1, DownloadedBytes = 1, Status = "verifying" });
            return Task.CompletedTask;
        }
    }

    private sealed class FailingDownloadAdoptium : IAdoptiumClient
    {
        public Task<AdoptiumAssetInfo> GetLatestJdkAsync(int major, CancellationToken ct = default)
        {
            return Task.FromResult(new AdoptiumAssetInfo
            {
                DownloadUrl = "https://test.local/temurin.zip",
                Sha256Checksum = new string('0', 64),
                SizeBytes = 10
            });
        }

        public async Task DownloadFileAsync(string url, string destination, IProgress<InstallProgress>? progress, CancellationToken ct = default)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            await File.WriteAllBytesAsync(destination, new byte[] { 9, 9, 9 }, ct);
            throw new IOException("Simulated mid-download failure.");
        }
    }
}
