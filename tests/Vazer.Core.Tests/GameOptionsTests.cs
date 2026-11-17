using FluentAssertions;
using Vazer.Core.GameOptions;
using Xunit;

namespace Vazer.Core.Tests;

using GameOptions = Vazer.Core.GameOptions.GameOptions;

public class GameOptionsTests : IDisposable
{
    private readonly string _tempDir;

    public GameOptionsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "Vaz_GameOptionsTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public void Parse_ValidOptionsText_ShouldMapAllFieldsCorrectly()
    {
        var text = """
            # Minecraft options
            renderDistance:16
            simulationDistance:14
            maxFps:144
            graphicsMode:fast
            renderClouds:false
            guiScale:3
            gamma:0.8
            enableVsync:true
            entityShadows:false
            soundCategory_master:0.9
            soundCategory_music:0.2
            soundCategory_weather:0.6
            soundCategory_block:0.7
            soundCategory_hostile:0.85
            soundCategory_player:0.95
            soundCategory_ambient:0.4
            mouseSensitivity:0.45
            invertYMouse:true
            autoJump:true
            sneakToggle:true
            sprintToggle:false
            fov:85.5
            key_key.jump:key.keyboard.space
            mod_custom_setting:hello_world
            """;

        var opts = GameOptionsParser.Parse(text);

        opts.RenderDistance.Should().Be(16);
        opts.SimulationDistance.Should().Be(14);
        opts.MaxFps.Should().Be(144);
        opts.GraphicsMode.Should().Be("fast");
        opts.RenderClouds.Should().BeFalse();
        opts.GuiScale.Should().Be(3);
        opts.Gamma.Should().BeApproximately(0.8, 0.001);
        opts.EnableVsync.Should().BeTrue();
        opts.EntityShadows.Should().BeFalse();

        opts.SoundCategoryMaster.Should().BeApproximately(0.9, 0.001);
        opts.SoundCategoryMusic.Should().BeApproximately(0.2, 0.001);
        opts.SoundCategoryWeather.Should().BeApproximately(0.6, 0.001);
        opts.SoundCategoryBlock.Should().BeApproximately(0.7, 0.001);
        opts.SoundCategoryHostile.Should().BeApproximately(0.85, 0.001);
        opts.SoundCategoryPlayer.Should().BeApproximately(0.95, 0.001);
        opts.SoundCategoryAmbient.Should().BeApproximately(0.4, 0.001);

        opts.MouseSensitivity.Should().BeApproximately(0.45, 0.001);
        opts.InvertYMouse.Should().BeTrue();
        opts.AutoJump.Should().BeTrue();
        opts.SneakToggle.Should().BeTrue();
        opts.SprintToggle.Should().BeFalse();
        opts.Fov.Should().BeApproximately(85.5, 0.001);

        opts.CustomEntries.Should().ContainKey("key_key.jump").WhoseValue.Should().Be("key.keyboard.space");
        opts.CustomEntries.Should().ContainKey("mod_custom_setting").WhoseValue.Should().Be("hello_world");
    }

    [Fact]
    public void Parse_MinecraftStandardNormalizedFov_ShouldConvertToDegrees()
    {
        // 0.0 -> 70 deg (normal)
        var opts = GameOptionsParser.Parse("fov:0.0");
        opts.Fov.Should().Be(70.0);

        // 0.5 -> 90 deg
        opts = GameOptionsParser.Parse("fov:0.5");
        opts.Fov.Should().Be(90.0);

        // 1.0 -> 110 deg (Quake Pro)
        opts = GameOptionsParser.Parse("fov:1.0");
        opts.Fov.Should().Be(110.0);

        // -1.0 -> 30 deg
        opts = GameOptionsParser.Parse("fov:-1.0");
        opts.Fov.Should().Be(30.0);

        // Fallback for direct degrees
        opts = GameOptionsParser.Parse("fov:85.0");
        opts.Fov.Should().Be(85.0);

        // Clamp bounds
        opts = GameOptionsParser.Parse("fov:-5.0");
        opts.Fov.Should().Be(30.0);

        opts = GameOptionsParser.Parse("fov:180.0");
        opts.Fov.Should().Be(110.0);
    }

    [Fact]
    public void Parse_MinecraftStandardToggleCrouchAndSprint_ShouldMapCorrectly()
    {
        var text = """
            toggleCrouch:true
            toggleSprint:true
            """;
        var opts = GameOptionsParser.Parse(text);
        opts.SneakToggle.Should().BeTrue();
        opts.SprintToggle.Should().BeTrue();

        text = """
            togglecrouch:false
            togglesprint:false
            """;
        opts = GameOptionsParser.Parse(text);
        opts.SneakToggle.Should().BeFalse();
        opts.SprintToggle.Should().BeFalse();
    }

