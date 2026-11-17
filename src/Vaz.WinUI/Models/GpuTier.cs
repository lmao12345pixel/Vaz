using Microsoft.UI.Xaml.Data;

namespace Vaz.WinUI.Models;

/// <summary>
/// Cài đặt hiệu ứng do user chọn trong Settings. Auto để <see cref="Vaz.WinUI.Services.GpuTierDetector"/>
/// tự quyết định theo GPU; Full/Lite là override thủ công.
/// </summary>
public enum GpuTier
{
    Auto,
    Full,
    Lite
}

/// <summary>
/// Converter phục vụ Phase 1 (Design System): GpuTier → nhãn hiển thị trong Settings.
/// Đặt trong Models/GpuTier.cs để không sửa Helpers/Converters.cs (ngoài PHẠM VI).
/// </summary>
public sealed class GpuTierToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is GpuTier tier
            ? Describe(tier)
            : Describe(GpuTier.Auto);

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is string s && s.Contains("Đầy đủ", StringComparison.Ordinal)
            ? GpuTier.Full
            : GpuTier.Auto;

    private static string Describe(GpuTier tier) => tier switch
    {
        GpuTier.Full => "Đầy đủ (bật mọi hoạt ảnh)",
        GpuTier.Lite => "Nhẹ (tắt glint, gradient loop, backdrop)",
        _ => $"Tự động · {Vaz.WinUI.Services.GpuTierDetector.Current.Summary}"
    };
}
