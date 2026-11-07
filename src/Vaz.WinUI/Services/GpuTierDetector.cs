using Vaz.WinUI.Helpers;
using Vaz.WinUI.Models;

namespace Vaz.WinUI.Services;

/// <summary>
/// Đợt 21 — tự nhận diện cấp GPU để bật/tắt hoạt ảnh nặng (glint, gradient loop,
/// composition backdrop). Intel HD Graphics 4600 (driver 2016) → Lite;
/// NVIDIA / AMD / Intel Arc → Full; không xác định → Full (an toàn).
///
/// Không thêm package: tái dùng <see cref="SystemSpecsProvider.Gpu"/> (đọc registry
/// DriverDesc của display adapter). User có thể override qua Settings.
/// </summary>
public sealed class GpuTierDetector
{
    /// <summary>Instance dùng chung cho XAML/code-behind; App gán singleton DI vào đây.</summary>
    public static GpuTierDetector Current { get; set; } = new();

    private GpuTier _override = GpuTier.Auto;
    private readonly bool _detectedLowTier;

    public GpuTierDetector()
    {
        DetectedName = SystemSpecsProvider.Gpu;
        _detectedLowTier = IsLegacyIntelGpu(DetectedName);
    }

    /// <summary>Tên GPU đọc được (hoặc "Không xác định").</summary>
    public string DetectedName { get; }

    /// <summary>True khi GPU bị xếp vào nhóm cần tắt hiệu ứng nặng.</summary>
    public bool IsLowTierDetected => _detectedLowTier;

    /// <summary>True khi được phép chạy hoạt ảnh nặng (sau khi áp override).</summary>
    public bool IsHeavyEffectsEnabled => _override switch
    {
        GpuTier.Full => true,
        GpuTier.Lite => false,
        _ => !_detectedLowTier
    };

    /// <summary>Dòng trạng thái hiển thị trong Settings.</summary>
    public string Summary =>
        $"GPU: {DetectedName} · {(_detectedLowTier ? "Lite" : "Full")}" +
        $" · đang dùng {(IsHeavyEffectsEnabled ? "Full" : "Lite")}";

    /// <summary>Áp lựa chọn của user (Auto / Full / Lite).</summary>
    public void ApplyOverride(GpuTier tier) => _override = tier;

    private static bool IsLegacyIntelGpu(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        // GPU rời / GPU Arc mới → Full ngay, kể cả chuỗi có chứa "Intel".
        if (ContainsAny(name, "NVIDIA", "GeForce", "RTX", "GTX", "AMD", "Radeon", "Intel(R) Arc", "Intel Arc"))
        {
            return false;
        }

        // Intel HD Graphics 4600 / 4000 / UHD Graphics (iGPU đời cũ) → Lite.
        return ContainsAny(name, "Intel") &&
               ContainsAny(name, "HD Graphics", "UHD Graphics", "HD 4600");
    }

    private static bool ContainsAny(string value, params string[] tokens)
    {
        foreach (var token in tokens)
        {
            if (value.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
