using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vazer.Core;
using Vazer.Core.Settings;

namespace Vazer.Core.Tests;

/// <summary>
/// SettingsService: defaults, roundtrip, clamp RAM, JSON hỏng.
/// Dùng temp directory — không đụng %APPDATA% thật.
/// </summary>
public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly SettingsService _service;

    public SettingsServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "vaz-settings-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _service = CreateService(_tempRoot);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, true); } catch { /* ignore */ }
    }

    private static SettingsService CreateService(string root)
    {
        var options = Options.Create(new MinecraftOptions { RootPath = root });
        return new SettingsService(options, NullLogger<SettingsService>.Instance);
    }

    private string SettingsPath => Path.Combine(_tempRoot, "settings.json");

    private void WriteRawJson(string json) => File.WriteAllText(SettingsPath, json);

    [Fact]
    public async Task Load_WhenFileMissing_ReturnsWebDefaults()
    {
        var settings = await _service.LoadAsync();

        settings.Language.Should().Be("vi");
        settings.LaunchAction.Should().Be("hide");
        settings.CloseToTray.Should().BeTrue();
        settings.AutoCheckUpdates.Should().BeTrue();
        settings.JavaPath.Should().BeEmpty();
        settings.MinRamMb.Should().Be(2048);
        settings.RamMb.Should().Be(4096);
        settings.JvmPreset.Should().Be("standard");
        settings.JvmArgs.Should().Be(GlobalSettings.DefaultJvmArgs);
        settings.WindowWidth.Should().Be(1280);
        settings.WindowHeight.Should().Be(720);
        settings.StartFullscreen.Should().BeFalse();
        settings.Theme.Should().Be("dark");
        settings.AccentColor.Should().Be("emerald");
        settings.ReduceMotion.Should().BeFalse();
        settings.UiSoundEnabled.Should().BeTrue();
        settings.DownloadThreads.Should().Be(8);
        settings.DownloadMirror.Should().Be("cloudflare");
        settings.BandwidthLimit.Should().Be("unlimited");
        settings.DiscordRpc.Should().BeTrue();
        settings.DiscordStatus.Should().Be(GlobalSettings.DefaultDiscordStatus);
        settings.DiscordShowInstance.Should().BeTrue();
        settings.AnonymousTelemetry.Should().BeTrue();
        settings.CacheSizeMb.Should().Be(248);

        settings.DataPath.Should().Be(_tempRoot);
        settings.BackupPath.Should().Be(Path.Combine(_tempRoot, "backups"));
        File.Exists(SettingsPath).Should().BeFalse("Load không được tự ghi file");
    }

    [Fact]
    public async Task Save_Then_Load_RoundTripsEveryField()
    {
        var original = new GlobalSettings
        {
            Language = "en",
            LaunchAction = "minimize",
            CloseToTray = false,
            AutoCheckUpdates = false,
            JavaPath = @"C:\Program Files\Eclipse Adoptium\jdk-21\bin\javaw.exe",
            MinRamMb = 3072,
            RamMb = 8192,
            JvmPreset = "aikar",
            JvmArgs = "-XX:+UseG1GC",
            WindowWidth = 1920,
            WindowHeight = 1080,
            StartFullscreen = true,
            Theme = "obsidian",
            AccentColor = "violet",
            ReduceMotion = true,
            UiSoundEnabled = false,
            DownloadThreads = 16,
            DownloadMirror = "modrinth",
            BandwidthLimit = "25mb",
            DiscordRpc = false,
            DiscordStatus = "Đang đào kim cương",
            DiscordShowInstance = false,
            AnonymousTelemetry = false,
            DataPath = @"C:\vaz-data",
            BackupPath = @"D:\vaz-backups",
            CacheSizeMb = 512
        };

        var saved = await _service.SaveAsync(original);
        var loaded = await _service.LoadAsync();

        loaded.Should().BeEquivalentTo(saved);
        loaded.Should().BeEquivalentTo(original);
    }

    [Fact]
    public async Task Load_ClampsRamOutsideRange()
    {
        WriteRawJson("""{ "ramMb": 999999, "minRamMb": 20000 }""");

        var settings = await _service.LoadAsync();

        settings.RamMb.Should().Be(16384);
        settings.MinRamMb.Should().Be(16384);

        WriteRawJson("""{ "ramMb": 512, "minRamMb": 256 }""");

        var lower = await _service.LoadAsync();

        lower.RamMb.Should().Be(1024);
        lower.MinRamMb.Should().Be(1024);
    }

    [Fact]
    public async Task Load_ClampsMinRamToMaxRam_WhenMinExceedsMax()
    {
        WriteRawJson("""{ "ramMb": 4096, "minRamMb": 8192 }""");

        var settings = await _service.LoadAsync();

        settings.RamMb.Should().Be(4096);
        settings.MinRamMb.Should().Be(4096);
    }

    [Fact]
    public async Task Load_CorruptJson_ReturnsDefaultsWithoutThrowing()
    {
        WriteRawJson("{ this is not json ");

        var settings = await _service.LoadAsync();

        settings.RamMb.Should().Be(4096);
        settings.Theme.Should().Be("dark");
        settings.DataPath.Should().Be(_tempRoot);
    }

    [Fact]
    public async Task Load_UnknownEnumValues_FallBackToDefaults()
    {
        WriteRawJson("""
            {
              "language": "fr",
              "launchAction": "explode",
              "theme": "neon",
              "accentColor": "pink",
              "downloadMirror": "ftp",
              "bandwidthLimit": "999",
              "jvmPreset": "g1",
              "downloadThreads": 3
            }
            """);

        var settings = await _service.LoadAsync();

        settings.Language.Should().Be("vi");
        settings.LaunchAction.Should().Be("hide");
        settings.Theme.Should().Be("dark");
        settings.AccentColor.Should().Be("emerald");
        settings.DownloadMirror.Should().Be("cloudflare");
        settings.BandwidthLimit.Should().Be("unlimited");
        settings.JvmPreset.Should().Be("standard");
        settings.DownloadThreads.Should().Be(8);
    }

    [Fact]
    public async Task Load_LegacyMirrorAuto_FallsBackToCloudflare()
    {
        // P1: web bỏ mirror "auto" (4 lựa chọn, mặc định cloudflare) → settings cũ phải migrate.
        WriteRawJson("""{ "downloadMirror": "auto" }""");

        var settings = await _service.LoadAsync();

        settings.DownloadMirror.Should().Be("cloudflare");
    }

    [Fact]
    public async Task Load_LegacyShenandoahPreset_FallsBackToStandard()
    {
        // P1: preset JVM còn 4 lựa chọn (standard/aikar/zgc/custom) → shenandoah cũ về standard.
        WriteRawJson("""{ "jvmPreset": "shenandoah", "jvmArgs": "-XX:+UseShenandoahGC" }""");

        var settings = await _service.LoadAsync();

        settings.JvmPreset.Should().Be("standard");
    }

    [Fact]
    public async Task Load_RoundsRamTo512Step_AfterClamping()
    {
        WriteRawJson("""{ "ramMb": 4300, "minRamMb": 3000 }""");

        var settings = await _service.LoadAsync();

        settings.RamMb.Should().Be(4096, "4300 gần 4096 hơn 4608");
        settings.MinRamMb.Should().Be(3072);

        WriteRawJson("""{ "ramMb": 16350, "minRamMb": 999 }""");

        var upper = await _service.LoadAsync();

        upper.RamMb.Should().Be(16384, "clamp trần trước khi làm tròn");
        upper.MinRamMb.Should().Be(1024);
        (upper.RamMb % 512).Should().Be(0);
        (upper.MinRamMb % 512).Should().Be(0);
    }

    [Fact]
    public async Task Save_RoundsRamTo512Step()
    {
        var saved = await _service.SaveAsync(new GlobalSettings
        {
            RamMb = 4200,
            MinRamMb = 1900
        });

        saved.RamMb.Should().Be(4096);
        saved.MinRamMb.Should().Be(2048);
        (saved.RamMb % 512).Should().Be(0);
        (saved.MinRamMb % 512).Should().Be(0);
    }

    [Fact]
    public async Task Load_ClampsWindowSizeAndCache()
    {
        WriteRawJson("""{ "windowWidth": 12, "windowHeight": 99999, "cacheSizeMb": -5 }""");

        var settings = await _service.LoadAsync();

        settings.WindowWidth.Should().Be(640);
        settings.WindowHeight.Should().Be(2160);
        settings.CacheSizeMb.Should().Be(0);

        WriteRawJson("""{ "windowWidth": 0, "windowHeight": 0 }""");

        var zero = await _service.LoadAsync();
        zero.WindowWidth.Should().Be(1280);
        zero.WindowHeight.Should().Be(720);
    }

    [Fact]
    public async Task Save_ClampsValuesAndReturnsNormalizedCopy()
    {
        var saved = await _service.SaveAsync(new GlobalSettings
        {
            RamMb = 100,
            MinRamMb = 99999,
            WindowWidth = 10,
            WindowHeight = 10,
            DownloadThreads = 7
        });

        saved.RamMb.Should().Be(1024);
        saved.MinRamMb.Should().Be(1024);
        saved.WindowWidth.Should().Be(640);
        saved.WindowHeight.Should().Be(480);
        saved.DownloadThreads.Should().Be(8);
    }

    [Fact]
    public async Task Save_IsAtomic_NoTmpLeftBehind()
    {
        await _service.SaveAsync(new GlobalSettings());

        File.Exists(SettingsPath).Should().BeTrue();
        Directory.GetFiles(_tempRoot, "*.tmp").Should().BeEmpty();

        var json = await File.ReadAllTextAsync(SettingsPath);
        json.Should().Contain("\"ramMb\": 4096", "file dùng camelCase giống web state");
    }

    [Fact]
    public async Task Save_CreatesRootDirectoryWhenMissing()
    {
        var nestedRoot = Path.Combine(_tempRoot, "nested", "root");
        var service = CreateService(nestedRoot);

        await service.SaveAsync(new GlobalSettings());

        File.Exists(Path.Combine(nestedRoot, "settings.json")).Should().BeTrue();
        service.SettingsFilePath.Should().Be(Path.Combine(nestedRoot, "settings.json"));
    }

    [Fact]
    public void CreateDefault_UsesConfiguredRootPath()
    {
        var defaults = _service.CreateDefault();

        defaults.DataPath.Should().Be(_tempRoot);
        defaults.BackupPath.Should().Be(Path.Combine(_tempRoot, "backups"));
        defaults.RamMb.Should().Be(4096);
        defaults.Theme.Should().Be("dark");
    }

    [Fact]
    public async Task Save_Null_ThrowsArgumentNullException()
    {
        var act = async () => await _service.SaveAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
