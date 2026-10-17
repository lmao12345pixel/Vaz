using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Vaz.WinUI.ViewModels;

namespace Vaz.WinUI.Views;

/// <summary>
/// Account page (Task 4.5): list tài khoản, add offline/Microsoft, set active,
/// refresh token, remove — cần cho Launch ở Instance Detail.
/// </summary>
public sealed partial class AccountPage : Page
{
    public AccountViewModel ViewModel { get; }

    public AccountPage()
    {
        ViewModel = ((App)Application.Current).Services
            .GetRequiredService<AccountViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = ViewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void AddOffline_Click(object sender, RoutedEventArgs e)
    {
        var input = new TextBox
        {
            PlaceholderText = "username (vd: Steve)",
            AcceptsReturn = false
        };

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Thêm tài khoản Offline",
            Content = input,
            PrimaryButtonText = "Thêm",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.AddOfflineCommand.ExecuteAsync(input.Text);
        }
    }

    private async void AddMicrosoft_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.HasMsalClientId)
        {
            var warn = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "Chưa cấu hình MsalClientId",
                Content = new TextBlock
                {
                    Text = "MinecraftOptions.MsalClientId đang trống nên chưa đăng nhập Microsoft được. " +
                           "Bạn có thể dùng Add Offline (OfflineDemoMode).",
                    TextWrapping = TextWrapping.Wrap
                },
                CloseButtonText = "Đã hiểu"
            };
            await warn.ShowAsync();
            return;
        }

        await ViewModel.AddMicrosoftCommand.ExecuteAsync(null);
    }

    private async void RefreshActive_Click(object sender, RoutedEventArgs e)
    {
        var active = ViewModel.Accounts.FirstOrDefault(a => a.IsActive);
        if (active == null)
        {
            ViewModel.StatusMessage = "Chưa có tài khoản active để refresh.";
            return;
        }

        await ViewModel.RefreshCommand.ExecuteAsync(active);
    }

    private async void Reload_Click(object sender, RoutedEventArgs e) =>
        await ViewModel.LoadCommand.ExecuteAsync(null);

    private async void SetActive_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is AccountRow row)
        {
            await ViewModel.SetActiveCommand.ExecuteAsync(row);
        }
    }

    private async void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not AccountRow row) return;

        var confirm = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Xóa tài khoản?",
            Content = $"Xóa \"{row.DisplayName}\" khỏi danh sách tài khoản?",
            PrimaryButtonText = "Xóa",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Close
        };

        var result = await confirm.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.RemoveCommand.ExecuteAsync(row);
        }
    }
}
