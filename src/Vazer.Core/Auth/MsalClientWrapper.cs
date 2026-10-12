using System.Threading;
using Microsoft.Identity.Client;

namespace Vazer.Core.Auth;

/// <summary>
/// Production <see cref="IMsalClientWrapper"/> over <see cref="IPublicClientApplication"/>.
/// </summary>
public sealed class MsalClientWrapper : IMsalClientWrapper
{
    private readonly IPublicClientApplication _pca;

    public MsalClientWrapper(IPublicClientApplication pca)
    {
        _pca = pca;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MsalCachedAccount>> GetAccountsAsync(CancellationToken ct = default)
    {
        var accounts = await _pca.GetAccountsAsync();
        return accounts
            .Select(a => new MsalCachedAccount
            {
                HomeAccountId = a.HomeAccountId.Identifier,
                Username = a.Username
            })
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<MsalTokenResult> AcquireTokenInteractiveAsync(string[] scopes, CancellationToken ct = default)
    {
        var result = await _pca.AcquireTokenInteractive(scopes)
            .WithPrompt(Prompt.SelectAccount)
            .ExecuteAsync(ct);
        return new MsalTokenResult
        {
            AccessToken = result.AccessToken,
            Username = result.Account.Username,
            HomeAccountId = result.Account.HomeAccountId.Identifier
        };
    }

    /// <inheritdoc />
    public async Task<MsalTokenResult> AcquireTokenSilentAsync(string[] scopes, string accountHint, CancellationToken ct = default)
    {
        var accounts = await _pca.GetAccountsAsync();
        var account = accounts.FirstOrDefault(a =>
                a.HomeAccountId.Identifier.Contains(accountHint, StringComparison.OrdinalIgnoreCase)
                || a.Username.Equals(accountHint, StringComparison.OrdinalIgnoreCase))
            ?? throw new MsalUiRequiredException("no_account", "No matching MSAL account in cache.");

        var result = await _pca.AcquireTokenSilent(scopes, account).ExecuteAsync(ct);
        return new MsalTokenResult
        {
            AccessToken = result.AccessToken,
            Username = result.Account.Username,
            HomeAccountId = result.Account.HomeAccountId.Identifier
        };
    }

    /// <inheritdoc />
    public async Task RemoveAccountAsync(string accountHint, CancellationToken ct = default)
    {
        var accounts = await _pca.GetAccountsAsync();
        var account = accounts.FirstOrDefault(a =>
            a.HomeAccountId.Identifier.Contains(accountHint, StringComparison.OrdinalIgnoreCase)
            || a.Username.Equals(accountHint, StringComparison.OrdinalIgnoreCase));
        if (account != null)
        {
            await _pca.RemoveAsync(account);
        }
    }
}
