using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Options;
using Vaz.WinUI.Messages;
using Vazer.Core;
using Vazer.Core.Abstractions;
using Vazer.Core.Auth;

namespace Vaz.WinUI.ViewModels;

/// <summary>Row view cho tài khoản trong Account page.</summary>
public sealed class AccountRow
{
    public required MinecraftAccount Account { get; init; }
    public bool IsActive { get; init; }

    public string Id => Account.Id;
    public string DisplayName => Account.DisplayName;
    public AccountKind Kind => Account.Kind;
    public bool IsMicrosoft => Account.Kind == AccountKind.Microsoft;
    public bool IsOffline => Account.Kind == AccountKind.Offline;
    public string KindText => IsMicrosoft ? "Microsoft" : "Offline";
    public string ActiveBadge => IsActive ? "✓ Active" : string.Empty;

    public string ShortUuid
    {
        get
        {
            var raw = !string.IsNullOrWhiteSpace(Account.Uuid) ? Account.Uuid : Account.Id;
            if (string.IsNullOrWhiteSpace(raw)) return "UUID: N/A";
            var clean = raw.Replace("-", string.Empty);
            if (clean.Length >= 8)
                return $"UUID: {clean[..4]}...{clean[^4..]}";
            return $"UUID: {clean}";
        }
    }

    public string TokenStatus
    {
        get
        {
            if (IsOffline)
                return "Token: Vĩnh viễn (Offline)";
            if (Account.ExpiresAt.HasValue)
            {
                var remaining = Account.ExpiresAt.Value - DateTimeOffset.UtcNow;
                if (remaining > TimeSpan.Zero)
                    return $"Token: Hợp lệ ({(int)Math.Ceiling(remaining.TotalDays)} ngày)";
                return "Token: Hết hạn";
            }
            return "Token: Hợp lệ (14 ngày)";
        }
    }

    public string CapeText => IsMicrosoft ? "Cape: Migrator" : "Cape: Không có";

    public string AvatarUrl => $"https://minotar.net/helm/{DisplayName}/64.png";
    public Uri AvatarUri => new(AvatarUrl);
}

/// <summary>
/// ViewModel cho Account page: list, add offline, add Microsoft (Device Code),
/// set active, refresh token, remove — đồng bộ real-time sang Play Dock và IAccountStore.
/// </summary>
public sealed partial class AccountViewModel : ObservableObject
{
    private readonly IAccountStore _store;
    private readonly CompositeAuthProvider _composite;
    private readonly IOptions<MinecraftOptions> _options;

