using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Vaz.WinUI.Messages;
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

        Unloaded += (s, e) => ViewModel.Dispose();
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
        {
            await ViewModel.LoadCommand.ExecuteAsync(null);
            ViewModel.SetActiveTarget(dialog.CreatedInstanceId);
            WeakReferenceMessenger.Default.Send(new InstancesChangedMessage());
        }
    }

    private void Instance_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is InstanceCardItem card)
        {
            ViewModel.SetActiveTarget(card.Id);
            NavigateToDetail(card.Id);
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            var card = fe.DataContext as InstanceCardItem ?? (fe.Tag is string tag ? ViewModel.Instances.FirstOrDefault(i => i.Id == tag) : null);
            if (card != null)
            {
                NavigateToDetail(card.Id);
            }
            else if (ViewModel.Instances.Count > 0)
            {
                NavigateToDetail(ViewModel.Instances[0].Id);
            }
        }
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is InstanceCardItem card)
        {
            _ = ViewModel.PlayCommand.ExecuteAsync(card);
        }
    }

    private void CardFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is InstanceCardItem card)
        {
            InstancesViewModel.OpenFolder(card);
        }
    }

    private InstanceCardItem? FindCard(object sender)
    {
        var id = (sender as FrameworkElement)?.Tag as string;
        if (string.IsNullOrEmpty(id)) return null;
        return ViewModel.Instances.FirstOrDefault(i => i.Id == id);
    }

    private void ContextPlay_Click(object sender, RoutedEventArgs e)
    {
        var card = FindCard(sender);
        if (card != null) _ = ViewModel.PlayCommand.ExecuteAsync(card);
    }

    private void ContextEdit_Click(object sender, RoutedEventArgs e)
    {
        var card = FindCard(sender);
        if (card != null) NavigateToDetail(card.Id);
    }

    private void ContextOpenFolder_Click(object sender, RoutedEventArgs e)
    {
        InstancesViewModel.OpenFolder(FindCard(sender));
    }

    private async void ContextDelete_Click(object sender, RoutedEventArgs e)
    {
        var card = FindCard(sender);
        if (card == null) return;

        var confirm = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = $"Xóa \"{card.Name}\"?",
            Content = "Toàn bộ thư mục instance sẽ bị xóa khỏi disk. Không thể hoàn tác.",
            PrimaryButtonText = "Xóa",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Close
        };

        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteInstanceAsync(card);
        }
    }

    private void NavigateToDetail(string instanceId)
    {
        try
        {
            var logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vazer", "nav_log.txt");
            System.IO.File.AppendAllText(logPath, $"NavigateToDetail called for {instanceId}\n");
        }
        catch { }

        if (App.MainWindow is MainWindow mw)
        {
            mw.NavigateTo(typeof(InstanceDetailPage), instanceId);
        }
        else
        {
            Frame?.Navigate(typeof(InstanceDetailPage), instanceId);
        }
    }
}
