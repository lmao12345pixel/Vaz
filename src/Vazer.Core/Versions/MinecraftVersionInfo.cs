using System.Text.Json.Serialization;

namespace Vazer.Core.Versions;

/// <summary>
///Thông tin một phiên bản Minecraft từ Mojang version_manifest_v2.json.
/// </summary>
public record MinecraftVersionInfo
{
    /// <summary>Version ID (VD: "1.21.1", "24w36a").</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Loại: "release", "snapshot", "old_beta", "old_alpha".</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "release";

    /// <summary>Ngày phát hành (ISO 8601).</summary>
    [JsonPropertyName("releaseTime")]
    public string ReleaseTime { get; set; } = string.Empty;

    /// <summary>Đường dẫn manifest JSON (không dùng, chỉ lưu để parse).</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Từ khóa tìm kiếm để lọc (release, snapshot, beta, alpha).</summary>
    public string FilterKey => Type switch
    {
        "release" => "release",
        "snapshot" => "snapshot",
        "old_beta" => "beta",
        "old_alpha" => "alpha",
        _ => Type
    };

    /// <summary>Nếu version này đã được cài đặt (thư mục versions/{id} tồn tại).</summary>
    public bool IsInstalled { get; set; }
}
