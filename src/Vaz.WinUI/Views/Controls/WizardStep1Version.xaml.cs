using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Vaz.WinUI.Helpers;
using Vaz.WinUI.Models;
using Vazer.Core.Versions;

namespace Vaz.WinUI.Views.Controls;

/// <summary>
/// Step 1 wizard "Tạo instance": chọn MC version qua grid cards (search + cover cache).
/// </summary>
public sealed partial class WizardStep1Version : UserControl
{
    private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(150);

    private readonly IMinecraftVersionService _versionService;
    private readonly List<VersionEntry> _allVersions = new();
    private List<VersionEntry> _shownVersions = new();
    private CancellationTokenSource? _searchCts;
    private bool _loaded;
    private int _lastCols;

    /// <summary>State dùng chung với wizard (dialog giữ instance).</summary>
    public WizardState State { get; set; } = new();

    /// <summary>User bấm "Tiếp tục" khi đã chọn version.</summary>
    public event EventHandler? ContinueRequested;

    /// <summary>User bấm "Hủy".</summary>
    public event EventHandler? CancelRequested;

    public WizardStep1Version()
    {
        var app = Application.Current as App;
        _versionService = app?.Services?.GetRequiredService<IMinecraftVersionService>()
            ?? throw new InvalidOperationException("IMinecraftVersionService not registered");
        InitializeComponent();
        Loaded += OnLoaded;
        SearchBox.TextChanged += SearchBox_TextChanged;
        SearchBox.GotFocus += (_, _) => AnimateSearchGlow(true);
        SearchBox.LostFocus += (_, _) => AnimateSearchGlow(false);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        _loaded = true;

        // Staggered entrance cho hero/search/footer (#1, nhẹ vì chỉ 4 phần tử).
        if (Parent is Grid rootGrid)
        {
            foreach (UIElement child in rootGrid.Children)
            {
                UiAnimationHelper.RunStaggeredEntrance(child, 0, fromY: 16, durationMs: 250);
            }
        }

        SetLoading(true, "Đang tải danh sách version...");
        try
        {
            var versions = await _versionService.GetVanillaVersionsAsync();

            // Release trước, rồi snapshot (tối đa 30 bản mới nhất), rồi old_beta/old_alpha.
            var ordered = versions
                .OrderBy(v => TypeRank(v.Type))
                .ThenByDescending(v => v.ReleaseTime)
                .ToList();
            var releases = ordered.Where(v => string.Equals(v.Type, "release", StringComparison.OrdinalIgnoreCase));
            var snapshots = ordered.Where(v => string.Equals(v.Type, "snapshot", StringComparison.OrdinalIgnoreCase)).Take(30);
            var legacy = ordered.Where(v => string.Equals(v.Type, "old_beta", StringComparison.OrdinalIgnoreCase)
                                         || string.Equals(v.Type, "old_alpha", StringComparison.OrdinalIgnoreCase)).Take(20);
            foreach (var v in releases.Concat(snapshots).Concat(legacy))
            {
                _allVersions.Add(new VersionEntry(v));
            }

            if (_allVersions.Count > 0)
            {
                _allVersions[0].IsSelected = true;
                State.SelectedVersion = _allVersions[0].Info.Id;
                ContinueButton.IsEnabled = true;
            }

            RebuildCards(_allVersions);
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void SetLoading(bool loading, string message = "")
    {
        LoadingRing.IsActive = loading;
        LoadingRing.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
        StatusText.Text = message;
        StatusText.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RebuildCards(IEnumerable<VersionEntry> entries)
    {
        _shownVersions = entries.ToList();
        CardGrid.Children.Clear();
        CardGrid.ColumnDefinitions.Clear();
        CardGrid.RowDefinitions.Clear();

        var cols = ComputeColumns();
        _lastCols = cols;
        for (var c = 0; c < cols; c++)
        {
            CardGrid.ColumnDefinitions.Add(new Microsoft.UI.Xaml.Controls.ColumnDefinition { Width = GridLength.Auto });
        }
        var rows = _shownVersions.Count == 0 ? 1 : (int)Math.Ceiling(_shownVersions.Count / (double)cols);
        for (var r = 0; r < rows; r++)
        {
            CardGrid.RowDefinitions.Add(new Microsoft.UI.Xaml.Controls.RowDefinition { Height = GridLength.Auto });
        }

        for (var i = 0; i < _shownVersions.Count; i++)
        {
            var entry = _shownVersions[i];
            var card = new WizardVersionCard { Version = entry.Info, IsSelected = entry.IsSelected };
            card.CardClicked += OnCardClicked;
            Grid.SetColumn(card, i % cols);
            Grid.SetRow(card, i / cols);
            CardGrid.Children.Add(card);

            // #1 Card Staggered Entrance: trượt từ dưới lên 20px + fade, 30ms/thẻ (tối đa 25 thẻ đầu).
            if (i < 25)
            {
                UiAnimationHelper.RunStaggeredEntrance(card, i * 30, fromY: 20, durationMs: 250);
            }
        }
    }

    private int ComputeColumns()
    {
        var available = Math.Max(0, CardScroll.ActualWidth - 12);
        return Math.Clamp((int)((available + 14) / 230), 2, 4);
    }

    private void OnCardClicked(object? sender, MinecraftVersionInfo version)
    {
        try { System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vazer", "nav_log.txt"), $"OnCardClicked version={version.Id}\n"); } catch { }
        // Single-select: bỏ chọn tất cả trước.
        foreach (var entry in _allVersions)
        {
            entry.IsSelected = false;
        }
        foreach (var card in CardGrid.Children.OfType<WizardVersionCard>())
        {
            card.IsSelected = false;
        }

        var selected = _allVersions.FirstOrDefault(e => e.Info == version);
        if (selected != null)
        {
            selected.IsSelected = true;
        }
        if (sender is WizardVersionCard clicked)
        {
            clicked.IsSelected = true;
        }

        State.SelectedVersion = version.Id;
        ContinueButton.IsEnabled = true;
    }

    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchCts?.Cancel();
        var cts = new CancellationTokenSource();
        _searchCts = cts;

        try
        {
            await Task.Delay(SearchDebounce, cts.Token);
            var query = SearchBox.Text.Trim();

            IEnumerable<VersionEntry> filtered = _allVersions;
            if (!string.IsNullOrEmpty(query))
            {
                filtered = _allVersions.Where(v => v.Info.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
                                                || (v.Info.Type?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));
            }
            var list = filtered.ToList();

            // #7 Search Filter: fade-out cũ → rebuild → fade-in mới.
            if (AppThemeManager.IsReduceMotion)
            {
                RebuildCards(list);
                return;
            }
            await AnimateSearchFilterAsync(list);
        }
        catch (OperationCanceledException)
        {
            // debounce: gõ tiếp → bỏ qua lần này
        }
    }

    private Task AnimateSearchFilterAsync(List<VersionEntry> list)
    {
        var tcs = new TaskCompletionSource<bool>();
        var sbOut = new Storyboard();
        var animOut = new DoubleAnimation
        {
            To = 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(100)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(animOut, CardGrid);
        Storyboard.SetTargetProperty(animOut, nameof(UIElement.Opacity));
        sbOut.Children.Add(animOut);
        sbOut.Completed += (_, _) => tcs.TrySetResult(true);
        sbOut.Begin();

        return tcs.Task.ContinueWith(_ =>
        {
            RebuildCards(list);
            var sbIn = new Storyboard();
            var animIn = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(animIn, CardGrid);
            Storyboard.SetTargetProperty(animIn, nameof(UIElement.Opacity));
            sbIn.Children.Add(animIn);
            sbIn.Begin();
        }, TaskContinuationOptions.ExecuteSynchronously);
    }

    private void CardScroll_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Responsive 2-4 cột: dựng lại grid nếu số cột thay đổi.
        var cols = ComputeColumns();
        if (cols == _lastCols) return;
        if (_shownVersions.Count > 0) RebuildCards(_shownVersions);
    }

    private void AnimateSearchGlow(bool focused)
    {
        // #6 Search Focus: border glow accent (150ms).
        var target = Windows.UI.Color.FromArgb(255, 60, 66, 74); // khớp CardStrokeColorDefaultBrush (dark-ish neutral)
        if (focused && Application.Current?.Resources.TryGetValue("VazAccentBrush", out var accent) == true && accent is SolidColorBrush accentBrush)
        {
            target = accentBrush.Color;
        }

        var sb = new Storyboard();
        var animation = new ColorAnimation
        {
            To = target,
            Duration = new Duration(TimeSpan.FromMilliseconds(150)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(animation, SearchGlow);
        Storyboard.SetTargetProperty(animation, "(Border.BorderBrush).(SolidColorBrush.Color)");
        sb.Children.Add(animation);
        sb.Begin();
    }

    private void ContinueButton_Click(object sender, RoutedEventArgs e)
    {
        try { System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vazer", "nav_log.txt"), $"ContinueButton_Click selected={State.SelectedVersion}\n"); } catch { }
        if (!string.IsNullOrEmpty(State.SelectedVersion))
        {
            ContinueRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
        => CancelRequested?.Invoke(this, EventArgs.Empty);

    private static int TypeRank(string? type) => (type?.Trim().ToLowerInvariant()) switch
    {
        "release" => 0,
        "snapshot" => 1,
        "old_beta" => 2,
        "old_alpha" => 3,
        _ => 4
    };

    private sealed class VersionEntry
    {
        public VersionEntry(MinecraftVersionInfo info) { Info = info; }
        public MinecraftVersionInfo Info { get; }
        public bool IsSelected { get; set; }
    }
}
