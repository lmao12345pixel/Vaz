using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vaz.WinUI.Services;
using Vaz.WinUI.ViewModels;
using Vaz.WinUI.Views;
using Vazer.Core.Abstractions;

namespace Vaz.WinUI;

/// <summary>
/// Main shell window: NavigationView (Mod Browser / Instances / Accounts),
/// instance picker in pane header, Frame content on the right.
/// </summary>
public sealed partial class MainWindow : Window
{
    public InstancePickerViewModel Picker { get; }

    public MainWindow()
    {
        InitializeComponent();

        Picker = ((App)Application.Current).Services
            .GetRequiredService<InstancePickerViewModel>();

        NavView.SelectionChanged += NavView_SelectionChanged;
        InstanceCombo.SelectionChanged += InstanceCombo_SelectionChanged;
        ContentFrame.Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await Picker.LoadAsync();
        if (Picker.SelectedInstance != null)
        {
            InstanceCombo.SelectedItem = Picker.SelectedInstance;
        }
        ContentFrame.Navigate(typeof(ModBrowserPage));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
        {
            return;
        }

        var pageType = tag switch
        {
            "modbrowser" => typeof(ModBrowserPage),
            "instances" => typeof(InstanceDetailPage),
            "accounts" => typeof(AccountPage),
            _ => null
        };

        if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }

    private void InstanceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (InstanceCombo.SelectedItem is MinecraftInstance inst)
        {
            Picker.SelectedInstance = inst;
        }
    }

    private async void NewInstanceButton_Click(object sender, RoutedEventArgs e)
    {
        var vm = ((App)Application.Current).Services
            .GetRequiredService<CreateInstanceViewModel>();
        var dialog = new CreateInstanceDialog(vm) { XamlRoot = Content.XamlRoot };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.CreatedInstanceId != null)
        {
            var createdId = dialog.CreatedInstanceId;

            // Refresh ComboBox rồi auto-select instance vừa tạo.
            // So theo Id vì object trong list sau reload là instance mới.
            await Picker.LoadAsync();
            var target = Picker.Instances.FirstOrDefault(i => i.Id == createdId);
            if (target != null)
            {
                Picker.SelectedInstance = target;
                InstanceCombo.SelectedItem = target;
            }
        }
    }
}
