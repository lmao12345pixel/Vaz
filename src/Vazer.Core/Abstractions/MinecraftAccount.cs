using System.Text.Json.Serialization;

namespace Vazer.Core.Abstractions;

/// <summary>
/// Thông tin tài khoản Minecraft đã xác thực.
/// </summary>
public sealed record MinecraftAccount
{
    /// <summary>
    /// UUID của tài khoản (dùng làm identifier chính).
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Tên hiển thị (username Minecraft).
    /// </summary>
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    /// <summary>
    /// Loại tài khoản.
    /// </summary>
    [JsonPropertyName("kind")]
    public required AccountKind Kind { get; init; }

    /// <summary>
    /// UUID gốc từ profile Mojang (có thể khác Id nếu offline).
    /// </summary>
    [JsonPropertyName("uuid")]
    public string? Uuid { get; init; }

    /// <summary>
    /// Access token Minecraft (để launch game).
    /// Được mã hóa khi lưu trữ (xem _protected fields).
    /// </summary>
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; init; }

    /// <summary>
    /// Refresh token Microsoft (để renew access token).
    /// Được mã hóa khi lưu trữ.
    /// </summary>
    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Thời điểm access token hết hạn (UTC).
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Access token đã được bảo vệ (Windows DPAPI).
    /// Chỉ dùng khi serialize ra file.
    /// </summary>
    [JsonPropertyName("accessToken_protected")]
    public string? AccessTokenProtected { get; init; }

    /// <summary>
    /// Refresh token đã được bảo vệ (Windows DPAPI).
    /// Chỉ dùng khi serialize ra file.
    /// </summary>
    [JsonPropertyName("refreshToken_protected")]
    public string? RefreshTokenProtected { get; init; }

    /// <summary>
    /// Kiểm tra xem access token còn hợp lệ không.
    /// </summary>
    public bool IsTokenValid => ExpiresAt.HasValue && ExpiresAt.Value > DateTimeOffset.UtcNow.AddMinutes(5);

    /// <summary>
    /// Tạo bản sao với access token mới (sau khi refresh).
    /// </summary>
    public MinecraftAccount WithTokens(string accessToken, string? refreshToken, DateTimeOffset expiresAt)
    {
        return this with
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken ?? RefreshToken,
            ExpiresAt = expiresAt
        };
    }
}
