using System.Threading;
using Microsoft.Identity.Client;

namespace Vazer.Core.Auth;

/// <summary>
/// Minimal MSAL surface used by <see cref="MsalAuthProvider"/>, extracted as a
/// seam so the OAuth chain is unit-testable without UI or network.
/// Production implementation is <see cref="MsalClientWrapper"/>.
/// </summary>
public interface IMsalClientWrapper
{
    /// <summary>Accounts currently in the MSAL token cache.</summary>
    Task<IReadOnlyList<MsalCachedAccount>> GetAccountsAsync(CancellationToken ct = default);

    /// <summary>Interactive login (browser/system webview). UI only in production.</summary>
    Task<MsalTokenResult> AcquireTokenInteractiveAsync(string[] scopes, CancellationToken ct = default);

    /// <summary>
    /// Silent token acquisition for a cached account.
    /// Throws <see cref="MsalUiRequiredException"/> when interaction is required.
    /// </summary>
    Task<MsalTokenResult> AcquireTokenSilentAsync(string[] scopes, string accountHint, CancellationToken ct = default);

    /// <summary>Removes a cached account.</summary>
    Task RemoveAccountAsync(string accountHint, CancellationToken ct = default);
}

/// <summary>Cached MSAL account identifier (no tokens).</summary>
public sealed record MsalCachedAccount
{
    /// <summary>MSAL home account id.</summary>
    public required string HomeAccountId { get; init; }

    /// <summary>Username (UPN/email).</summary>
    public required string Username { get; init; }
}

/// <summary>MSAL authentication result (no refresh token in MSAL 4.x public API).</summary>
public sealed record MsalTokenResult
{
    /// <summary>Microsoft access token.</summary>
    public required string AccessToken { get; init; }

    /// <summary>Username of the authenticated account.</summary>
    public required string Username { get; init; }

    /// <summary>Home account id of the authenticated account.</summary>
    public required string HomeAccountId { get; init; }
}
