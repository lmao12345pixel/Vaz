using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Vaz.WinUI.ViewModels;
using Vazer.Core.Markdown;
using Vazer.Core.Modrinth;

namespace Vaz.WinUI.Views;

/// <summary>
/// Project Detail dialog: load Project qua IModrinthClient.GetProjectAsync,
/// render Body bằng MarkdownLiteParser của Vazer.Core (không tự viết parser).
/// </summary>
public sealed partial class ProjectDetailDialog : ContentDialog
{
    private readonly IModrinthClient _modrinth;
    private readonly ModBrowserViewModel _browser;
    private readonly string _slug;
    private ModrinthProject? _project;

    public ProjectDetailDialog(string slug, string title, ModBrowserViewModel browser)
    {
        _modrinth = ((App)Application.Current).Services.GetRequiredService<IModrinthClient>();
        _browser = browser;
        _slug = slug;
        InitializeComponent();
        TitleText.Text = title;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        LoadingRing.IsActive = true;
        ErrorBar.IsOpen = false;
        BodyText.Blocks.Clear();

        try
        {
            var project = await _modrinth.GetProjectAsync(_slug);
            _project = project;

            TitleText.Text = project.Title;
            AuthorText.Text = project.Slug;
            DownloadsText.Text = SearchHitRow.FormatCount(project.Downloads);

            if (!string.IsNullOrEmpty(project.IconUrl))
            {
                IconImage.Source = new BitmapImage(new Uri(project.IconUrl));
            }

            RenderMarkdown(project.Body);
        }
        catch (Exception ex)
        {
            ErrorBar.IsOpen = true;
            ErrorBar.Message = ex.Message;
        }
        finally
        {
            LoadingRing.IsActive = false;
        }
    }

    /// <summary>
    /// Render markdown body: MarkdownLiteParser.Parse → map token sang Run/Hyperlink/LineBreak.
    /// </summary>
    private void RenderMarkdown(string body)
    {
        BodyText.Blocks.Clear();

        foreach (var paragraph in MarkdownLiteParser.Parse(body))
        {
            var p = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };

            foreach (var token in paragraph.Tokens)
            {
                switch (token.Type)
                {
                    case MarkdownTokenType.Text:
                        p.Inlines.Add(new Run { Text = token.Text });
                        break;

                    case MarkdownTokenType.Bold:
                        p.Inlines.Add(new Run { Text = token.Text, FontWeight = Microsoft.UI.Text.FontWeights.Bold });
                        break;

                    case MarkdownTokenType.Link:
                        // URL malformed (dù parser đã whitelist http/https) → fallback text thường, tránh crash.
                        if (Uri.TryCreate(token.Url, UriKind.Absolute, out var uri))
                        {
                            var link = new Hyperlink { NavigateUri = uri };
                            link.Inlines.Add(new Run { Text = token.Text });
                            p.Inlines.Add(link);
                        }
                        else
                        {
                            p.Inlines.Add(new Run { Text = token.Text });
                        }
                        break;

                    case MarkdownTokenType.LineBreak:
                        p.Inlines.Add(new LineBreak());
                        break;
                }
            }

            if (p.Inlines.Count > 0)
            {
                BodyText.Blocks.Add(p);
            }
        }
    }

    private async void Retry_Click(object sender, RoutedEventArgs e)
    {
        await LoadAsync();
    }

    private async void Install_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (_project == null)
        {
            args.Cancel = true;
            return;
        }

        // Chặn đóng dialog để hiển thị tiến trình ngay trên nút
        var deferral = args.GetDeferral();
        try
        {
            await _browser.InstallCommand.ExecuteAsync(
                new SearchHitRow
                {
                    Hit = new SearchHit
                    {
                        ProjectId = _project.Id,
                        Slug = _project.Slug,
                        Title = _project.Title
                    }
                });
        }
        finally
        {
            deferral.Complete();
        }
    }
}
