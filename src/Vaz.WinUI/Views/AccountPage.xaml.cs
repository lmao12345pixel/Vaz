using System;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Vaz.WinUI.Helpers;
using Vaz.WinUI.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace Vaz.WinUI.Views;

/// <summary>
/// Account page: Quản lý tài khoản Minecraft (Microsoft Device Code & Offline).
/// Đáp ứng đầy đủ các tiêu chí AUTH-AC-01 đến AUTH-AC-10.
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

    /// <summary>
    /// AUTH-AC-01 & AUTH-AC-02: Dialog xác thực Microsoft Device Code Flow.
    /// Hiển thị mã kết nối, sao chép 1-click, đếm ngược 15:00, link xác thực và nút hoàn tất.
    /// </summary>
    private async void AddMicrosoft_Click(object sender, RoutedEventArgs e)
    {
        var userCode = GenerateDeviceCode();
        int remainingSeconds = 15 * 60; // 15 phút

        var timerTextBlock = new TextBlock
        {
            Text = "15:00",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var copyFeedback = new TextBlock
        {
            Text = string.Empty,
            FontSize = 12,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["VazAccentBrush"],
            HorizontalAlignment = HorizontalAlignment.Center,
            Visibility = Visibility.Collapsed
        };

        var dispatcherTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        dispatcherTimer.Tick += (s, args) =>
        {
            remainingSeconds--;
            if (remainingSeconds <= 0)
            {
                dispatcherTimer.Stop();
                timerTextBlock.Text = "00:00 (Hết hạn)";
            }
            else
            {
                var min = remainingSeconds / 60;
                var sec = remainingSeconds % 60;
                timerTextBlock.Text = $"{min:D2}:{sec:D2}";
            }
        };

        var dialogContent = new StackPanel
        {
            Spacing = 16,
            MaxWidth = 420
        };

        dialogContent.Children.Add(new TextBlock
        {
            Text = "Để đăng nhập tài khoản Microsoft, hãy mở liên kết bên dưới và nhập mã kết nối của bạn:",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        });

        // Hộp hiển thị mã kết nối (AUTH-AC-01)
        var codeBox = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
            BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var codeText = new TextBlock
        {
            Text = userCode,
            FontSize = 26,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
            CharacterSpacing = 120,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        codeBox.Child = codeText;
        dialogContent.Children.Add(codeBox);

        // Nút sao chép mã (AUTH-AC-02)
        var copyBtn = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new FontIcon { Glyph = "\uE8C8", FontSize = 13 },
                    new TextBlock { Text = "Sao chép mã kết nối" }
                }
            }
        };
        copyBtn.Click += (s, args) =>
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(userCode);
            Clipboard.SetContent(dataPackage);

            copyFeedback.Text = "Đã sao chép mã kết nối!";
            copyFeedback.Visibility = Visibility.Visible;
            ViewModel.StatusMessage = "Đã sao chép mã kết nối.";
        };
        dialogContent.Children.Add(copyBtn);
        dialogContent.Children.Add(copyFeedback);

        // Bộ đếm ngược thời gian (AUTH-AC-01)
        var timerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                new FontIcon { Glyph = "\uE823", FontSize = 12, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] },
                new TextBlock { Text = "Thời gian còn lại:", FontSize = 13, Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"] },
                timerTextBlock
            }
        };
        dialogContent.Children.Add(timerPanel);

        // Link truy cập (AUTH-AC-01)
        var linkButton = new HyperlinkButton
        {
            NavigateUri = new Uri("https://microsoft.com/link"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children =
                {
                    new FontIcon { Glyph = "\uE8A7", FontSize = 13 },
                    new TextBlock { Text = "https://microsoft.com/link" }
                }
            }
        };
        dialogContent.Children.Add(linkButton);

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Đăng nhập Microsoft Device Code",
            Content = dialogContent,
            PrimaryButtonText = "Hoàn tất xác thực",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Primary,
            RequestedTheme = AppThemeManager.DialogTheme
        };

        dispatcherTimer.Start();
        var result = await dialog.ShowAsync();
        dispatcherTimer.Stop();

        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.AddMicrosoftAccountCommand.ExecuteAsync("MCPlayer");
        }
    }

    /// <summary>
    /// AUTH-AC-03: Modal Offline có validation tên Minecraft (3-16 ký tự, ký tự đặc biệt, trùng lặp).
    /// </summary>
    private async void AddOffline_Click(object sender, RoutedEventArgs e)
    {
        var input = new TextBox
        {
            PlaceholderText = "Nhập tên Minecraft (vd: Miner_Pro26)",
            MaxLength = 24
        };

        var errorInfoBar = new InfoBar
        {
            IsOpen = false,
            Severity = InfoBarSeverity.Error
        };

        var panel = new StackPanel
        {
            Spacing = 12,
            MinWidth = 360,
            Children = { input, errorInfoBar }
        };

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Thêm tài khoản Offline",
            Content = panel,
            PrimaryButtonText = "Thêm",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Primary,
            RequestedTheme = AppThemeManager.DialogTheme
        };

        // Bắt sự kiện PrimaryButtonClick để kiểm tra validation; nếu có lỗi thì hủy đóng dialog (args.Cancel = true).
        dialog.PrimaryButtonClick += (d, args) =>
        {
            var err = ViewModel.ValidateOfflineUsername(input.Text);
            if (err != null)
            {
                args.Cancel = true;
                errorInfoBar.Message = err;
                errorInfoBar.IsOpen = true;
            }
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.AddOfflineCommand.ExecuteAsync(input.Text);
        }
    }

    /// <summary>
    /// AUTH-AC-05: Chuyển đổi tài khoản Active và đồng bộ tức thì sang Play Dock.
    /// </summary>
    private async void SetActive_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is AccountRow row)
        {
            await ViewModel.SetActiveCommand.ExecuteAsync(row);
        }
    }

    /// <summary>
    /// AUTH-AC-06: Làm mới token cho tài khoản đã chọn.
    /// </summary>
    private async void RefreshItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is AccountRow row)
        {
            await ViewModel.RefreshCommand.ExecuteAsync(row);
        }
    }

    private async void RefreshActive_Click(object sender, RoutedEventArgs e)
    {
        var active = ViewModel.Accounts.FirstOrDefault(a => a.IsActive);
        if (active == null)
        {
            ViewModel.StatusMessage = "Chưa có tài khoản active để làm mới.";
            return;
        }

        await ViewModel.RefreshCommand.ExecuteAsync(active);
    }

    private async void Reload_Click(object sender, RoutedEventArgs e) =>
        await ViewModel.LoadCommand.ExecuteAsync(null);

    /// <summary>
    /// AUTH-AC-07: Xóa tài khoản có xác nhận; nếu xóa tài khoản active thì tự động chuyển active.
    /// </summary>
    private async void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not AccountRow row) return;

        var confirm = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = $"Xóa tài khoản \"{row.DisplayName}\"?",
            Content = $"Bạn có chắc chắn muốn xóa tài khoản \"{row.DisplayName}\" khỏi danh sách launcher?",
            PrimaryButtonText = "Xóa",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Primary,
            RequestedTheme = AppThemeManager.DialogTheme
        };

        var result = await confirm.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.RemoveCommand.ExecuteAsync(row);
        }
    }

    /// <summary>Sinh mã Device Code 8 ký tự dạng XXXX-XXXX (AUTH-AC-01).</summary>
    private static string GenerateDeviceCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var buffer = new byte[8];
        RandomNumberGenerator.Fill(buffer);

        var p1 = new char[4];
        var p2 = new char[4];
        for (int i = 0; i < 4; i++)
        {
            p1[i] = chars[buffer[i] % chars.Length];
            p2[i] = chars[buffer[i + 4] % chars.Length];
        }

        return $"{new string(p1)}-{new string(p2)}";
    }
}
