using System.Globalization;
using System.Text;

namespace Vazer.Core.GameOptions;

/// <summary>
/// Parser và Serializer cho file options.txt của Minecraft.
/// </summary>
public static class GameOptionsParser
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static GameOptions Parse(string text)
    {
        var options = new GameOptions();
        if (string.IsNullOrWhiteSpace(text))
        {
            return options;
        }

        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            var colonIdx = trimmed.IndexOf(':');
            if (colonIdx <= 0)
            {
                continue;
            }

            var key = trimmed[..colonIdx].Trim();
            var val = trimmed[(colonIdx + 1)..].Trim();

            switch (key.ToLowerInvariant())
            {
                case "renderdistance":
                    if (int.TryParse(val, NumberStyles.Integer, Inv, out var rd)) options.RenderDistance = rd;
                    break;
                case "simulationdistance":
                    if (int.TryParse(val, NumberStyles.Integer, Inv, out var sd)) options.SimulationDistance = sd;
                    break;
                case "maxfps":
                    if (int.TryParse(val, NumberStyles.Integer, Inv, out var fps)) options.MaxFps = fps;
                    break;
                case "graphicsmode":
                    options.GraphicsMode = val.ToLowerInvariant() switch
                    {
                        "0" or "fast" => "fast",
                        "1" or "fancy" => "fancy",
                        "2" or "fabulous" => "fabulous",
                        _ => val.ToLowerInvariant()
                    };
                    break;
                case "renderclouds":
                    if (bool.TryParse(val, out var rc)) options.RenderClouds = rc;
                    break;
                case "guiscale":
                    if (int.TryParse(val, NumberStyles.Integer, Inv, out var gs)) options.GuiScale = gs;
                    break;
                case "gamma":
                    if (double.TryParse(val, NumberStyles.Float, Inv, out var gamma)) options.Gamma = gamma;
                    break;
                case "enablevsync":
                    if (bool.TryParse(val, out var vsync)) options.EnableVsync = vsync;
                    break;
                case "entityshadows":
                    if (bool.TryParse(val, out var shadows)) options.EntityShadows = shadows;
                    break;
                case "soundcategory_master":
                    if (double.TryParse(val, NumberStyles.Float, Inv, out var sm)) options.SoundCategoryMaster = sm;
                    break;
                case "soundcategory_music":
                    if (double.TryParse(val, NumberStyles.Float, Inv, out var smu)) options.SoundCategoryMusic = smu;
                    break;
                case "soundcategory_weather":
                    if (double.TryParse(val, NumberStyles.Float, Inv, out var sw)) options.SoundCategoryWeather = sw;
                    break;
                case "soundcategory_block":
                    if (double.TryParse(val, NumberStyles.Float, Inv, out var sb)) options.SoundCategoryBlock = sb;
                    break;
                case "soundcategory_hostile":
                    if (double.TryParse(val, NumberStyles.Float, Inv, out var sh)) options.SoundCategoryHostile = sh;
                    break;
                case "soundcategory_player":
                    if (double.TryParse(val, NumberStyles.Float, Inv, out var sp)) options.SoundCategoryPlayer = sp;
                    break;
                case "soundcategory_ambient":
                    if (double.TryParse(val, NumberStyles.Float, Inv, out var sa)) options.SoundCategoryAmbient = sa;
                    break;
                case "mousesensitivity":
                    if (double.TryParse(val, NumberStyles.Float, Inv, out var ms)) options.MouseSensitivity = ms;
                    break;
                case "inverty_mouse" or "invertymouse":
                    if (bool.TryParse(val, out var iy)) options.InvertYMouse = iy;
                    break;
                case "autojump":
                    if (bool.TryParse(val, out var aj)) options.AutoJump = aj;
                    break;
                case "togglecrouch" or "sneaktoggle" or "togglesneak":
                    if (bool.TryParse(val, out var st)) options.SneakToggle = st;
                    break;
                case "togglesprint" or "sprinttoggle":
                    if (bool.TryParse(val, out var sprt)) options.SprintToggle = sprt;
                    break;
                case "fov":
                    if (double.TryParse(val, NumberStyles.Float, Inv, out var rawFov))
                    {
                        double degrees;
                        if (rawFov >= -1.0 && rawFov <= 1.0)
                        {
                            degrees = Math.Round(40.0 * rawFov + 70.0);
                        }
                        else
                        {
                            degrees = rawFov;
                        }
                        options.Fov = Math.Clamp(degrees, 30.0, 110.0);
                    }
                    break;
                default:
                    options.CustomEntries[key] = val;
                    break;
            }
        }

        return options;
    }

    public static string Serialize(GameOptions options)
    {
        var sb = new StringBuilder();

        // Graphics
        sb.AppendLine(Inv, $"renderDistance:{options.RenderDistance}");
        sb.AppendLine(Inv, $"simulationDistance:{options.SimulationDistance}");
        sb.AppendLine(Inv, $"maxFps:{options.MaxFps}");

        var graphicsModeVal = (options.GraphicsMode ?? "fancy").ToLowerInvariant() switch
        {
            "fast" or "0" => "0",
            "fabulous" or "2" => "2",
            _ => "1"
        };
        sb.AppendLine(Inv, $"graphicsMode:{graphicsModeVal}");

        sb.AppendLine(Inv, $"renderClouds:{options.RenderClouds.ToString().ToLowerInvariant()}");
        sb.AppendLine(Inv, $"guiScale:{options.GuiScale}");
        sb.AppendLine(Inv, $"gamma:{options.Gamma:0.0#}");
        sb.AppendLine(Inv, $"enableVsync:{options.EnableVsync.ToString().ToLowerInvariant()}");
        sb.AppendLine(Inv, $"entityShadows:{options.EntityShadows.ToString().ToLowerInvariant()}");

        // Audio
        sb.AppendLine(Inv, $"soundCategory_master:{options.SoundCategoryMaster:0.0#}");
        sb.AppendLine(Inv, $"soundCategory_music:{options.SoundCategoryMusic:0.0#}");
        sb.AppendLine(Inv, $"soundCategory_weather:{options.SoundCategoryWeather:0.0#}");
        sb.AppendLine(Inv, $"soundCategory_block:{options.SoundCategoryBlock:0.0#}");
        sb.AppendLine(Inv, $"soundCategory_hostile:{options.SoundCategoryHostile:0.0#}");
        sb.AppendLine(Inv, $"soundCategory_player:{options.SoundCategoryPlayer:0.0#}");
        sb.AppendLine(Inv, $"soundCategory_ambient:{options.SoundCategoryAmbient:0.0#}");

        // Controls
        sb.AppendLine(Inv, $"mouseSensitivity:{options.MouseSensitivity:0.0#}");
        sb.AppendLine(Inv, $"invertYMouse:{options.InvertYMouse.ToString().ToLowerInvariant()}");
        sb.AppendLine(Inv, $"autoJump:{options.AutoJump.ToString().ToLowerInvariant()}");
        sb.AppendLine(Inv, $"toggleCrouch:{options.SneakToggle.ToString().ToLowerInvariant()}");
        sb.AppendLine(Inv, $"toggleSprint:{options.SprintToggle.ToString().ToLowerInvariant()}");
        sb.AppendLine(Inv, $"fov:{(options.Fov - 70.0) / 40.0:0.0######}");

        // Custom / Modded / Unmapped entries
        foreach (var (key, val) in options.CustomEntries)
        {
            sb.AppendLine($"{key}:{val}");
        }

        return sb.ToString();
    }
}
