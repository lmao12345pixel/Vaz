using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using Vazer.Core;
using Vazer.Core.Abstractions;
using Vazer.Core.Auth;

namespace Vaz.WinUI.ViewModels;

/// <summary>Row view cho tài khoản trong Account page.</summary>
public sealed class AccountRow
{
    public required MinecraftAccount Account { get; init; }
    public bool IsActive { get; init; }

    public string DisplayName => Account.DisplayName;
    public string KindText => Account.Kind == AccountKind.Microsoft ? "Microsoft" : "Offline";
    public string ActiveBadge => IsActive ? "✓ Active" : string.Empty;
    public string AvatarUrl => $"https://minotar.net/helm/{DisplayName}/64.png";
    public Uri AvatarUri => new(AvatarUrl);
}

/// <summary>
/// ViewModel cho Account page (Task 4.5): list, add offline, add Microsoft,
/// set active, refresh, remove — qua IAccountStore + CompositeAuthProvider.
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

    [RelayCommand]
    public async Task AddOfflineAsync(string? username)
    {
        var name = username?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            HasError = true;
            ErrorMessage = "Nhập username offline trước.";
            return;
        }

        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            HasError = false;

            // DI đăng ký OfflineAuthProvider như IAuthProvider — lấy qua composite rồi cast.
            if (_composite.GetProvider(AccountKind.Offline) is not OfflineAuthProvider offline)
                throw new InvalidOperationException("Offline provider không khả dụng.");

            var account = await offline.LoginAsync(name);
            await _store.SaveAsync(account);
            await _store.SetActiveAsync(account.Id);
            StatusMessage = $"Đã thêm offline \"{account.DisplayName}\".";
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
    public async Task AddMicrosoftAsync()
    {
        if (!HasMsalClientId)
        {
            HasError = true;
            ErrorMessage = "Chưa cấu hình MsalClientId trong MinecraftOptions — dùng Add Offline trước.";
            return;
        }

        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            HasError = false;

            // LoginAndStoreAsync: login + save + set active.
            var account = await _composite.LoginAndStoreAsync(AccountKind.Microsoft);
            StatusMessage = $"Đã thêm Microsoft \"{account.DisplayName}\".";
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
    public async Task SetActiveAsync(AccountRow? row)
    {
        if (row == null) return;

        try
        {
            IsBusy = true;
            NotifyBusyChanged();
            await _store.SetActiveAsync(row.Account.Id);
            StatusMessage = $"Đã chọn \"{row.DisplayName}\" làm tài khoản active.";
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

            // CompositeAuthProvider.RefreshAsync tự save lại account.
            await _composite.RefreshAsync(row.Account);
            StatusMessage = $"Đã refresh token \"{row.DisplayName}\".";
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
            await _store.DeleteAsync(row.Account.Id);
            StatusMessage = $"Đã xóa \"{row.DisplayName}\".";
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
