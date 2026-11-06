using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Vaz.WinUI.Helpers;
using Vaz.WinUI.Models;
using Vazer.Core.Loaders;
using Vazer.Core.Versions;

namespace Vaz.WinUI.Views.Controls;

/// <summary>
/// Step 2 wizard "Tạo instance": chọn loader (4 card) + loader version + live preview.
/// </summary>
public sealed partial class WizardStep2Loader : UserControl
{
    private readonly IMinecraftVersionService _versionService;
    private CancellationTokenSource? _versionsCts;

    public WizardStep2Loader()
    {
        var app = Application.Current as App;
        _versionService = app?.Services?.GetRequiredService<IMinecraftVersionService>()
            ?? throw new InvalidOperationException("IMinecraftVersionService not registered");
        InitializeComponent();
        Loaded += OnLoaded;
        NameBox.TextChanged += (_, _) => ValidateCreate();
        LoaderVersionBox.SelectionChanged += LoaderVersionBox_SelectionChanged;
        SizeChanged += OnSizeChanged;
    }

    /// <summary>State dùng chung với wizard (dialog giữ instance).</summary>
    public WizardState State { get; set; } = new();

    /// <summary>Breadcrumb "Bước 2/2 · Minecraft x".</summary>
    public void SetBreadcrumb(string text) => BreadcrumbText.Text = text;

    /// <summary>Tên instance user nhập.</summary>
    public string InstanceName => NameBox.Text;

    /// <summary>Bật/tắt nút "Tạo Instance" (dùng khi đang cài).</summary>
    public void SetCreateEnabled(bool enabled) => CreateButton.IsEnabled = enabled;

    /// <summary>User bấm "Quay lại".</summary>
    public event EventHandler? BackRequested;

    /// <summary>User bấm "Hủy".</summary>
    public event EventHandler? CancelRequested;

