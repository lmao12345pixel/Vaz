namespace Vazer.Core.Auth;

internal sealed record XboxAuthRequest(
    string RelyingParty,
    string TokenType,
    XboxAuthProperties Properties);

internal sealed record XboxAuthProperties(
    string AuthMethod,
    string SiteName,
    string RpsTicket);

internal sealed record XstsAuthRequest(
    string RelyingParty,
    string TokenType,
    XstsAuthProperties Properties);

internal sealed record XstsAuthProperties(
    string[] UserTokens);

internal sealed record MinecraftLoginRequest(
    string IdentityToken);
