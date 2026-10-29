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
        ViewModel.LogAppended += OnLogAppended;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        Loaded += (s, e) => SyncGraphicsModeComboBox();
        Unloaded += (s, e) =>
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            ViewModel.LogAppended -= OnLogAppended;
            ViewModel.Dispose();
        };
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.GameOptions))
        {
            SyncGraphicsModeComboBox();
            Bindings.Update();
        }
    }

    private void OnLogAppended()
    {
        if (ViewModel.AutoScroll && LogScrollViewer != null)
        {
            _ = App.Dispatcher?.TryEnqueue(() =>
            {
                LogScrollViewer.ChangeView(null, LogScrollViewer.ScrollableHeight, null, false);
            });
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string instanceId)
        {
            var active = ((App)Application.Current).Services
                .GetRequiredService<Vaz.WinUI.Services.ActiveInstanceService>();
            if (instanceId == active.CurrentInstanceId)
            {
                _ = ViewModel.LoadCommand.ExecuteAsync(null);
            }
            else
            {
                active.SetCurrent(instanceId);
            }
        }
        else
        {
            _ = ViewModel.LoadCommand.ExecuteAsync(null);
        }
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

    private async void DownloadMods_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ModBrowserDialog(ViewModel.Current?.Id);
        dialog.XamlRoot = XamlRoot;
        await dialog.ShowAsync();
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearLog();
    }

    private void CopyLog_Click(object sender, RoutedEventArgs e)
    {
        var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
        package.SetText(ViewModel.LogText ?? string.Empty);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
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

    private void ViewMode_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch ts)
        {
            ViewModel.SwitchViewMode(ts.IsOn);
            if (!ts.IsOn)
            {
                SyncGraphicsModeComboBox();
                Bindings.Update();
            }
        }
    }

    private void PresetPerformance_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ApplyPreset("Performance");
        SyncGraphicsModeComboBox();
        Bindings.Update();
    }

    private void PresetBalanced_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ApplyPreset("Balanced");
        SyncGraphicsModeComboBox();
        Bindings.Update();
    }

    private void PresetHighQuality_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ApplyPreset("HighQuality");
        SyncGraphicsModeComboBox();
        Bindings.Update();
    }

    private void GraphicsMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GraphicsModeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string mode)
        {
            if (ViewModel?.GameOptions != null)
            {
                ViewModel.GameOptions.GraphicsMode = mode;
            }
        }
    }

    private void SyncGraphicsModeComboBox()
    {
        if (GraphicsModeComboBox == null || ViewModel?.GameOptions == null) return;
        var mode = ViewModel.GameOptions.GraphicsMode?.ToLowerInvariant();
        var match = GraphicsModeComboBox.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(x => string.Equals((string)x.Tag, mode, StringComparison.OrdinalIgnoreCase));
        GraphicsModeComboBox.SelectedItem = match ?? GraphicsModeComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => (string)x.Tag == "fancy");
    }

    private async void ExportOptions_Click(object sender, RoutedEventArgs e)
    {
        var picker = FilePickerHelper.CreateSavePicker(App.MainWindow, "options.txt", ".txt", "Minecraft Options");
        var file = await picker.PickSaveFileAsync();
        if (file != null)
        {
            await ViewModel.ExportOptionsAsync(file.Path);
        }
    }
}
