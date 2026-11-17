using Microsoft.Win32;
using Vaz.WinUI.Models;

namespace Vaz.WinUI.Services;

/// <summary>
/// Đợt 21 — tự nhận diện cấp GPU để bật/tắt hoạt ảnh nặng (glint, gradient loop,
/// composition backdrop). Intel HD Graphics 4600 (driver 2016) → Lite;
/// NVIDIA / AMD / Intel Arc → Full; không xác định → Full (an toàn).
///
/// Không thêm package: đọc trực tiếp registry DriverDesc của TẤT CẢ display adapter
/// (SystemSpecsProvider.Gpu chỉ trả adapter đầu tiên, thường là "Hyper-V Video" trên VM).
/// User có thể override qua Settings.
/// </summary>
public sealed class GpuTierDetector
{
    /// <summary>Instance dùng chung cho XAML/code-behind; App gán singleton DI vào đây.</summary>
    public static GpuTierDetector Current { get; set; } = new();

    private GpuTier _override = GpuTier.Auto;
    private readonly bool _detectedLowTier;

    public GpuTierDetector()
    {
        var saved = GpuTierStore.Load();
        if (saved.HasValue && saved.Value != GpuTier.Auto)
        {
            _override = saved.Value;
            DetectedName = $"Override ({saved.Value})";
            _detectedLowTier = saved.Value == GpuTier.Lite;
            return;
        }

        var detection = DetectPrimaryAdapter();
        DetectedName = detection.Name;
        _detectedLowTier = detection.IsLowTier;
        if (saved.HasValue)
        {
            _override = saved.Value;
        }
    }

    private GpuTierDetector(GpuTier forcedTier)
    {
        _override = forcedTier;
        DetectedName = $"Override ({forcedTier})";
        _detectedLowTier = forcedTier == GpuTier.Lite;
    }

    /// <summary>
    /// Đợt 21 — Đọc file trước. Nếu có giá trị hợp lệ khác Auto -> dùng override (không query GPU).
    /// Nếu không có hoặc Auto -> detect GPU như hiện tại.
    /// </summary>
    public static async Task<GpuTierDetector> DetectAsync()
    {
        var saved = await GpuTierStore.LoadAsync();
        if (saved.HasValue && saved.Value != GpuTier.Auto)
        {
            return new GpuTierDetector(saved.Value);
        }

        var detector = new GpuTierDetector();
        if (saved.HasValue)
        {
            detector.ApplyOverride(saved.Value);
        }
        return detector;
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

    private static (string Name, bool IsLowTier) DetectPrimaryAdapter()
    {
        const string displayClassKey =
            @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

        var names = new List<string>();
        try
        {
            using var root = Registry.LocalMachine.OpenSubKey(displayClassKey);
            if (root != null)
            {
                foreach (var subKeyName in root.GetSubKeyNames())
                {
                    using var adapter = root.OpenSubKey(subKeyName);
                    if (adapter?.GetValue("DriverDesc") is string desc && !string.IsNullOrWhiteSpace(desc))
                    {
                        names.Add(desc.Trim());
                    }
                }
            }
        }
        catch
        {
            // Registry bị chặn / không có key → không đủ căn cứ, mặc định Full.
        }

        // Bỏ adapter ảo (Hyper-V, Basic Display, USB virtual) rồi lấy GPU thật đầu tiên.
        foreach (var name in names)
        {
            if (IsVirtualAdapter(name)) continue;
            return (name, IsLegacyIntelGpu(name));
        }

        return (names.Count > 0 ? names[0] : "Không xác định", false);
    }

    private static bool IsVirtualAdapter(string name) =>
        ContainsAny(name, "Hyper-V Video", "Basic Display Adapter", "Virtual Display", "Mirror", "Remote Display");

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
