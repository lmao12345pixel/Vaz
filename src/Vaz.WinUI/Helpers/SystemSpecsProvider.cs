using System.Runtime.InteropServices;

namespace Vaz.WinUI.Helpers;

/// <summary>
/// Thông số hệ thống cho section "Thông tin &amp; Chẩn đoán" (SET-AC-09).
/// Không thêm package ngoài repo: OS/CPU/RAM lấy từ BCL, GPU đọc registry
/// (best-effort; máy không đọc được → ghi "Không xác định" thay vì bịa).
/// </summary>
public static class SystemSpecsProvider
{
    private static readonly Lazy<string> LazyOs = new(ResolveOs);
    private static readonly Lazy<string> LazyCpu = new(ResolveCpu);
    private static readonly Lazy<string> LazyGpu = new(ResolveGpu);
    private static readonly Lazy<string> LazyRam = new(ResolveRam);

    public static string Os => LazyOs.Value;

    public static string Cpu => LazyCpu.Value;

    public static string Gpu => LazyGpu.Value;

    public static string Ram => LazyRam.Value;

    /// <summary>Mô tả Java đang cấu hình; trống → Java tự động.</summary>
    public static string Java(string? javaPath)
    {
        if (string.IsNullOrWhiteSpace(javaPath))
        {
            return "Tự động (Java 21 LTS)";
        }

        var fileName = Path.GetFileName(javaPath);
        return string.IsNullOrEmpty(fileName) ? javaPath : $"{fileName} — {javaPath}";
    }

    private static string ResolveOs()
    {
        var architecture = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";

        // RuntimeInformation bị manifest cũ "khoá" ở 10.0.19041; registry cho build thật.
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            var product = key?.GetValue("ProductName") as string;
            var build = key?.GetValue("CurrentBuild") as string;

            if (!string.IsNullOrWhiteSpace(product) && int.TryParse(build, out var buildNumber))
            {
                // Windows 11 vẫn để ProductName "Windows 10 ..." trong registry.
                var name = buildNumber >= 22000
                    ? product.Replace("Windows 10", "Windows 11", StringComparison.OrdinalIgnoreCase)
                    : product;

                return $"{name} (Build {buildNumber}) ({architecture})";
            }
        }
        catch
        {
            // Registry bị chặn — dùng mô tả BCL bên dưới.
        }

        return $"{RuntimeInformation.OSDescription.Trim()} ({architecture})";
    }

    private static string ResolveCpu()
    {
        var identifier = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return $"{Environment.ProcessorCount} logical processors";
        }

        return $"{identifier.Trim()} ({Environment.ProcessorCount} threads)";
    }

    private static string ResolveGpu()
    {
        // DriverDesc trong class key của display adapters thường là tên GPU thân thiện.
        const string displayClassKey =
            @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

        try
        {
            for (var index = 0; index < 4; index++)
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey($@"{displayClassKey}\{index:0000}");
                if (key?.GetValue("DriverDesc") is string name && !string.IsNullOrWhiteSpace(name))
                {
                    return name.Trim();
                }
            }
        }
        catch
        {
            // Registry bị chặn / không có key — không đủ căn cứ để báo tên GPU.
        }

        return "Không xác định";
    }

    private static string ResolveRam()
    {
        // Tổng RAM vật lý (GC.GetGCMemoryInfo trả "available" nên thấp hơn thực tế trên VM).
        try
        {
            if (GetPhysicallyInstalledSystemMemory(out var kilobytes) && kilobytes > 0)
            {
                return $"{kilobytes / 1024d / 1024d:0.0} GB";
            }
        }
        catch
        {
            // P/Invoke bị chặn — fallback bên dưới.
        }

        var bytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        if (bytes <= 0)
        {
            return "Không xác định";
        }

        var gib = bytes / 1024d / 1024d / 1024d;
        return $"{gib:0.0} GB";
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetPhysicallyInstalledSystemMemory(out long totalMemoryInKilobytes);
}