    [Fact]
    public void Serialize_ShouldOutputStandardMinecraftFormat()
    {
        var opts = new GameOptions
        {
            GraphicsMode = "fast",
            Fov = 70.0,
            SneakToggle = true,
            SprintToggle = false
        };

        var text = GameOptionsParser.Serialize(opts);
        text.Should().Contain("graphicsMode:0");
        text.Should().Contain("fov:0.0");
        text.Should().Contain("toggleCrouch:true");
        text.Should().Contain("toggleSprint:false");

        opts.GraphicsMode = "fabulous";
        opts.Fov = 90.0;
        text = GameOptionsParser.Serialize(opts);
        text.Should().Contain("graphicsMode:2");
        text.Should().Contain("fov:0.5");
    }

    [Fact]
    public void ApplyTo_ShouldOnlyOverrideGraphicsFields_AndPreserveAudioControlsCustom()
    {
        var current = new GameOptions
        {
            RenderDistance = 16,
            SimulationDistance = 12,
            MaxFps = 200,
            GraphicsMode = "fancy",
            RenderClouds = true,
            EnableVsync = true,
            EntityShadows = true,
            SoundCategoryMaster = 0.25,
            SoundCategoryMusic = 0.15,
            MouseSensitivity = 0.88,
            Fov = 95.0,
            SneakToggle = true,
            SprintToggle = true,
            CustomEntries = new Dictionary<string, string>
            {
                ["my_key"] = "my_val"
            }
        };

        var preset = GameOptionsPresets.Performance;
        preset.ApplyTo(current);

        // Graphics overridden
        current.RenderDistance.Should().Be(preset.RenderDistance);
        current.SimulationDistance.Should().Be(preset.SimulationDistance);
        current.MaxFps.Should().Be(preset.MaxFps);
        current.GraphicsMode.Should().Be(preset.GraphicsMode);
        current.RenderClouds.Should().Be(preset.RenderClouds);
        current.EnableVsync.Should().Be(preset.EnableVsync);
        current.EntityShadows.Should().Be(preset.EntityShadows);

        // Preserved audio, controls, custom
        current.SoundCategoryMaster.Should().Be(0.25);
        current.SoundCategoryMusic.Should().Be(0.15);
        current.MouseSensitivity.Should().Be(0.88);
        current.Fov.Should().Be(95.0);
        current.SneakToggle.Should().BeTrue();
        current.SprintToggle.Should().BeTrue();
        current.CustomEntries.Should().ContainKey("my_key").WhoseValue.Should().Be("my_val");
    }

    [Fact]
    public void Parse_GraphicsModeNumeric_ShouldMapToStandardString()
    {
        var text = """
            graphicsMode:0
            """;
        var opts = GameOptionsParser.Parse(text);
        opts.GraphicsMode.Should().Be("fast");

        text = "graphicsMode:1";
        opts = GameOptionsParser.Parse(text);
        opts.GraphicsMode.Should().Be("fancy");

        text = "graphicsMode:2";
        opts = GameOptionsParser.Parse(text);
        opts.GraphicsMode.Should().Be("fabulous");
    }

    [Fact]
    public void RoundTrip_ParseSerializeParse_PreservesValuesAndCustomEntries()
    {
        var initial = new GameOptions
        {
            RenderDistance = 8,
            SimulationDistance = 8,
            MaxFps = 60,
            GraphicsMode = "fast",
            RenderClouds = false,
            GuiScale = 2,
            Gamma = 0.5,
            EnableVsync = true,
            EntityShadows = false,
            SoundCategoryMaster = 0.8,
            SoundCategoryMusic = 0.0,
            SoundCategoryWeather = 0.5,
            SoundCategoryBlock = 1.0,
            SoundCategoryHostile = 0.7,
            SoundCategoryPlayer = 0.9,
            SoundCategoryAmbient = 0.3,
            MouseSensitivity = 0.65,
            InvertYMouse = false,
            AutoJump = false,
            SneakToggle = true,
            SprintToggle = false,
            Fov = 90.0,
            CustomEntries = new Dictionary<string, string>
            {
                ["key_key.attack"] = "key.mouse.left",
                ["customModKey"] = "customValue"
            }
        };

        var serialized = GameOptionsParser.Serialize(initial);
        var parsed = GameOptionsParser.Parse(serialized);

        parsed.RenderDistance.Should().Be(initial.RenderDistance);
        parsed.SimulationDistance.Should().Be(initial.SimulationDistance);
        parsed.MaxFps.Should().Be(initial.MaxFps);
        parsed.GraphicsMode.Should().Be(initial.GraphicsMode);
        parsed.RenderClouds.Should().Be(initial.RenderClouds);
        parsed.GuiScale.Should().Be(initial.GuiScale);
        parsed.Gamma.Should().BeApproximately(initial.Gamma, 0.001);
        parsed.EnableVsync.Should().Be(initial.EnableVsync);
        parsed.EntityShadows.Should().Be(initial.EntityShadows);

        parsed.SoundCategoryMaster.Should().BeApproximately(initial.SoundCategoryMaster, 0.001);
        parsed.SoundCategoryMusic.Should().BeApproximately(initial.SoundCategoryMusic, 0.001);
        parsed.SoundCategoryWeather.Should().BeApproximately(initial.SoundCategoryWeather, 0.001);
        parsed.SoundCategoryBlock.Should().BeApproximately(initial.SoundCategoryBlock, 0.001);
        parsed.SoundCategoryHostile.Should().BeApproximately(initial.SoundCategoryHostile, 0.001);
        parsed.SoundCategoryPlayer.Should().BeApproximately(initial.SoundCategoryPlayer, 0.001);
        parsed.SoundCategoryAmbient.Should().BeApproximately(initial.SoundCategoryAmbient, 0.001);

        parsed.MouseSensitivity.Should().BeApproximately(initial.MouseSensitivity, 0.001);
        parsed.InvertYMouse.Should().Be(initial.InvertYMouse);
        parsed.AutoJump.Should().Be(initial.AutoJump);
        parsed.SneakToggle.Should().Be(initial.SneakToggle);
        parsed.SprintToggle.Should().Be(initial.SprintToggle);
        parsed.Fov.Should().BeApproximately(initial.Fov, 0.001);

        parsed.CustomEntries.Should().ContainKey("key_key.attack").WhoseValue.Should().Be("key.mouse.left");
        parsed.CustomEntries.Should().ContainKey("customModKey").WhoseValue.Should().Be("customValue");
    }

