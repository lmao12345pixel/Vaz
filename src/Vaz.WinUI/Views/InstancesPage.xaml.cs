using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Vaz.WinUI.ViewModels;
using Vazer.Core.Abstractions;

namespace Vaz.WinUI.Views;

public sealed partial class InstancesPage : Page
{
    public InstancesViewModel ViewModel { get; }

    public InstancesPage()
    {
        ViewModel = ((App)Application.Current).Services
            .GetRequiredService<InstancesViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = ViewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void NewInstance_Click(object sender, RoutedEventArgs e)
    {
        var vm = ((App)Application.Current).Services
            .GetRequiredService<CreateInstanceViewModel>();
        var dialog = new CreateInstanceDialog(vm) { XamlRoot = XamlRoot };
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.CreatedInstanceId != null)
            _ = ViewModel.LoadCommand.ExecuteAsync(null);
    }

    private void Instance_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is MinecraftInstance inst)
            NavigateToDetail(inst.Id);
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string id)
            NavigateToDetail(id);
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string id)
            NavigateToDetail(id);
    }

    private void NavigateToDetail(string instanceId)
    {
        var frame = ((App.MainWindow?.Content as NavigationView)
            ?.Content as Frame) ?? Frame;
        frame.Navigate(typeof(InstanceDetailPage), instanceId);
    }
}
