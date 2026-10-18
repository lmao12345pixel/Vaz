using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Vaz.WinUI.ViewModels;

namespace Vaz.WinUI.Views;

public sealed partial class InstancesPage : Page
{
    public InstancesViewModel ViewModel { get; }

    public InstancesPage()
    {
        ViewModel = ((App)Application.Current).Services
            .GetRequiredService<InstancesViewModel>();
        InitializeComponent();

        ViewModel.RequireAccountAction = async () =>
        {
            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "Chưa có tài khoản",
                Content = "Vui lòng chọn hoặc thêm tài khoản ở trang Accounts trước khi khởi chạy.",
                CloseButtonText = "Đóng",
                DefaultButton = ContentDialogButton.Close
            };
            await dialog.ShowAsync();
        };
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
        if (e.ClickedItem is InstanceCardItem card)
            NavigateToDetail(card.Id);
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is InstanceCardItem card)
            NavigateToDetail(card.Id);
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is InstanceCardItem card)
        {
            _ = ViewModel.PlayCommand.ExecuteAsync(card);
        }
    }

    private void NavigateToDetail(string instanceId)
    {
        var frame = ((App.MainWindow?.Content as NavigationView)
            ?.Content as Frame) ?? Frame;
        frame.Navigate(typeof(InstanceDetailPage), instanceId);
    }
}
