using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Vazer.Core.Abstractions;

namespace Vazer.Core.Auth;

/// <summary>
/// Offline auth provider - tạo tài khoản offline (không cần internet).
/// Dùng cho LAN, single-player, testing.
/// UUID được deterministic từ username (Offline UUID = UUIDv3(namespace, username)).
/// </summary>
public sealed class OfflineAuthProvider : IAuthProvider
{
    private readonly ILogger<OfflineAuthProvider> _logger;

    /// <inheritdoc />
    public AccountKind Kind => AccountKind.Offline;

    public OfflineAuthProvider(ILogger<OfflineAuthProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<MinecraftAccount> LoginAsync(CancellationToken ct = default)
    {
        // Offline login không cần interactive - sẽ được gọi từ UI với username input
        throw new InvalidOperationException("Offline login requires username. Use LoginAsync(username).");
    }

    /// <summary>
    /// Tạo tài khoản offline từ username.
    /// </summary>
    /// <param name="username">Username (1-16 ký tự, alphanumeric + underscore).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Offline MinecraftAccount.</returns>
    public Task<MinecraftAccount> LoginAsync(string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be empty", nameof(username));
        }

        if (username.Length > 16)
        {
            throw new ArgumentException("Username cannot exceed 16 characters", nameof(username));
        }

        var uuid = GenerateOfflineUuid(username);
        var account = new MinecraftAccount
        {
            Id = uuid,
            DisplayName = username,
            Kind = AccountKind.Offline,
            Uuid = uuid,
            AccessToken = "offline-token-" + uuid, // Placeholder, không dùng thực tế
            RefreshToken = null,
            ExpiresAt = DateTimeOffset.UtcNow.AddYears(10),
            AccessTokenProtected = null,
            RefreshTokenProtected = null
        };

        _logger.LogInformation("Created offline account: {Username} ({Uuid})", username, uuid);
        return Task.FromResult(account);
    }

    /// <inheritdoc />
    public Task<MinecraftAccount> RefreshAsync(MinecraftAccount account, CancellationToken ct = default)
    {
        // Offline account không cần refresh
        if (account.Kind != AccountKind.Offline)
        {
            throw new InvalidOperationException("Account is not offline type");
        }
        return Task.FromResult(account);
    }

    /// <inheritdoc />
    public Task LogoutAsync(MinecraftAccount account, CancellationToken ct = default)
    {
        // Offline account không có token để clear
        _logger.LogInformation("Offline logout for {Username}", account.DisplayName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Tạo deterministic UUID cho offline mode (UUID v3 với namespace "OfflinePlayer").
    /// Cùng username luôn cho cùng UUID.
    /// </summary>
    public static string GenerateOfflineUuid(string username)
    {
        // Minecraft offline UUID: UUIDv3(Minecraft namespace, "OfflinePlayer:" + username)
        // Namespace: 00000000-0000-0000-0000-000000000000 (nil UUID)
        // Thực tế Minecraft dùng: UUID.nameUUIDFromBytes(("OfflinePlayer:" + username).getBytes())
        var input = "OfflinePlayer:" + username;
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = MD5.HashData(bytes);

        // Convert to UUID v3 format (network byte order).
        // NOTE: must format hex directly — new Guid(hash) swaps the first
        // 3 fields to little-endian and moves the version nibble.
        hash[6] = (byte)((hash[6] & 0x0f) | 0x30);
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