    /// <summary>User bấm "Tạo Instance".</summary>
    public event EventHandler? CreateRequested;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        BreadcrumbText.Text = $"Bước 2/3 · Minecraft {State.SelectedVersion ?? "?"}";
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            NameBox.Text = State.SelectedVersion != null ? $"Minecraft {State.SelectedVersion}" : "New Instance";
        }

        // Staggered entrance cho hero + loader grid (#1).
        foreach (UIElement child in ((Grid)Content).Children)
        {
            UiAnimationHelper.RunStaggeredEntrance(child, 0, fromY: 16, durationMs: 250);
        }

        UpdatePreview();
        UpdateLoaderCardStyles();
        UpdatePreviewVisibility();

        if (string.IsNullOrWhiteSpace(State.SelectedLoader))
        {
            State.SelectedLoader = "Fabric";
            _ = RefreshLoaderVersionAreaAsync("Fabric");
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        => UpdatePreviewVisibility();

    private void UpdatePreviewVisibility()
    {
        // Live preview panel bên phải hiển thị khi dialog có độ rộng hợp lý (hoặc khi chưa đo thì mặc định Visible).
        PreviewPanel.Visibility = ActualWidth == 0 || ActualWidth >= 650 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void LoaderButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string loader }) return;
        await SelectLoaderAsync(loader);
    }

    private async Task SelectLoaderAsync(string loader)
    {
        State.SelectedLoader = loader;
        State.SelectedLoaderVersion = null;

        // #5 Loader Logo Spin: logo xoay 360° (600ms CubicEase EaseInOut).
        AnimateLogoSpin(loader switch
        {
            "Vanilla" => VanillaLogo,
            "Fabric" => FabricLogo,
            "Quilt" => QuiltLogo,
            "NeoForge" => NeoForgeLogo,
            "Forge" => ForgeLogo,
            _ => (TextBlock?)null
        });

        UpdateLoaderCardStyles();
        await RefreshLoaderVersionAreaAsync(loader);
        UpdatePreview();
        ValidateCreate();
    }

    /// <summary>
    /// Preselect loader (+ loader version nếu có) — dùng cho chế độ "Đổi phiên bản".
    /// </summary>
    public async Task SelectLoaderAndVersionAsync(string loaderType, string? loaderVersion)
    {
        // LoaderCards dùng tag hiển thị ("Fabric"...), Instance lưu lowercase ("fabric") — so khớp case-insensitive.
        var match = LoaderCards().FirstOrDefault(c => string.Equals(c.Tag, loaderType, StringComparison.OrdinalIgnoreCase));
        var loader = match.Tag;
        if (loader == null)
        {
            loader = string.IsNullOrWhiteSpace(loaderType)
                ? "Fabric"
                : char.ToUpperInvariant(loaderType[0]) + loaderType[1..].ToLowerInvariant();
        }

        await SelectLoaderAsync(loader);

        if (!string.IsNullOrWhiteSpace(loaderVersion) &&
            LoaderVersionBox.ItemsSource is System.Collections.IEnumerable items)
        {
            var exists = items.Cast<object>()
                .Select(o => o?.ToString())
                .FirstOrDefault(s => string.Equals(s, loaderVersion, StringComparison.OrdinalIgnoreCase));
            if (exists != null)
            {
                LoaderVersionBox.SelectedItem = exists;
                State.SelectedLoaderVersion = exists;
                UpdatePreview();
                ValidateCreate();
            }
        }
    }

    private void AnimateLogoSpin(TextBlock? logo)
    {
        if (logo == null) return;
        if (AppThemeManager.IsReduceMotion) return;

        logo.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        var transform = new RotateTransform { Angle = 0 };
        logo.RenderTransform = transform;

        var sb = new Storyboard();
        var anim = new DoubleAnimation
        {
            From = 0,
            To = 360,
            Duration = new Duration(TimeSpan.FromMilliseconds(600)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTarget(anim, transform);
        Storyboard.SetTargetProperty(anim, nameof(RotateTransform.Angle));
        sb.Children.Add(anim);
        sb.Begin();
    }

    private void UpdateLoaderCardStyles()
    {
        var resources = Application.Current?.Resources;
        if (resources is null)
        {
            return;
        }

        var selected = State.SelectedLoader;
        foreach (var (button, tag) in LoaderCards())
        {
            var isSelected = string.Equals(selected, tag, StringComparison.OrdinalIgnoreCase);
            button.BorderBrush = isSelected
                ? (Brush)resources["VazAccentBrush"]
                : (Brush)resources["VazShellBackgroundBrush"];
            button.BorderThickness = new Thickness(isSelected ? 2 : 1);
            if (!isSelected && Application.Current?.Resources.TryGetValue("CardStrokeColorDefaultBrush", out var stroke) == true && stroke is Brush s)
            {
                button.BorderBrush = s;
            }
        }
    }

    private IEnumerable<(Button Button, string Tag)> LoaderCards() => new[]
    {
        (VanillaButton, "Vanilla"),
        (FabricButton, "Fabric"),
        (QuiltButton, "Quilt"),
        (NeoForgeButton, "NeoForge"),
        (ForgeButton, "Forge")
    };

    private async Task RefreshLoaderVersionAreaAsync(string loader)
    {
        LoaderVersionBox.ItemsSource = null;
        NoLoaderVersionsText.Visibility = Visibility.Collapsed;

        if (string.Equals(loader, "Vanilla", StringComparison.OrdinalIgnoreCase))
        {
            VanillaNote.IsOpen = true;
            LoaderVersionPanel.Visibility = Visibility.Collapsed;
            return;
        }

        VanillaNote.IsOpen = false;
        LoaderVersionPanel.Visibility = Visibility.Visible;
        AnimateLoaderVersionSlide(true); // slide-down + fade (#8 tương tự dropdown mở)

        if (string.Equals(loader, "Forge", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(State.SelectedVersion) &&
            !ForgeInstaller.IsHeadlessSupported(State.SelectedVersion))
        {
            NoLoaderVersionsText.Text = "Forge headless chỉ hỗ trợ MC 1.13+.";
            NoLoaderVersionsText.Visibility = Visibility.Visible;
            State.SelectedLoaderVersion = null;
            ValidateCreate();
            return;
        }

        // Không assume version mặc định: thiếu MC version thì không query loader versions.
        var mcVersion = State.SelectedVersion;
        if (string.IsNullOrWhiteSpace(mcVersion))
        {
            NoLoaderVersionsText.Text = "Chọn phiên bản Minecraft trước.";
            NoLoaderVersionsText.Visibility = Visibility.Visible;
            State.SelectedLoaderVersion = null;
            ValidateCreate();
            return;
        }

        // Tải danh sách loader version
        _versionsCts?.Cancel();
        var cts = new CancellationTokenSource();
        _versionsCts = cts;

        SetVersionLoading(true);
        try
        {
            var versions = await _versionService.GetLoaderVersionsAsync(
                loader.ToLowerInvariant(),
                mcVersion,
                cts.Token);

            var stable = versions
                .Where(v => v.Stable)
                .Select(v => v.Version)
                .OrderByDescending(v => v, new LoaderVersionComparer())
                .ToList();
            var beta = versions
                .Where(v => !v.Stable && v.Installable)
                .Select(v => v.Version)
                .OrderByDescending(v => v, new LoaderVersionComparer())
                .ToList();
            var items = stable.Concat(beta).ToList();

            LoaderVersionBox.ItemsSource = items;
            if (items.Count == 0)
            {
                NoLoaderVersionsText.Visibility = Visibility.Visible;
                State.SelectedLoaderVersion = null;
            }
            else
            {
                LoaderVersionBox.SelectedItem = items[0];
                State.SelectedLoaderVersion = items[0];
            }
        }
        catch (OperationCanceledException)
        {
            // user đổi loader nhanh → bỏ qua
        }
        finally
        {
            SetVersionLoading(false);
        }

        ValidateCreate();
    }

    private void AnimateLoaderVersionSlide(bool show)
    {
        if (AppThemeManager.IsReduceMotion) return;

        LoaderVersionPanel.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.0);
        var transform = new TranslateTransform { Y = show ? -12 : 0 };
        LoaderVersionPanel.RenderTransform = transform;
        LoaderVersionPanel.Opacity = show ? 0.0 : 1.0;

        var sb = new Storyboard();
        var s = new Duration(TimeSpan.FromMilliseconds(220));
        var animY = new DoubleAnimation { To = 0, Duration = s, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        Storyboard.SetTarget(animY, transform);
        Storyboard.SetTargetProperty(animY, nameof(TranslateTransform.Y));
        var animOp = new DoubleAnimation { To = 1, Duration = s, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        Storyboard.SetTarget(animOp, LoaderVersionPanel);
        Storyboard.SetTargetProperty(animOp, nameof(UIElement.Opacity));
        sb.Children.Add(animY);
        sb.Children.Add(animOp);
        sb.Begin();
    }

    private void SetVersionLoading(bool loading)
    {
        LoaderLoadingRing.IsActive = loading;
        LoaderLoadingRing.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
        LoaderVersionBox.IsEnabled = !loading;
    }

    private void LoaderVersionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        State.SelectedLoaderVersion = LoaderVersionBox.SelectedItem as string;
        UpdatePreview();
        ValidateCreate();
    }

    private void UpdatePreview()
    {
        PreviewVersion.Text = State.SelectedVersion != null
            ? $"Minecraft {State.SelectedVersion}"
            : "Minecraft";

        var loader = State.SelectedLoader;
        if (string.IsNullOrWhiteSpace(loader))
        {
            PreviewLoader.Text = "Loader";
            PreviewSize.Text = "~214 MB";
            PreviewHint.Text = "Chọn loader để xem tóm tắt cấu hình.";
            return;
        }

        var loaderLine = loader switch
        {
            "Vanilla" => "Vanilla (không mod)",
            "Fabric" => $"Fabric {State.SelectedLoaderVersion ?? ""}".Trim(),
            "Quilt" => $"Quilt {State.SelectedLoaderVersion ?? ""}".Trim(),
            "NeoForge" => $"NeoForge {State.SelectedLoaderVersion ?? ""}".Trim(),
            "Forge" => $"Forge {State.SelectedLoaderVersion ?? ""}".Trim(),
            _ => loader
        };
        PreviewLoader.Text = loaderLine;

        var mb = loader switch
        {
            "Vanilla" => 214,
            "Fabric" => 226,
            "Quilt" => 231,
            "NeoForge" => 282,
            "Forge" => 275,
            _ => 214
        };
        PreviewSize.Text = $"~{mb} MB";
        PreviewHint.Text = loader switch
        {
            "NeoForge" => "NeoForge cần 1.20.2+ (build chính thức). Lần tạo đầu sẽ tải thêm thư viện đã vá.",
            "Forge" => "Forge headless chỉ hỗ trợ MC 1.13+. Lần tạo đầu sẽ tải installer và patch client.",
            "Vanilla" => "Bản vanilla gốc từ Mojang, không có mod.",
            _ => "Loader nhẹ, khởi động nhanh."
        };
    }

    private void ValidateCreate()
    {
        var loader = State.SelectedLoader;
        var mc = State.SelectedVersion ?? "";

        if (string.Equals(loader, "Forge", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(mc) &&
            !ForgeInstaller.IsHeadlessSupported(mc))
        {
            CreateButton.IsEnabled = false;
            return;
        }

        var loaderOk = loader == null
            ? false
            : string.Equals(loader, "Vanilla", StringComparison.OrdinalIgnoreCase)
                || !string.IsNullOrWhiteSpace(State.SelectedLoaderVersion);
        var nameOk = !string.IsNullOrWhiteSpace(NameBox.Text);
        var mcOk = !string.IsNullOrWhiteSpace(mc);
        CreateButton.IsEnabled = mcOk && loaderOk && nameOk;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
        => BackRequested?.Invoke(this, EventArgs.Empty);

    private void CancelButton_Click(object sender, RoutedEventArgs e)
        => CancelRequested?.Invoke(this, EventArgs.Empty);

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        if (CreateButton.IsEnabled)
        {
            State.SelectedLoaderVersion = LoaderVersionBox.SelectedItem as string ?? State.SelectedLoaderVersion;
            CreateRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
