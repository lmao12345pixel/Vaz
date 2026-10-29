using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vaz.WinUI.Services;
using Vaz.WinUI.ViewModels;

namespace Vaz.WinUI.Views;

/// <summary>
/// Mod Browser as a ContentDialog — opened from Instance Detail.
/// Reuses ModBrowserViewModel; instance context comes from ActiveInstanceService.
/// </summary>
public sealed partial class ModBrowserDialog : ContentDialog
{
    public ModBrowserViewModel ViewModel { get; }

    public ModBrowserDialog(string? instanceId = null)
    {
        ViewModel = ((App)Application.Current).Services
            .GetRequiredService<ModBrowserViewModel>();
        InitializeComponent();

        Loaded += async (_, _) =>
        {
            if (!string.IsNullOrEmpty(instanceId))
            {
                var active = ((App)Application.Current).Services
                    .GetRequiredService<ActiveInstanceService>();
                active.SetCurrent(instanceId);
            }
            await ViewModel.RefreshInstanceContextAsync();
            if (ViewModel.Results.Count == 0)
            {
                await ViewModel.SearchAsync();
            }
        };
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

    private SearchHitRow? _pendingRowForInstall;

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is SearchHitRow row)
        {
            var reqDeps = Models.ModCatalogData.GetRequiredDependencies(row.Slug);
            if (reqDeps.Count > 0)
            {
                _pendingRowForInstall = row;
                var depNames = string.Join(", ", reqDeps.Select(d => char.ToUpper(d[0]) + d.Substring(1)));
                DependencyPromptText.Text = $"Mod '{row.Title}' yêu cầu các thư viện phụ thuộc: {depNames}.\n\nBạn có muốn tự động tải và cài đặt kèm toàn bộ dependencies này để game hoạt động ổn định?";
                DependencyPromptOverlay.Visibility = Visibility.Visible;
                return;
            }

            await ViewModel.InstallAsync(row);
        }
    }

    private void CancelDependencyPrompt_Click(object sender, RoutedEventArgs e)
    {
        DependencyPromptOverlay.Visibility = Visibility.Collapsed;
        _pendingRowForInstall = null;
    }

    private async void ConfirmDependencyPrompt_Click(object sender, RoutedEventArgs e)
    {
        DependencyPromptOverlay.Visibility = Visibility.Collapsed;
        if (_pendingRowForInstall != null)
        {
            var target = _pendingRowForInstall;
            _pendingRowForInstall = null;
            await ViewModel.InstallAsync(target);
        }
    }

    private void IconImage_Failed(object sender, ExceptionRoutedEventArgs e)
    {
        if (sender is Image img) img.Visibility = Visibility.Collapsed;
    }
}
