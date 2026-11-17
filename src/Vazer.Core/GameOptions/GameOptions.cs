using System.Globalization;

namespace Vazer.Core.GameOptions;

/// <summary>
/// Domain model biểu diễn các thiết lập cơ bản trong options.txt của Minecraft.
/// </summary>
public sealed class GameOptions
{
    // Graphics
    public int RenderDistance { get; set; } = 12;
    public int SimulationDistance { get; set; } = 12;
    public int MaxFps { get; set; } = 120;
    public string GraphicsMode { get; set; } = "fancy";
    public bool RenderClouds { get; set; } = true;
    public int GuiScale { get; set; } = 0;
    public double Gamma { get; set; } = 1.0;
    public bool EnableVsync { get; set; } = false;
    public bool EntityShadows { get; set; } = true;

    // Audio (0.0 .. 1.0)
    public double SoundCategoryMaster { get; set; } = 1.0;
    public double SoundCategoryMusic { get; set; } = 0.5;
    public double SoundCategoryWeather { get; set; } = 0.7;
    public double SoundCategoryBlock { get; set; } = 1.0;
    public double SoundCategoryHostile { get; set; } = 0.8;
    public double SoundCategoryPlayer { get; set; } = 1.0;
    public double SoundCategoryAmbient { get; set; } = 1.0;

    // Controls
    public double MouseSensitivity { get; set; } = 0.5;
    public bool InvertYMouse { get; set; } = false;
    public bool AutoJump { get; set; } = false;
    public bool SneakToggle { get; set; } = false;
    public bool SprintToggle { get; set; } = true;
    public double Fov { get; set; } = 70.0;

    // Custom / modded / unmapped options
    public Dictionary<string, string> CustomEntries { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Tạo bản sao sâu (deep clone) của GameOptions.
    /// </summary>
    public GameOptions Clone()
    {
        var clone = (GameOptions)MemberwiseClone();
        clone.CustomEntries = new Dictionary<string, string>(CustomEntries, StringComparer.OrdinalIgnoreCase);
        return clone;
    }
}
