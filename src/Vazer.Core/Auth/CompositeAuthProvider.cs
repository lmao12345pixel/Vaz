using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Vazer.Core.Abstractions;

namespace Vazer.Core.Auth;

/// <summary>
/// Composite auth providerdelegate đến các provider con dựa trên AccountKind.
/// Cũng quản lý lưu trữ tài khoản qua IAccountStore.
/// </summary>
public sealed class CompositeAuthProvider : IAuthProvider
{
    private readonly IEnumerable<IAuthProvider> _providers;
    private readonly IAccountStore _accountStore;
    private readonly ILogger<CompositeAuthProvider> _logger;

    /// <inheritdoc />
    public AccountKind Kind => AccountKind.Microsoft; // Default, không dùng trực tiếp

    public CompositeAuthProvider(
        IEnumerable<IAuthProvider> providers,
        IAccountStore accountStore,
        ILogger<CompositeAuthProvider> logger)
    {
        _providers = providers;
        _accountStore = accountStore;
        _logger = logger;
    }

    /// <summary>
    /// Lấy provider cho AccountKind cụ thể.
    /// </summary>
    public IAuthProvider GetProvider(AccountKind kind)
    {
        var provider = _providers.FirstOrDefault(p => p.Kind == kind);
        if (provider == null)
        {
            throw new InvalidOperationException($"No auth provider registered for {kind}");
        }
        return provider;
    }

    /// <summary>
    /// Đăng nhập và tự động lưu vào store.
    /// </summary>
    public async Task<MinecraftAccount> LoginAndStoreAsync(AccountKind kind, CancellationToken ct = default)
    {
        var provider = GetProvider(kind);
        _logger.LogInformation("Starting login for {Kind}", kind);

        var account = await provider.LoginAsync(ct);

        await _accountStore.SaveAsync(account, ct);
        await _accountStore.SetActiveAsync(account.Id, ct);

        _logger.LogInformation("Login successful for {DisplayName} ({Id})", account.DisplayName, account.Id);
        return account;
    }

    /// <summary>
    /// Thử silent login (refresh token) cho tài khoản đã lưu.
    /// </summary>
    public async Task<MinecraftAccount?> TrySilentLoginAsync(string accountId, CancellationToken ct = default)
    {
        var account = await _accountStore.GetAsync(accountId, ct);
        if (account == null)
        {
            _logger.LogWarning("Account {Id} not found for silent login", accountId);
            return null;
        }

        if (!account.IsTokenValid)
        {
            _logger.LogInformation("Token expired for {Id}, attempting refresh", accountId);
            var provider = GetProvider(account.Kind);

            try
            {
                var refreshed = await provider.RefreshAsync(account, ct);
                await _accountStore.SaveAsync(refreshed, ct);
                _logger.LogInformation("Silent refresh successful for {Id}", accountId);
                return refreshed;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Silent refresh failed for {Id}", accountId);
                return null;
            }
        }

        return account;
    }

    /// <inheritdoc />
    public async Task<MinecraftAccount> LoginAsync(CancellationToken ct = default)
    {
        // Default to Microsoft
        return await LoginAndStoreAsync(AccountKind.Microsoft, ct);
    }

    /// <inheritdoc />
    public async Task<MinecraftAccount> RefreshAsync(MinecraftAccount account, CancellationToken ct = default)
    {
        var provider = GetProvider(account.Kind);
        var refreshed = await provider.RefreshAsync(account, ct);
        await _accountStore.SaveAsync(refreshed, ct);
        return refreshed;
    }

    /// <inheritdoc />
    public async Task LogoutAsync(MinecraftAccount account, CancellationToken ct = default)
    {
        var provider = GetProvider(account.Kind);
        await provider.LogoutAsync(account, ct);

        // Clear tokens but keep record
        var cleared = account with
        {
            AccessToken = null,
            RefreshToken = null,
            ExpiresAt = null,
            AccessTokenProtected = null,
            RefreshTokenProtected = null
        };
        await _accountStore.SaveAsync(cleared, ct);
    }
}
