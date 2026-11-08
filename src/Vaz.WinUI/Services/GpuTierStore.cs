using System.IO;
using System.Text.Json;
using Vaz.WinUI.Models;

namespace Vaz.WinUI.Services;

/// <summary>
/// Đợt 21 — lưu trữ bền vững lựa chọn GpuTier (Auto | Full | Lite) vào file JSON tại %APPDATA%/Vazer/mkvibe-gputier.json.
/// </summary>
public static class GpuTierStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Vazer",
        "mkvibe-gputier.json");

    /// <summary>Đọc đồng bộ cấp GpuTier đã lưu từ file JSON.</summary>
    public static GpuTier? Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            var text = File.ReadAllText(FilePath).Trim().Trim('"');
            if (Enum.TryParse<GpuTier>(text, ignoreCase: true, out var tier))
            {
                return tier;
            }
        }
        catch
        {
            // Fallback an toàn nếu file bị hỏng hoặc không có quyền truy cập
        }
        return null;
    }

    /// <summary>Đọc bất đồng bộ cấp GpuTier đã lưu từ file JSON.</summary>
    public static async Task<GpuTier?> LoadAsync()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            var text = (await File.ReadAllTextAsync(FilePath)).Trim().Trim('"');
            if (Enum.TryParse<GpuTier>(text, ignoreCase: true, out var tier))
            {
                return tier;
            }
        }
        catch
        {
        }
        return null;
    }

    /// <summary>Ghi đồng bộ cấp GpuTier vào file JSON ("Auto" | "Full" | "Lite").</summary>
    public static void Save(GpuTier tier)
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(FilePath, $"\"{tier}\"");
        }
        catch
        {
        }
    }

    /// <summary>Ghi bất đồng bộ cấp GpuTier vào file JSON ("Auto" | "Full" | "Lite").</summary>
    public static async Task SaveAsync(GpuTier tier)
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            await File.WriteAllTextAsync(FilePath, $"\"{tier}\"");
        }
        catch
        {
        }
    }
}
