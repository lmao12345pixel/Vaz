using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Vaz.WinUI.Helpers;
using Vaz.WinUI.ViewModels;

namespace Vaz.WinUI.Views;

/// <summary>
/// Settings page (SET-AC-01..SET-AC-10). Dialog xác nhận đặt ở code-behind
/// theo pattern AccountPage; logic nằm trong <see cref="SettingsViewModel"/>.
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = ((App)Application.Current).Services
            .GetRequiredService<SettingsViewModel>();
        InitializeComponent();
    }

    /// <summary>ContentDialog không tự thừa hưởng RequestedTheme của page → set theo theme đang chọn.</summary>
    private static ElementTheme DialogTheme => AppThemeManager.DialogTheme;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = ViewModel.LoadCommand.ExecuteAsync(null);
    }

    // === Chip danh mục (SET-AC-01) ===

    private void CategoryChip_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton chip && chip.Tag is string value)
        {
            chip.IsChecked = string.Equals(value, ViewModel.SelectedCategoryKey, StringComparison.Ordinal);
        }
    }

    private void CategoryChip_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string value })
        {
            var category = ViewModel.Categories.FirstOrDefault(c => c.Value == value);
            if (category != null)
            {
                ViewModel.SelectedCategory = category;
            }
        }
    }

    private async void Reset_Click(object sender, RoutedEventArgs e)
    {
        var confirm = new ContentDialog
        {
            XamlRoot = XamlRoot,
            RequestedTheme = DialogTheme,
            Title = "Khôi phục cài đặt mặc định?",
            Content = "Tất cả thiết lập launcher, Java, RAM, giao diện và mạng sẽ trở về mặc định.",
            PrimaryButtonText = "Xác nhận",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Primary
        };

        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.ResetCommand.ExecuteAsync(null);
        }
    }

    private async void ClearCache_Click(object sender, RoutedEventArgs e)
    {
        var confirm = new ContentDialog
        {
            XamlRoot = XamlRoot,
            RequestedTheme = DialogTheme,
            Title = "Xóa cache tải xuống?",
            Content = "Các gói tải tạm sẽ bị xóa. Instance và mod đã cài vẫn giữ nguyên.",
            PrimaryButtonText = "Xóa",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Primary
        };

        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
        {
            ViewModel.ClearCacheCommand.Execute(null);
        }
    }

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e) =>
        ViewModel.OpenFolderCommand.Execute(ViewModel.DataPath);

    private void OpenBackupFolder_Click(object sender, RoutedEventArgs e) =>
        ViewModel.OpenFolderCommand.Execute(ViewModel.BackupPath);

    private async void ExportDiagnostic_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = FilePickerHelper.CreateSavePicker(
                App.MainWindow,
                $"vaz-diagnostic-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                ".json",
                "JSON");

            var file = await picker.PickSaveFileAsync();
            if (file == null)
            {
                return;
            }

            await ViewModel.ExportDiagnosticCommand.ExecuteAsync(file.Path);
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                RequestedTheme = DialogTheme,
                Title = "Không mở được hộp thoại lưu",
                Content = ex.Message,
                CloseButtonText = "Đóng",
                DefaultButton = ContentDialogButton.Close
            };

            await dialog.ShowAsync();
        }
    }
}
