using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vaz.WinUI.ViewModels;
using Vazer.Core.Abstractions;
using Vazer.Core.Loaders.Optifine;

namespace Vaz.WinUI.Views;

/// <summary>
/// Dialog cài OptiFine 2 tab: Tự động (scrape catalog qua IOptifineInstallerService)
/// và Thủ công (paste URL từ optifine.net). Không bundle JAR.
/// </summary>
public sealed partial class OptifineInstallDialog : ContentDialog
{
    private sealed class ReleaseRow
    {
        public required OptifineRelease Release { get; init; }
        public string Label =>
            $"[{(string.Equals(Release.Type, "preview", StringComparison.OrdinalIgnoreCase) ? "Preview" : "Stable")}] " +
            $"{Release.Edition} — {Release.Version}";
    }

    private readonly InstanceDetailViewModel _vm;

    public OptifineInstallDialog(InstanceDetailViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        _ = LoadCatalogAsync();
    }

    // ---------- Tab tự động ----------

    private async Task LoadCatalogAsync()
    {
        AutoErrorBar.IsOpen = false;
        SetBusy(true, "Đang tải catalog từ optifine.net...");
        ReleaseCombo.ItemsSource = null;
        InstallCatalogBtn.IsEnabled = false;

        try
        {
            var catalog = await _vm.GetOptifineCatalogAsync();

            // Group theo Edition, Version desc trong mỗi group.
            var rows = catalog.Releases
                .OrderBy(r => r.Edition, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(r => r.Version, StringComparer.OrdinalIgnoreCase)
                .Select(r => new ReleaseRow { Release = r })
                .ToList();

            ReleaseCombo.ItemsSource = rows;
            if (rows.Count > 0)
            {
                ReleaseCombo.SelectedIndex = 0;
                ReleaseHintVisible(true);
            }
            else
            {
                AutoErrorBar.Title = "Catalog rỗng";
                AutoErrorBar.Message = "optifine.net không có bản nào cho MC này. Dùng tab Thủ công.";
                AutoErrorBar.IsOpen = true;
            }
        }
        catch (OptifineVersionNotFoundException ex)
        {
            AutoErrorBar.Title = "MC không có trên optifine.net";
            AutoErrorBar.Message = $"MC {ex.McVersion} không có trên optifine.net. Dùng tab Manual.";
            AutoErrorBar.IsOpen = true;
            Tabs.SelectedIndex = 1; // auto switch sang tab Manual
        }
        catch (Exception ex)
        {
            AutoErrorBar.Title = "Lỗi mạng";
            AutoErrorBar.Message = $"{ex.Message} — thử lại, hoặc dùng tab Thủ công.";
            AutoErrorBar.IsOpen = true;
        }
        finally
        {
            SetBusy(false, string.Empty);
        }
    }

    private void ReleaseHintVisible(bool visible) =>
        InstallCatalogBtn.IsEnabled = visible && ReleaseCombo.SelectedItem is ReleaseRow;

    private void ReleaseCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        InstallCatalogBtn.IsEnabled = !IsBusyNow && ReleaseCombo.SelectedItem is ReleaseRow;

    private async void RetryCatalog_Click(object sender, RoutedEventArgs e) => await LoadCatalogAsync();

    private async void InstallCatalog_Click(object sender, RoutedEventArgs e)
    {
        if (ReleaseCombo.SelectedItem is not ReleaseRow row) return;
        await RunInstallAsync(progress => _vm.InstallOptifineAsync(row.Release, progress));
    }

    // ---------- Tab thủ công ----------

    private void UrlBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var url = UrlBox.Text?.Trim() ?? string.Empty;
        var valid = IsValidOptifineUrl(url);
        UrlValidation.Visibility = url.Length > 0 && !valid ? Visibility.Visible : Visibility.Collapsed;
        InstallUrlBtn.IsEnabled = !IsBusyNow && valid;
    }

    private static bool IsValidOptifineUrl(string url) =>
        url.StartsWith("https://optifine.net/", StringComparison.OrdinalIgnoreCase);

    private async void InstallUrl_Click(object sender, RoutedEventArgs e)
    {
        var url = UrlBox.Text?.Trim() ?? string.Empty;
        if (!IsValidOptifineUrl(url)) return;
        await RunInstallAsync(progress => _vm.InstallOptifineUrlAsync(url, progress));
    }

    // ---------- Dùng chung ----------

    private bool IsBusyNow => BusyRing.IsActive;

    private void SetBusy(bool busy, string message)
    {
        BusyRing.IsActive = busy;
        BusyText.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
        BusyText.Text = message;
        InstallCatalogBtn.IsEnabled = !busy && ReleaseCombo.SelectedItem is ReleaseRow;
        InstallUrlBtn.IsEnabled = !busy && IsValidOptifineUrl(UrlBox.Text?.Trim() ?? string.Empty);
    }

    private async Task RunInstallAsync(Func<IProgress<InstallProgress>, Task<InstallOptifineResult>> install)
    {
        SetBusy(true, "Đang cài...");
        try
        {
            var progress = new Progress<InstallProgress>(p =>
                App.Dispatcher.TryEnqueue(() =>
                    BusyText.Text = p.Status ?? $"{p.PercentComplete}%"));

            var result = await install(progress);

            if (!result.Success)
            {
                AutoErrorBar.Title = "Cài thất bại";
                AutoErrorBar.Message = result.Warning ?? "Không cài được OptiFine.";
                AutoErrorBar.IsOpen = true;
                Tabs.SelectedIndex = 0;
                return;
            }

            Hide(); // WinUI chỉ cho 1 ContentDialog/lúc — đóng trước khi mở follow-up

            if (!string.IsNullOrEmpty(result.Warning))
            {
                await ShowWarningFollowupAsync(result.Warning);
            }
            return;
        }
        catch (OptifineVersionNotFoundException ex)
        {
            AutoErrorBar.Title = "MC không có trên optifine.net";
            AutoErrorBar.Message = $"MC {ex.McVersion} không có trên optifine.net. Dùng tab Manual.";
            AutoErrorBar.IsOpen = true;
            Tabs.SelectedIndex = 1;
        }
        catch (Exception ex)
        {
            AutoErrorBar.Title = "Lỗi khi cài";
            AutoErrorBar.Message = $"{ex.Message} — thử lại, hoặc dùng tab Thủ công.";
            AutoErrorBar.IsOpen = true;
            Tabs.SelectedIndex = 0;
        }
        finally
        {
            SetBusy(false, string.Empty);
        }
    }

    /// <summary>Sau khi cài xong mà có Warning: dialog 2 nút [Đã hiểu] [Chuyển sang Performance Pack].</summary>
    private async Task ShowWarningFollowupAsync(string warning)
    {
        await Task.Delay(200);

        var dlg = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Cài xong — có cảnh báo",
            Content = new TextBlock { Text = warning, TextWrapping = TextWrapping.Wrap },
            PrimaryButtonText = "Chuyển sang Performance Pack",
            CloseButtonText = "Đã hiểu",
            DefaultButton = ContentDialogButton.Close
        };

        var r = await dlg.ShowAsync();
        if (r == ContentDialogResult.Primary)
        {
            await _vm.InstallPerformancePackCommand.ExecuteAsync(null);
        }
    }
}
