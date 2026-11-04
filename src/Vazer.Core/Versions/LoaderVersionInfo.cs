using System.Text.Json.Serialization;

namespace Vazer.Core.Versions;

/// <summary>
/// Thông tin một phiên bản loader (Fabric/Quilt) từ metadata API.
/// </summary>
public record LoaderVersionInfo
{
    /// <summary>Version của loader (VD: "0.16.9", "5.2.0").</summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>Loader type (fabric/quilt).</summary>
    [JsonPropertyName("loader")]
    public string Loader { get; set; } = string.Empty;

    /// <summary>Phiên bản Minecraft tương thích (VD: "1.21.1").</summary>
    [JsonPropertyName("minecraft")]
    public string MinecraftVersion { get; set; } = string.Empty;

    /// <summary>Release type: "stable" hoặc "beta".</summary>
    [JsonPropertyName("release")]
    public string Release { get; set; } = "stable";

    /// <summary>Link download (không dùng, chỉ lưu).</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Nếu là release ổn định.</summary>
    public bool Stable => string.Equals(Release, "stable", StringComparison.OrdinalIgnoreCase);

    /// <summary>Nếu có thể cài đặt (release ổn định hoặc beta).</summary>
    public bool Installable => Stable || string.Equals(Release, "beta", StringComparison.OrdinalIgnoreCase);
}
