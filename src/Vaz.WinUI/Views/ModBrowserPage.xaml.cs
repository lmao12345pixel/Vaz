using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Vaz.WinUI.ViewModels;
using Windows.Storage.Pickers;

namespace Vaz.WinUI.Views;

/// <summary>
/// Mod Browser: search Modrinth, filter, install, mở Project Detail dialog.
/// </summary>
public sealed partial class ModBrowserPage : Page
{
    public ModBrowserViewModel ViewModel { get; }

    public ModBrowserPage()
    {
        ViewModel = ((App)Application.Current).Services
            .GetRequiredService<ModBrowserViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = ViewModel.RefreshInstanceContextAsync();
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ViewModel.SearchText = sender.Text;
            ViewModel.OnSearchTextChangedDebounced();
        }
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        ViewModel.SearchText = args.QueryText;
        if (ViewModel.SearchCommand.CanExecute(null))
        {
            ViewModel.SearchCommand.Execute(null);
        }
    }

    private void Card_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is SearchHitRow row)
        {
            e.Handled = true;
            _ = OpenProjectDetailAsync(row);
        }
    }

    private async System.Threading.Tasks.Task OpenProjectDetailAsync(SearchHitRow row)
    {
        var dialog = new ProjectDetailDialog(row.Slug, row.Title, ViewModel);
        dialog.XamlRoot = XamlRoot;
        await dialog.ShowAsync();
    }

    private void IconImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        if (sender is Image img)
        {
            img.Source = null;
            img.Visibility = Visibility.Collapsed;
        }
    }
}
