using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Vazer.Core.GameOptions;

/// <summary>
/// Quản lý phân tích và đồng bộ hóa dòng 'resourcePacks' trong options.txt của Minecraft (RPS-AC-02).
/// Đảm bảo ghi nguyên tử (atomic), giữ nguyên các mục hệ thống (vanilla, mod packs),
/// và ánh xạ đúng thứ tự ưu tiên (Minecraft đọc từ trái sang phải, gói cuối mảng đè lên gói trước).
/// </summary>
public static class ResourcePackOptionsHelper
{
    public const string DefaultVanillaEntry = "\"vanilla\"";

    /// <summary>
    /// Phân tích danh sách resource pack từ chuỗi dòng 'resourcePacks:[...]'.
    /// Hỗ trợ cả định dạng JSON chuẩn lẫn định dạng legacy không có dấu ngoặc kép.
    /// </summary>
    public static List<string> ParseResourcePacksFromLine(string line)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(line)) return result;

        var colonIdx = line.IndexOf(':');
        if (colonIdx < 0) return result;

        var arrayPart = line[(colonIdx + 1)..].Trim();
        if (arrayPart.StartsWith('[') && arrayPart.EndsWith(']'))
        {
            arrayPart = arrayPart[1..^1].Trim();
        }

        if (string.IsNullOrWhiteSpace(arrayPart)) return result;

        // Thử parse qua JsonDocument nếu là mảng JSON hợp lệ
        try
        {
            using var doc = JsonDocument.Parse($"[{arrayPart}]");
            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                var val = elem.GetString();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    result.Add(val);
                }
            }
            return result;
        }
        catch
        {
            // Fallback sang parse thủ công theo dấu phẩy nếu tệp cấu hình không theo chuẩn JSON
            var parts = arrayPart.Split(',');
            foreach (var part in parts)
            {
                var trimmed = part.Trim().Trim('\"', '\'');
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    result.Add(trimmed);
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Cập nhật nội dung options.txt:
    /// - Giữ nguyên các mục không phải resource pack người dùng (vanilla, modded packs).
    /// - Đưa các pack đang bật vào mảng theo thứ tự Minecraft (pack ưu tiên #1 của UI nằm ở cuối mảng).
    /// </summary>
    public static string UpdateResourcePacksLine(string optionsContent, IEnumerable<string> activePackFileNames)
    {
        var lines = (optionsContent ?? string.Empty)
            .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
            .ToList();

        var preservedNonAppEntries = new List<string>();
        int existingLineIndex = -1;

        for (int i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("resourcePacks:", StringComparison.OrdinalIgnoreCase))
            {
                existingLineIndex = i;
                var existingEntries = ParseResourcePacksFromLine(trimmed);
                foreach (var entry in existingEntries)
                {
                    // Các mục không bắt đầu bằng "file/" là vanilla hoặc mod packs cần bảo toàn
                    if (!entry.StartsWith("file/", StringComparison.OrdinalIgnoreCase))
                    {
                        preservedNonAppEntries.Add(entry);
                    }
                }
                break;
            }
        }

        // Đảm bảo luôn có "vanilla" nếu chưa có
        if (!preservedNonAppEntries.Any(e => e.Equals("vanilla", StringComparison.OrdinalIgnoreCase)))
        {
            preservedNonAppEntries.Insert(0, "vanilla");
        }

        // Chuẩn hóa tên pack người dùng: định dạng "file/<name>"
        // UI: pack index 0 có độ ưu tiên cao nhất (# 1).
        // Minecraft: nạp từ trái qua phải, gói đứng sau ghi đè gói đứng trước.
        // Do đó pack index 0 của UI phải nằm ở CUỐI CÙNG trong mảng của options.txt.
        var userPacks = (activePackFileNames ?? Enumerable.Empty<string>())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name =>
            {
                var clean = name.Trim();
                if (!clean.StartsWith("file/", StringComparison.OrdinalIgnoreCase))
                {
                    clean = "file/" + clean;
                }
                return clean;
            })
            .Reverse()
            .ToList();

        var finalEntries = new List<string>();
        foreach (var nonApp in preservedNonAppEntries)
        {
            finalEntries.Add($"\"{nonApp}\"");
        }
        foreach (var userPack in userPacks)
        {
            finalEntries.Add($"\"{userPack}\"");
        }

        var newLine = $"resourcePacks:[{string.Join(",", finalEntries)}]";

        if (existingLineIndex >= 0)
        {
            lines[existingLineIndex] = newLine;
        }
        else
        {
            lines.Add(newLine);
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Đọc danh sách file resource pack đang kích hoạt từ options.txt theo thứ tự hiển thị của UI.
    /// Trả về danh sách tên file theo thứ tự ưu tiên từ cao xuống thấp (phần tử cuối của options.txt ở index 0).
    /// </summary>
    public static List<string> ReadActivePacksFromOptionsFile(string optionsFilePath)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(optionsFilePath) || !File.Exists(optionsFilePath)) return result;

        try
        {
            var content = File.ReadAllText(optionsFilePath);
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("resourcePacks:", StringComparison.OrdinalIgnoreCase))
                {
                    var entries = ParseResourcePacksFromLine(trimmed);
                    var fileEntries = entries
                        .Where(e => e.StartsWith("file/", StringComparison.OrdinalIgnoreCase))
                        .Select(e => e[5..])
                        .Reverse()
                        .ToList();
                    result.AddRange(fileEntries);
                    break;
                }
            }
        }
        catch { }

        return result;
    }

    /// <summary>
    /// Ghi đè options.txt an toàn qua file tạm và atomic replace.
    /// Trả về false nếu gặp lỗi I/O hoặc thiếu quyền ghi mà không làm sập ứng dụng.
    /// </summary>
    public static bool SyncToOptionsFile(string optionsFilePath, IEnumerable<string> activePackFileNames)
    {
        if (string.IsNullOrWhiteSpace(optionsFilePath)) return false;

        string? tmpPath = null;
        try
        {
            var dir = Path.GetDirectoryName(optionsFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string originalContent = File.Exists(optionsFilePath)
                ? File.ReadAllText(optionsFilePath)
                : string.Empty;

            string updatedContent = UpdateResourcePacksLine(originalContent, activePackFileNames);

            tmpPath = optionsFilePath + ".tmp." + Guid.NewGuid().ToString("N");
            File.WriteAllText(tmpPath, updatedContent);

            if (File.Exists(optionsFilePath))
            {
                File.Move(tmpPath, optionsFilePath, overwrite: true);
            }
            else
            {
                File.Move(tmpPath, optionsFilePath);
            }

            return true;
        }
        catch
        {
            if (tmpPath != null && File.Exists(tmpPath))
            {
                try { File.Delete(tmpPath); } catch { }
            }
            return false;
        }
    }
}
