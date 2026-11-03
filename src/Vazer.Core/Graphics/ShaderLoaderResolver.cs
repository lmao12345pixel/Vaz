using System;
using System.Collections.Generic;
using System.Linq;

namespace Vazer.Core.Graphics;

public static class ShaderLoaderResolver
{
    private static readonly string[] ShaderLoaderKeywords = { "iris", "oculus", "optifine" };

    /// <summary>
    /// Kiểm tra danh sách tên mod hoặc mod ID đã cài đặt xem có chứa Shader Loader (Iris, Oculus, OptiFine) hay không.
    /// </summary>
    public static bool DetectShaderLoader(IEnumerable<string> installedModIdentifiers)
    {
        if (installedModIdentifiers == null) return false;

        return installedModIdentifiers.Any(id =>
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            var lower = id.ToLowerInvariant();
            return ShaderLoaderKeywords.Any(k => lower.Contains(k));
        });
    }

    /// <summary>
    /// Kiểm tra định dạng PackFormat có tương thích với phiên bản Minecraft của instance hay không.
    /// Đối với Minecraft 1.21 / 1.21.1: PackFormat chuẩn là 34.
    /// </summary>
    public static bool IsPackFormatCompatible(int packFormat, string mcVersion)
    {
        if (packFormat <= 0) return true; // Không xác định, cho phép load

        if (mcVersion.StartsWith("1.21"))
        {
            return packFormat == 34;
        }

        if (mcVersion.StartsWith("1.20.5") || mcVersion.StartsWith("1.20.6"))
        {
            return packFormat == 32;
        }

        if (mcVersion.StartsWith("1.20"))
        {
            return packFormat == 15 || packFormat == 18;
        }

        return true;
    }

    /// <summary>
    /// Lấy PackFormat chuẩn mong đợi cho phiên bản Minecraft.
    /// </summary>
    public static int GetExpectedPackFormat(string mcVersion)
    {
        if (mcVersion.StartsWith("1.21")) return 34;
        if (mcVersion.StartsWith("1.20.5") || mcVersion.StartsWith("1.20.6")) return 32;
        if (mcVersion.StartsWith("1.20")) return 15;
        return 34;
    }

    /// <summary>
    /// Xác định phiên bản Iris tương thích dựa trên phiên bản Minecraft và Mod Loader (Fabric / NeoForge).
    /// </summary>
    public static (string Version, string DownloadUrl, string FileName) ResolveIrisVersion(string mcVersion, string loaderType)
    {
        // Default target cho Minecraft 1.21.1 Fabric
        if (mcVersion.StartsWith("1.21"))
        {
            return (
                "1.7.5+1.21",
                "https://cdn.modrinth.com/data/YL57xq9U/versions/8U1iPcvT/iris-1.7.5%2B1.21.jar",
                "iris-1.7.5+1.21.jar"
            );
        }

        // Fallback target cho Minecraft 1.20.1 Fabric
        return (
            "1.7.0+1.20.1",
            "https://cdn.modrinth.com/data/YL57xq9U/versions/iris-1.7.0-1.20.1.jar",
            "iris-1.7.0+1.20.1.jar"
        );
    }

    /// <summary>
    /// Lọc danh sách theo truy vấn tìm kiếm (tên hoặc tác giả).
    /// </summary>
    public static bool MatchesSearchQuery(string name, string author, string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;
        var q = query.Trim().ToLowerInvariant();
        return (name != null && name.ToLowerInvariant().Contains(q)) ||
               (author != null && author.ToLowerInvariant().Contains(q));
    }
}
