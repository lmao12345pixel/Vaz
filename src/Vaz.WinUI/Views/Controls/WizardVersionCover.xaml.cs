using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Vaz.WinUI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Vaz.WinUI.Views.Controls;

/// <summary>
/// Ô cover art cho một MC version: gradient + version text fallback ngay lập tức,
/// Image overlay khi tải từ cache/wiki thành công (không block UI).
/// </summary>
public sealed partial class WizardVersionCover : UserControl
{
    private static readonly System.Collections.Generic.Dictionary<string, string?> ResolvedCache = new(StringComparer.OrdinalIgnoreCase);

    public WizardVersionCover()
    {
        InitializeComponent();
    }

    /// <summary>Tên version (vd "1.21.1") — quyết định URL + text fallback.</summary>
    public static readonly DependencyProperty VersionIdProperty = DependencyProperty.Register(
        nameof(VersionId), typeof(string), typeof(WizardVersionCover),
        new PropertyMetadata(null, OnVersionIdChanged));

    public string VersionId
    {
        get => (string)GetValue(VersionIdProperty);
        set => SetValue(VersionIdProperty, value);
    }

    /// <summary>True nếu cover tải được từ wiki (false = đang dùng gradient fallback).</summary>
    public bool CoverLoaded { get; private set; }

    private static async void OnVersionIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WizardVersionCover self) return;
        if (e.NewValue is not string id || string.IsNullOrWhiteSpace(id))
        {
            self.ApplyFallback(null);
            return;
        }

        self.FallbackText.Text = id;
        self.ApplyFallback(id);

        if (        ResolvedCache.TryGetValue(id, out var cached))
        {
            if (cached != null) self.ApplyCover(cached);
            return;
        }

        // Cap cache để tránh leak RAM khi mở wizard nhiều lần (max 200 entry)
        if (ResolvedCache.Count >= 200) ResolvedCache.Clear();

        try
        {
            var app = Application.Current as App;
            var service = app?.Services?.GetService<CoverImageCacheService>();
            var path = service != null ? await service.GetCoverPathAsync(id) : null;
            ResolvedCache[id] = path;
            if (path != null) self.ApplyCover(path);
        }
        catch
        {
            ResolvedCache[id] = null;
        }
    }

    private void ApplyFallback(string? versionId)
    {
        CoverLoaded = false;
        // Gradient per-version seed (hash nhẹ vào 3 màu) — vẫn là fallback an toàn khi wiki 403.
        var hash = string.IsNullOrEmpty(versionId) ? 0 : (uint)versionId.GetHashCode();
        var c1 = Windows.UI.Color.FromArgb(255, (byte)(40 + (hash & 0x3F)), (byte)(90 + ((hash >> 6) & 0x4F)), (byte)(60 + ((hash >> 12) & 0x4F)));
        var c2 = Windows.UI.Color.FromArgb(255, (byte)(15 + (hash & 0x1F)), (byte)(45 + ((hash >> 6) & 0x2F)), (byte)(25 + ((hash >> 12) & 0x2F)));
        GradientFallback.Background = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0),
            EndPoint = new Windows.Foundation.Point(1, 1),
            GradientStops =
            {
                new GradientStop { Color = c1, Offset = 0 },
                new GradientStop { Color = c2, Offset = 1 }
            }
        };
        CoverImage.Visibility = Visibility.Collapsed;
        FallbackText.Visibility = Visibility.Visible;
    }

    private void ApplyCover(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                CoverImage.Source = new BitmapImage(new Uri(path, UriKind.Absolute));
                CoverLoaded = true;
                CoverImage.Visibility = Visibility.Visible;
                FallbackText.Visibility = Visibility.Collapsed;
            }
        }
        catch
        {
            ApplyFallback(VersionId);
        }
    }
}