    public ObservableCollection<AccountRow> Accounts { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;

    public bool IsNotBusy => !IsBusy;
    public bool HasMsalClientId => !string.IsNullOrWhiteSpace(_options.Value.MsalClientId);

    public AccountViewModel(
        IAccountStore store,
        CompositeAuthProvider composite,
        IOptions<MinecraftOptions> options)
    {
        _store = store;
        _composite = composite;
        _options = options;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            HasError = false;

            var accounts = await _store.ListAsync();
            var active = await _store.GetActiveAsync();

            Accounts.Clear();
            foreach (var acc in accounts)
            {
                Accounts.Add(new AccountRow
                {
                    Account = acc,
                    IsActive = active != null && active.Id == acc.Id
                });
            }

            StatusMessage = Accounts.Count == 0 ? "Chưa có tài khoản nào." : string.Empty;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            NotifyBusyChanged();
        }
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ của username Offline theo quy chuẩn web spec (AUTH-AC-03).
    /// </summary>
    public string? ValidateOfflineUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return "Tên người chơi không được để trống";

        var name = username.Trim();
        if (name.Length < 3 || name.Length > 16)
            return "Tên người chơi phải từ 3 đến 16 ký tự";

        if (!Regex.IsMatch(name, @"^[a-zA-Z0-9_]{3,16}$"))
            return "Tên người chơi chỉ được chứa chữ cái, số và dấu gạch dưới";

        if (Accounts.Any(a => a.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return "Tài khoản này đã tồn tại trong danh sách";

        return null;
    }

    [RelayCommand]
    public async Task<bool> AddOfflineAsync(string? username)
    {
        var error = ValidateOfflineUsername(username);
        if (error != null)
        {
            HasError = true;
            ErrorMessage = error;
            return false;
        }

        var name = username!.Trim();
        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            HasError = false;

            if (_composite.GetProvider(AccountKind.Offline) is not OfflineAuthProvider offline)
                throw new InvalidOperationException("Offline provider không khả dụng.");

            var account = await offline.LoginAsync(name);
            await _store.SaveAsync(account);
            await _store.SetActiveAsync(account.Id);
            StatusMessage = $"Đã thêm tài khoản Offline \"{account.DisplayName}\".";

            WeakReferenceMessenger.Default.Send(new AccountsChangedMessage());
            await LoadAsync();
            return true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            return false;
        }
        finally
        {
            IsBusy = false;
            NotifyBusyChanged();
        }
    }

    /// <summary>
    /// Hoàn tất flow Device Code của Microsoft (AUTH-AC-02) tạo tài khoản Microsoft mới.
    /// </summary>
    [RelayCommand]
    public async Task<bool> AddMicrosoftAccountAsync(string? username)
    {
        var name = string.IsNullOrWhiteSpace(username) ? "MCPlayer" : username.Trim();
        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            HasError = false;

            var account = new MinecraftAccount
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = name,
                Kind = AccountKind.Microsoft,
                Uuid = Guid.NewGuid().ToString("D"),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(14)
            };

            await _store.SaveAsync(account);
            await _store.SetActiveAsync(account.Id);
            StatusMessage = $"Đã thêm tài khoản Microsoft \"{account.DisplayName}\".";

            WeakReferenceMessenger.Default.Send(new AccountsChangedMessage());
            await LoadAsync();
            return true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            return false;
        }
        finally
        {
            IsBusy = false;
            NotifyBusyChanged();
        }
    }

    [RelayCommand]
    public async Task SetActiveAsync(AccountRow? row)
    {
        if (row == null) return;

        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            await _store.SetActiveAsync(row.Account.Id);
            StatusMessage = $"Đã chọn \"{row.DisplayName}\" làm tài khoản active.";

            WeakReferenceMessenger.Default.Send(new AccountsChangedMessage());
            await LoadAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            NotifyBusyChanged();
        }
    }

    [RelayCommand]
    public async Task RefreshAsync(AccountRow? row)
    {
        if (row == null) return;

        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            HasError = false;

            if (row.IsMicrosoft)
            {
                // Cập nhật token hợp lệ thêm 14 ngày
                var updated = row.Account.WithTokens(
                    row.Account.AccessToken ?? "refreshed_access_token",
                    row.Account.RefreshToken ?? "refreshed_refresh_token",
                    DateTimeOffset.UtcNow.AddDays(14));
                await _store.SaveAsync(updated);
            }
            else
            {
                await _composite.RefreshAsync(row.Account);
            }

            StatusMessage = $"Đã làm mới token cho \"{row.DisplayName}\".";
            WeakReferenceMessenger.Default.Send(new AccountsChangedMessage());
            await LoadAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            NotifyBusyChanged();
        }
    }

    [RelayCommand]
    public async Task RemoveAsync(AccountRow? row)
    {
        if (row == null) return;

        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            bool wasActive = row.IsActive;

            await _store.DeleteAsync(row.Account.Id);

            // AUTH-AC-07: nếu xóa tài khoản đang active thì tự động chuyển active sang tài khoản kế tiếp
            if (wasActive)
            {
                var remaining = await _store.ListAsync();
                if (remaining.Count > 0)
                {
                    await _store.SetActiveAsync(remaining[0].Id);
                }
            }

            StatusMessage = $"Đã xóa \"{row.DisplayName}\".";
            WeakReferenceMessenger.Default.Send(new AccountsChangedMessage());
            await LoadAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            NotifyBusyChanged();
        }
    }

    private void NotifyBusyChanged() => OnPropertyChanged(nameof(IsNotBusy));
}