    [Fact]
    public void Presets_VerifyConfigurationsAndLookup()
    {
        var perf = GameOptionsPresets.Performance;
        perf.RenderDistance.Should().Be(6);
        perf.SimulationDistance.Should().Be(6);
        perf.MaxFps.Should().Be(260);
        perf.GraphicsMode.Should().Be("fast");
        perf.RenderClouds.Should().BeFalse();
        perf.EnableVsync.Should().BeFalse();
        perf.EntityShadows.Should().BeFalse();

        var balanced = GameOptionsPresets.Balanced;
        balanced.RenderDistance.Should().Be(12);
        balanced.SimulationDistance.Should().Be(10);
        balanced.MaxFps.Should().Be(120);
        balanced.GraphicsMode.Should().Be("fancy");
        balanced.RenderClouds.Should().BeTrue();
        balanced.EnableVsync.Should().BeTrue();
        balanced.EntityShadows.Should().BeTrue();

        var hq = GameOptionsPresets.HighQuality;
        hq.RenderDistance.Should().Be(20);
        hq.SimulationDistance.Should().Be(16);
        hq.MaxFps.Should().Be(144);
        hq.GraphicsMode.Should().Be("fabulous");
        hq.RenderClouds.Should().BeTrue();
        hq.EnableVsync.Should().BeTrue();
        hq.EntityShadows.Should().BeTrue();

        GameOptionsPresets.GetByName("performance").Should().NotBeNull();
        GameOptionsPresets.GetByName("Tối ưu FPS").Should().NotBeNull();
        GameOptionsPresets.GetByName("balanced").Should().NotBeNull();
        GameOptionsPresets.GetByName("Cân bằng").Should().NotBeNull();
        GameOptionsPresets.GetByName("highquality").Should().NotBeNull();
        GameOptionsPresets.GetByName("Đồ họa cao").Should().NotBeNull();
        GameOptionsPresets.GetByName("invalid_name").Should().BeNull();
    }

    [Fact]
    public async Task Service_FileDoesNotExist_ShouldReturnDefaultGameOptions()
    {
        var service = new GameOptionsService();
        var opts = await service.LoadAsync(_tempDir);

        opts.Should().NotBeNull();
        opts.RenderDistance.Should().Be(12);
        opts.MaxFps.Should().Be(120);
        opts.GraphicsMode.Should().Be("fancy");
    }

    [Fact]
    public async Task Service_SaveAndLoad_ShouldPersistFileOnDisk()
    {
        var service = new GameOptionsService();
        var opts = new GameOptions
        {
            RenderDistance = 18,
            MaxFps = 240,
            GraphicsMode = "fast",
            RenderClouds = false
        };
        opts.CustomEntries["custom_test"] = "active";

        await service.SaveAsync(_tempDir, opts);

        var filePath = Path.Combine(_tempDir, "options.txt");
        File.Exists(filePath).Should().BeTrue();

        var loaded = await service.LoadAsync(_tempDir);
        loaded.RenderDistance.Should().Be(18);
        loaded.MaxFps.Should().Be(240);
        loaded.GraphicsMode.Should().Be("fast");
        loaded.RenderClouds.Should().BeFalse();
        loaded.CustomEntries.Should().ContainKey("custom_test").WhoseValue.Should().Be("active");

        var raw = await service.LoadRawAsync(_tempDir);
        raw.Should().Contain("renderDistance:18");
        raw.Should().Contain("custom_test:active");
    }

    [Fact]
    public async Task Service_SaveRawAsync_ShouldPersistRawString()
    {
        var service = new GameOptionsService();
        var rawContent = "renderDistance:8\nmaxFps:90\ncustomEntry:123";

        await service.SaveRawAsync(_tempDir, rawContent);

        var loadedRaw = await service.LoadRawAsync(_tempDir);
        loadedRaw.Trim().Should().Be(rawContent.Trim());

        var loaded = await service.LoadAsync(_tempDir);
        loaded.RenderDistance.Should().Be(8);
        loaded.MaxFps.Should().Be(90);
        loaded.CustomEntries.Should().ContainKey("customEntry").WhoseValue.Should().Be("123");
    }
}
