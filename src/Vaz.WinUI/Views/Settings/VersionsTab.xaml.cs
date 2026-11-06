using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vaz.WinUI.ViewModels;
using Vazer.Core.Versions;

namespace Vaz.WinUI.Views.Settings;

public sealed partial class VersionsTab : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(SettingsViewModel),
            typeof(VersionsTab),
            new PropertyMetadata(null));

    public SettingsViewModel? ViewModel
    {
        get => (SettingsViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public VersionsTab()
    {
        InitializeComponent();
    }

    private async void DeleteVersion_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: InstalledVersionInfo item } || ViewModel == null) return;

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Xác nhận xóa phiên bản",
            Content = $"Xóa version '{item.VersionId}'?\nThao tác này sẽ giải phóng {item.SizeDisplay}.",
            PrimaryButtonText = "Xóa",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteVersionAsync(item);
        }
    }

    private async void Cleanup_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        var orphans = ViewModel.InstalledVersions.Where(v => !v.IsInUse).ToList();
        if (orphans.Count == 0)
        {
            var emptyDialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "Dọn dẹp phiên bản",
                Content = "Không có phiên bản mồ côi (không sử dụng) nào cần dọn dẹp.",
                CloseButtonText = "Đóng",
                DefaultButton = ContentDialogButton.Close
            };
            await emptyDialog.ShowAsync();
            return;
        }

        var totalBytes = orphans.Sum(o => o.SizeBytes);
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Dọn dẹp phiên bản không sử dụng",
            Content = $"Xóa {orphans.Count} version không dùng? Thao tác này sẽ giải phóng {InstalledVersionInfo.FormatBytes(totalBytes)}.",
            PrimaryButtonText = "Dọn dẹp ngay",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.CleanupOrphanVersionsAsync();
        }
    }
}
