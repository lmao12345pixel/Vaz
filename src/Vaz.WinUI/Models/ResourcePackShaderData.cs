using System;
using System.Collections.Generic;

namespace Vaz.WinUI.Models;

public sealed class ResourcePackItem
{
    public string Id { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Resolution { get; set; } = "32x";
    public string Size { get; set; } = "10 MB";
    public string Version { get; set; } = "Unknown";
    public int PackFormat { get; set; } = 34; // 1.21.x format
    public string IconUri { get; set; } = string.Empty;
    public string BannerUri { get; set; } = string.Empty;
    public bool IsCompatible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; }
}

public sealed class ShaderPackItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Style { get; set; } = "realistic";
    public string StyleLabel { get; set; } = "Thực tế";
    public string Version { get; set; } = "1.0.0";
    public string Size { get; set; } = "5 MB";
    public string ImageUri { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#22c55e";
    public List<string> Features { get; set; } = new();
}

public static class ResourcePackShaderData
{
    // Đợt P1.2b/P1.4: Toàn bộ danh sách Resource Packs và Shaders được nạp từ manifest
    // (resourcepacks.json và shaderpacks.json) của từng instance thực tế, không dùng mock cứng.
    public static readonly List<ResourcePackItem> DefaultResourcePacks = new();
    public static readonly List<ShaderPackItem> DefaultShaders = new();
}
