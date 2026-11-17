namespace Vazer.Core.GameOptions;

/// <summary>
/// Các cấu hình mẫu (preset) tối ưu cho GameOptions.
/// </summary>
public static class GameOptionsPresets
{
    public static GameOptions Performance => new()
    {
        RenderDistance = 6,
        SimulationDistance = 6,
        MaxFps = 260,
        GraphicsMode = "fast",
        RenderClouds = false,
        EnableVsync = false,
        EntityShadows = false
    };

    public static GameOptions Balanced => new()
    {
        RenderDistance = 12,
        SimulationDistance = 10,
        MaxFps = 120,
        GraphicsMode = "fancy",
        RenderClouds = true,
        EnableVsync = true,
        EntityShadows = true
    };

    public static GameOptions HighQuality => new()
    {
        RenderDistance = 20,
        SimulationDistance = 16,
        MaxFps = 144,
        GraphicsMode = "fabulous",
        RenderClouds = true,
        EnableVsync = true,
        EntityShadows = true
    };

    /// <summary>
    /// Áp dụng các trường đồ họa từ preset vào options mục tiêu, giữ nguyên âm thanh, điều khiển và custom entries.
    /// </summary>
    public static void ApplyTo(this GameOptions preset, GameOptions target)
    {
        ArgumentNullException.ThrowIfNull(preset);
        ArgumentNullException.ThrowIfNull(target);

        target.RenderDistance = preset.RenderDistance;
        target.SimulationDistance = preset.SimulationDistance;
        target.MaxFps = preset.MaxFps;
        target.GraphicsMode = preset.GraphicsMode;
        target.RenderClouds = preset.RenderClouds;
        target.EnableVsync = preset.EnableVsync;
        target.EntityShadows = preset.EntityShadows;
    }

    /// <summary>
    /// Lấy preset theo tên hoặc alias ("performance"/"fps", "balanced", "highquality"/"high").
    /// </summary>
    public static GameOptions? GetByName(string presetName)
    {
        if (string.IsNullOrWhiteSpace(presetName)) return null;

        var key = presetName.Trim().ToLowerInvariant();
        return key switch
        {
            "performance" or "fps" or "tối ưu fps" => Performance,
            "balanced" or "cân bằng" => Balanced,
            "highquality" or "high" or "đồ họa cao" => HighQuality,
            _ => null
        };
    }
}
