using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vazer.Core.Abstractions;

namespace Vazer.Core.Store;

/// <summary>
/// File-based account store with DPAPI token encryption.
/// </summary>
public sealed class AccountStore : IAccountStore
{
    private readonly string _filePath;
    private readonly ILogger<AccountStore> _logger;
    private readonly MinecraftOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public AccountStore(
        ILogger<AccountStore> logger,
        IOptions<MinecraftOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _filePath = Path.Combine(_options.RootPath, "accounts", "accounts.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MinecraftAccount>> ListAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var data = await LoadAsync(ct);
            // Return accounts without sensitive tokens (protected versions stay in file)
            return data.Accounts
                .Select(a => a with
                {
                    AccessToken = null,
                    RefreshToken = null,
                    AccessTokenProtected = null,
                    RefreshTokenProtected = null
                })
                .ToList()
                .AsReadOnly();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<MinecraftAccount?> GetAsync(string id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var data = await LoadAsync(ct);
            var account = data.Accounts.FirstOrDefault(a => a.Id == id);
            if (account == null) return null;

            // Decrypt tokens for runtime use
            return DecryptAccount(account);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(MinecraftAccount account, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var data = await LoadAsync(ct);

            // Encrypt tokens before saving
            var toSave = EncryptAccount(account);

            var existingIndex = data.Accounts.FindIndex(a => a.Id == account.Id);
            if (existingIndex >= 0)
            {
                data.Accounts[existingIndex] = toSave;
            }
            else
            {
                data.Accounts.Add(toSave);
            }

            await SaveAsync(data, ct);
            _logger.LogDebug("Saved account {Id}", account.Id);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var data = await LoadAsync(ct);
            data.Accounts.RemoveAll(a => a.Id == id);
            if (data.ActiveId == id)
            {
                data.ActiveId = null;
            }
            await SaveAsync(data, ct);
            _logger.LogInformation("Deleted account {Id}", id);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<MinecraftAccount?> GetActiveAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var data = await LoadAsync(ct);
            if (string.IsNullOrEmpty(data.ActiveId)) return null;

            var account = data.Accounts.FirstOrDefault(a => a.Id == data.ActiveId);
            return account != null ? DecryptAccount(account) : null;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SetActiveAsync(string id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var data = await LoadAsync(ct);
            if (!data.Accounts.Any(a => a.Id == id))
            {
                throw new InvalidOperationException($"Account {id} not found");
            }
            data.ActiveId = id;
            await SaveAsync(data, ct);
            _logger.LogInformation("Set active account: {Id}", id);
        }
        finally
        {
            _lock.Release();
        }
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    private async Task<AccountsFile> LoadAsync(CancellationToken ct)
    {
        if (!File.Exists(_filePath))
        {
            return new AccountsFile();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filePath, ct);
            return JsonSerializer.Deserialize<AccountsFile>(json, _jsonOptions) ?? new AccountsFile();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse accounts file, creating new");
            return new AccountsFile();
        }
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    private async Task SaveAsync(AccountsFile data, CancellationToken ct)
    {
        var tmpPath = _filePath + ".tmp";
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        await File.WriteAllTextAsync(tmpPath, json, ct);
        File.Move(tmpPath, _filePath, true);
    }

    private MinecraftAccount EncryptAccount(MinecraftAccount account)
    {
        return account with
        {
            AccessTokenProtected = Protect(account.AccessToken),
            RefreshTokenProtected = Protect(account.RefreshToken),
            AccessToken = null,
            RefreshToken = null
        };
    }

    private MinecraftAccount DecryptAccount(MinecraftAccount account)
    {
        return account with
        {
            AccessToken = Unprotect(account.AccessTokenProtected),
            RefreshToken = Unprotect(account.RefreshTokenProtected)
        };
    }

    private string? Protect(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return null;

        try
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to protect token");
            return null;
        }
    }

    private string? Unprotect(string? protectedBase64)
    {
        if (string.IsNullOrEmpty(protectedBase64)) return null;

        try
        {
            var protectedBytes = Convert.FromBase64String(protectedBase64);
            var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to unprotect token - may be from different user or corrupted");
            return null;
        }
    }

    private sealed class AccountsFile
    {
        public string? ActiveId { get; set; }
        public List<MinecraftAccount> Accounts { get; set; } = new();
    }
}

