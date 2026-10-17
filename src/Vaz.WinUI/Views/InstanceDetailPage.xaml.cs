using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Vaz.WinUI.Helpers;
using Vaz.WinUI.ViewModels;

namespace Vaz.WinUI.Views;

/// <summary>
/// Instance Detail: header + Launch, tab Mods (list/update/mrpack/OptiFine/folder),
/// tab Settings (RAM/JVM disabled, path readonly, delete).
/// </summary>
public sealed partial class InstanceDetailPage : Page
{
    public InstanceDetailViewModel ViewModel { get; }

    public InstanceDetailPage()
    {
        ViewModel = ((App)Application.Current).Services
            .GetRequiredService<InstanceDetailViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = ViewModel.LoadCommand.ExecuteAsync(null);
    }

    private void ModEnabled_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts && ts.DataContext is InstalledModRow row && ts.IsOn != row.Enabled)
        {
            _ = ViewModel.SetModEnabledAsync(row, ts.IsOn);
        }
    }

    private async void InstallMrpack_Click(object sender, RoutedEventArgs e)
    {
        var picker = FilePickerHelper.CreateOpenPicker(App.MainWindow, ".mrpack");
        var file = await picker.PickSingleFileAsync();
        if (file == null) return;

        try
        {
            var preview = await ViewModel.InspectMrpackAsync(file.Path);

            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "Cài modpack?",
                PrimaryButtonText = "Cài",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Primary,
                Content = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = $"{preview.Name}\n" +
                           (string.IsNullOrEmpty(preview.Summary) ? "" : $"{preview.Summary}\n") +
                           $"Minecraft: {preview.MinecraftVersion}\n" +
                           $"Loader: {preview.LoaderType} {preview.LoaderVersion}\n" +
                           $"Files: {preview.FileCount}"
                }
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.ImportMrpackCommand.ExecuteAsync((file.Path, preview.Name));
            }
        }
        catch (Exception ex)
        {
            ViewModel.HasError = true;
            ViewModel.ErrorMessage = ex.Message;
        }
    }

    private async void InstallOptifine_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OptifineInstallDialog(ViewModel);
        dialog.XamlRoot = XamlRoot;
        await dialog.ShowAsync();
    }

    private async void DeleteInstance_Click(object sender, RoutedEventArgs e)
    {
        var confirm = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Xóa instance?",
            Content = "Toàn bộ thư mục instance sẽ bị xóa khỏi disk. Không thể hoàn tác.",
            PrimaryButtonText = "Xóa",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Close
        };

        var result = await confirm.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteCommand.ExecuteAsync(null);
        }
    }
}
