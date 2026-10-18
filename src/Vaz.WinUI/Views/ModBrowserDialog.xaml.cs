using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Vaz.WinUI.ViewModels;

namespace Vaz.WinUI.Views;

/// <summary>
/// Mod Browser as a ContentDialog — opened from Instance Detail.
/// Reuses ModBrowserViewModel; instance context comes from ActiveInstanceService (already set).
/// </summary>
public sealed partial class ModBrowserDialog : ContentDialog
{
    public ModBrowserViewModel ViewModel { get; }

    public ModBrowserDialog()
    {
        ViewModel = ((App)Application.Current).Services
            .GetRequiredService<ModBrowserViewModel>();
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.RefreshInstanceContextAsync();
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
        ViewModel.SearchText = sender.Text;
        _ = ViewModel.SearchCommand.ExecuteAsync(null);
    }

    private async void Card_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is SearchHitRow row)
        {
            var detail = new ProjectDetailDialog(row.Slug, row.Title, ViewModel);
            detail.XamlRoot = XamlRoot;
            await detail.ShowAsync();
        }
    }

    private void Install_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is SearchHitRow row)
        {
            _ = ViewModel.InstallCommand.ExecuteAsync(row);
        }
    }

    private void IconImage_Failed(object sender, ExceptionRoutedEventArgs e)
    {
        if (sender is Image img) img.Visibility = Visibility.Collapsed;
    }
}
