using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Vazer.Core.Abstractions;

namespace Vazer.Core.Auth;

/// <summary>
/// Microsoft OAuth provider - full chain: MSAL → Xbox Live → XSTS → Minecraft.
/// </summary>
public sealed class MsalAuthProvider : IAuthProvider
{
    private static readonly string[] Scopes = ["XboxLive.signin", "offline_access"];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MsalAuthProvider> _logger;
    private readonly MinecraftOptions _options;
    private readonly IMsalClientWrapper? _msal;

    /// <inheritdoc />
    public AccountKind Kind => AccountKind.Microsoft;

    public MsalAuthProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<MsalAuthProvider> logger,
        IOptions<MinecraftOptions> options)
        : this(httpClientFactory, logger, options, CreateDefaultWrapper(options.Value))
    {
    }

    internal MsalAuthProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<MsalAuthProvider> logger,
        IOptions<MinecraftOptions> options,
        IMsalClientWrapper? msal)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.MsalClientId) && !_options.OfflineDemoMode)
        {
            throw new InvalidOperationException("MsalClientId is not configured in MinecraftOptions");
        }

        _msal = msal;
    }

    private static IMsalClientWrapper? CreateDefaultWrapper(MinecraftOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.MsalClientId))
        {
            // Chưa cấu hình MSAL. OfflineDemoMode chỉ cần OfflineAuthProvider —
            // trả null; chỉ throw khi chế độ online thật sự cần đăng nhập Microsoft.
            if (!options.OfflineDemoMode)
            {
                throw new InvalidOperationException("MsalClientId is not configured in MinecraftOptions");
            }

            return null;
        }

        var cachePath = Path.Combine(options.RootPath, "auth", "msal_cache.bin");
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);

        var storageProperties = new StorageCreationPropertiesBuilder(
            Path.GetFileName(cachePath),
            Path.GetDirectoryName(cachePath)!)
            .Build();

        var cacheHelper = MsalCacheHelper.CreateAsync(storageProperties).GetAwaiter().GetResult();

        var pca = PublicClientApplicationBuilder.Create(options.MsalClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, "consumers")
            .WithRedirectUri("http://localhost")
            .Build();

        cacheHelper.RegisterCache(pca.UserTokenCache);
        return new MsalClientWrapper(pca);
    }

    private IMsalClientWrapper RequireMsal()
    {
        return _msal ?? throw new InvalidOperationException(
            "MSAL login requires MsalClientId. Configure it in appsettings.json or use Offline account.");
    }

    /// <inheritdoc />
    public async Task<MinecraftAccount> LoginAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting Microsoft OAuth login flow");

        // Step 1: MSAL Interactive - get Microsoft access token
        var msToken = await AcquireMsTokenAsync(ct);
        _logger.LogDebug("Acquired MS token (length: {Length})", msToken.AccessToken.Length);

        // Step 2: Xbox Live authenticate
        var (xblToken, uhs) = await XboxLiveAuthenticateAsync(msToken.AccessToken, ct);
        _logger.LogDebug("Xbox Live auth successful, uhs: {Uhs}", uhs);

        // Step 3: XSTS authorize
        var xstsToken = await XstsAuthorizeAsync(xblToken, ct);
        _logger.LogDebug("XSTS authorize successful");

        // Step 4: Minecraft login with Xbox
        var (mcToken, expiresIn) = await MinecraftLoginAsync(uhs, xstsToken, ct);
        _logger.LogDebug("Minecraft login successful, expires in {ExpiresIn}s", expiresIn);

        // Step 5: Get Minecraft profile
        var (uuid, name) = await GetMinecraftProfileAsync(mcToken, ct);
        _logger.LogDebug("Minecraft profile: {Name} ({Uuid})", name, uuid);

        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

        return new MinecraftAccount
        {
            Id = uuid,
            DisplayName = name,
            Kind = AccountKind.Microsoft,
            Uuid = uuid,
            AccessToken = mcToken,
            RefreshToken = "", // MSAL 4.x doesn't expose refresh token directly
            ExpiresAt = expiresAt,
            AccessTokenProtected = null,
            RefreshTokenProtected = null
        };
    }

    /// <inheritdoc />
    public async Task<MinecraftAccount> RefreshAsync(MinecraftAccount account, CancellationToken ct = default)
    {
        if (account.Kind != AccountKind.Microsoft)
        {
            throw new InvalidOperationException("Account is not Microsoft type");
        }

        _logger.LogInformation("Refreshing Microsoft token for {Id}", account.Id);

        // Let MsalUiRequiredException bubble up so the caller can fall back to LoginAsync.
        var result = await RequireMsal().AcquireTokenSilentAsync(Scopes, account.Id, ct);

        // Re-run Xbox Live → XSTS → Minecraft chain
        var (xblToken, uhs) = await XboxLiveAuthenticateAsync(result.AccessToken, ct);
        var xstsToken = await XstsAuthorizeAsync(xblToken, ct);
        var (mcToken, expiresIn) = await MinecraftLoginAsync(uhs, xstsToken, ct);
        var (uuid, name) = await GetMinecraftProfileAsync(mcToken, ct);

        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

        return account.WithTokens(mcToken, "", expiresAt);
    }

    /// <inheritdoc />
    public async Task LogoutAsync(MinecraftAccount account, CancellationToken ct = default)
    {
        if (account.Kind != AccountKind.Microsoft)
        {
            throw new InvalidOperationException("Account is not Microsoft type");
        }

        _logger.LogInformation("Logging out Microsoft account {Id}", account.Id);
        await RequireMsal().RemoveAccountAsync(account.Id, ct);
    }

    private async Task<MsalTokenResult> AcquireMsTokenAsync(CancellationToken ct)
    {
        var msal = RequireMsal();
        var accounts = await msal.GetAccountsAsync(ct);
        var account = accounts.FirstOrDefault();

        try
        {
            // Try silent first
            if (account != null)
            {
                return await msal.AcquireTokenSilentAsync(Scopes, account.HomeAccountId, ct);
            }
        }
        catch (MsalUiRequiredException)
        {
            // Fall through to interactive
        }

        // Interactive login
        return await msal.AcquireTokenInteractiveAsync(Scopes, ct);
    }

    [RequiresUnreferencedCode("Calls Vazer.Core.Auth.MsalAuthProvider.PostJsonAsync(HttpClient, String, Object, CancellationToken)")]
    private async Task<(string XblToken, string Uhs)> XboxLiveAuthenticateAsync(string msAccessToken, CancellationToken ct)
    {
        using var client = CreateHttpClient();
        var request = new
        {
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT",
            Properties = new
            {
                AuthMethod = "RPS",
                SiteName = "user.auth.xboxlive.com",
                RpsTicket = $"d={msAccessToken}"
            }
        };

        string response;
        try
        {
            response = await PostJsonAsync(client, "https://user.auth.xboxlive.com/user/authenticate", request, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException(
                "Xbox Live rejected the Microsoft token (401). Sign in again: the token may be expired or revoked.", ex);
        }

        var json = JsonNode.Parse(response)!;

        var xblToken = json["Token"]?.GetValue<string>() ?? throw new InvalidOperationException("XBL token missing in response");
        var uhs = json["DisplayClaims"]?["xui"]?[0]?["uhs"]?.GetValue<string>() ?? throw new InvalidOperationException("UHS missing in response");

        return (xblToken, uhs);
    }

    [RequiresUnreferencedCode("Calls Vazer.Core.Auth.MsalAuthProvider.PostJsonAsync(HttpClient, String, Object, CancellationToken)")]
    private async Task<string> XstsAuthorizeAsync(string xblToken, CancellationToken ct)
    {
        using var client = CreateHttpClient();
        var request = new
        {
            RelyingParty = "rp://api.minecraftservices.com/",
            TokenType = "JWT",
            Properties = new
            {
                UserTokens = new[] { xblToken }
            }
        };

        var response = await PostJsonAsync(client, "https://xsts.auth.xboxlive.com/xsts/authorize", request, ct);
        var json = JsonNode.Parse(response)!;

        // Check for XErr errors (these are 32-bit unsigned values > int.MaxValue)
        if (json["XErr"] != null)
        {
            var xErrValue = json["XErr"]?.GetValue<uint>() ?? 0u;
            string message;
            if (xErrValue == 2148916233u)
            {
                message = "Account does not have an Xbox profile. Create one at xbox.com";
            }
            else if (xErrValue == 2148916235u)
            {
                message = "Region blocked - Xbox Live not available in your region";
            }
            else if (xErrValue == 2148916238u)
            {
                message = "Child account requires parental consent";
            }
            else
            {
                message = $"XSTS authentication failed with error code: {xErrValue}";
            }
            throw new InvalidOperationException(message);
        }

        return json["Token"]?.GetValue<string>() ?? throw new InvalidOperationException("XSTS token missing in response");
    }

    [RequiresUnreferencedCode("Calls Vazer.Core.Auth.MsalAuthProvider.PostJsonAsync(HttpClient, String, Object, CancellationToken)")]
    private async Task<(string McToken, int ExpiresIn)> MinecraftLoginAsync(string uhs, string xstsToken, CancellationToken ct)
    {
        using var client = CreateHttpClient();
        var request = new
        {
            identityToken = $"XBL3.0 x={uhs};{xstsToken}"
        };

        var response = await PostJsonAsync(client, "https://api.minecraftservices.com/authentication/login_with_xbox", request, ct);
        var json = JsonNode.Parse(response)!;

        var mcToken = json["access_token"]?.GetValue<string>() ?? throw new InvalidOperationException("Minecraft access token missing");
        var expiresIn = json["expires_in"]?.GetValue<int>() ?? 86400;

        return (mcToken, expiresIn);
    }

    private async Task<(string Uuid, string Name)> GetMinecraftProfileAsync(string mcAccessToken, CancellationToken ct)
    {
        using var client = CreateHttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mcAccessToken);

        string response;
        try
        {
            response = await GetWithRetryAsync(client, "https://api.minecraftservices.com/minecraft/profile", ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException(
                "Minecraft profile not found (404). This account owns no Minecraft copy: " +
                "a paid Minecraft: Java Edition license is required.", ex);
        }

        var json = JsonNode.Parse(response)!;

        var uuid = json["id"]?.GetValue<string>() ?? throw new InvalidOperationException("UUID missing in profile");
        var name = json["name"]?.GetValue<string>() ?? throw new InvalidOperationException("Name missing in profile");

        return (uuid, name);
    }

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(_options.HttpTimeoutSeconds);
        return client;
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    private async Task<string> PostJsonAsync(HttpClient client, string url, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        for (int attempt = 0; attempt <= _options.HttpMaxRetries; attempt++)
        {
            try
            {
                var response = await client.PostAsync(url, content, ct);
                var responseBody = await response.Content.ReadAsStringAsync(ct);

                if ((int)response.StatusCode >= 500 && attempt < _options.HttpMaxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
                    continue;
                }

                response.EnsureSuccessStatusCode();
                return responseBody;
            }
            catch (HttpRequestException) when (attempt < _options.HttpMaxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }
        }

        throw new HttpRequestException($"Failed after {_options.HttpMaxRetries + 1} attempts");
    }

    private async Task<string> GetWithRetryAsync(HttpClient client, string url, CancellationToken ct)
    {
        for (int attempt = 0; attempt <= _options.HttpMaxRetries; attempt++)
        {
            try
            {
                var response = await client.GetAsync(url, ct);
                var responseBody = await response.Content.ReadAsStringAsync(ct);

                if ((int)response.StatusCode >= 500 && attempt < _options.HttpMaxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
                    continue;
                }

                response.EnsureSuccessStatusCode();
                return responseBody;
            }
            catch (HttpRequestException) when (attempt < _options.HttpMaxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }
        }

        throw new HttpRequestException($"Failed after {_options.HttpMaxRetries + 1} attempts");
    }
}
