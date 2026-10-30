using System;
using System.Collections.Generic;

namespace Vaz.WinUI.Models;

public sealed class ResourcePackItem
{
    public string Id { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Resolution { get; set; } = "32x";
    public string Size { get; set; } = "10 MB";
    public string Version { get; set; } = "1.21.1";
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
    public static readonly List<ResourcePackItem> DefaultResourcePacks = new()
    {
        new ResourcePackItem
        {
            Id = "faithful-32x",
            Slug = "faithful-32x",
            Name = "Faithful 32x",
            Author = "Faithful Team",
            Description = "Giữ trọn vẹn phong cách nguyên bản của Minecraft nhưng nhân đôi độ phân giải lên 32x sắc nét.",
            Resolution = "32x",
            Size = "18.5 MB",
            Version = "v1.21-r1",
            PackFormat = 34,
            IconUri = "ms-appx:///Assets/resourcepacks/faithful-icon.png",
            BannerUri = "ms-appx:///Assets/resourcepacks/faithful-banner.jpg",
            IsCompatible = true,
            Enabled = true,
            Priority = 0
        },
        new ResourcePackItem
        {
            Id = "bare-bones",
            Slug = "bare-bones",
            Name = "Bare Bones",
            Author = "RobotPantaloons",
            Description = "Đơn giản hóa vân bề mặt Minecraft theo phong cách trailer chính thức của Mojang.",
            Resolution = "16x",
            Size = "4.2 MB",
            Version = "v1.21.1",
            PackFormat = 34,
            IconUri = "ms-appx:///Assets/resourcepacks/bare-bones-icon.png",
            BannerUri = "ms-appx:///Assets/resourcepacks/bare-bones-banner.jpg",
            IsCompatible = true,
            Enabled = true,
            Priority = 1
        },
        new ResourcePackItem
        {
            Id = "stay-true",
            Slug = "stay-true",
            Name = "Stay True",
            Author = "HaiScribe",
            Description = "Tối ưu và làm mới texture vanilla: kết nối lá cây mượt mà, biến thể khối và hoa cỏ sinh động.",
            Resolution = "16x",
            Size = "14.8 MB",
            Version = "1.21.1-v1",
            PackFormat = 34,
            IconUri = "ms-appx:///Assets/resourcepacks/stay-true-icon.png",
            BannerUri = "ms-appx:///Assets/resourcepacks/stay-true-banner.jpg",
            IsCompatible = true,
            Enabled = false,
            Priority = 2
        },
        new ResourcePackItem
        {
            Id = "fresh-animations",
            Slug = "fresh-animations",
            Name = "Fresh Animations",
            Author = "FreshLX",
            Description = "Làm mới toàn bộ chuyển động của sinh vật Minecraft, biểu cảm tự nhiên và uyển chuyển hơn.",
            Resolution = "16x",
            Size = "3.1 MB",
            Version = "v1.9.2",
            PackFormat = 34,
            IconUri = "ms-appx:///Assets/resourcepacks/fresh-animations-icon.png",
            BannerUri = "ms-appx:///Assets/resourcepacks/fresh-animations-banner.jpg",
            IsCompatible = true,
            Enabled = false,
            Priority = 3
        },
        new ResourcePackItem
        {
            Id = "dokucraft-light-legacy",
            Slug = "dokucraft-light-legacy",
            Name = "Dokucraft Light (MC 1.20)",
            Author = "Doku & Community",
            Description = "Gói tài nguyên phong cách RPG thời Trung Cổ dành cho Minecraft 1.20 cũ, chưa nâng cấp lên 1.21.",
            Resolution = "32x",
            Size = "22.4 MB",
            Version = "v1.20.4",
            PackFormat = 15,
            IconUri = "ms-appx:///Assets/resourcepacks/bare-bones-icon.png",
            BannerUri = "ms-appx:///Assets/resourcepacks/bare-bones-banner.jpg",
            IsCompatible = false,
            Enabled = false,
            Priority = 4
        }
    };

    public static readonly List<ShaderPackItem> DefaultShaders = new()
    {
        new ShaderPackItem
        {
            Id = "bsl-shaders",
            Name = "BSL Shaders",
            Author = "Capt Tatsu",
            Description = "Hình ảnh tươi sáng, màu sắc ấm áp tự nhiên, độ tùy biến cao và hiệu năng mượt mà.",
            Style = "realistic",
            StyleLabel = "Thực tế",
            Version = "v8.2.09",
            Size = "8.4 MB",
            ImageUri = "ms-appx:///Assets/shaders/bsl-shaders.jpg",
            AccentColor = "#38bdf8",
            Features = new() { "Màu sắc ấm áp", "Mây volumetric", "Phản chiếu thời gian thực" }
        },
        new ShaderPackItem
        {
            Id = "complementary-reimagined",
            Name = "Complementary Reimagined",
            Author = "EminGT",
            Description = "Cân bằng hoàn hảo giữa phong cách Minecraft nguyên bản và đồ họa shader hiện đại đỉnh cao.",
            Style = "hybrid",
            StyleLabel = "Hiện đại",
            Version = "r5.2.2",
            Size = "12.1 MB",
            ImageUri = "ms-appx:///Assets/shaders/complementary-reimagined.jpg",
            AccentColor = "#22c55e",
            Features = new() { "Giữ khối Vanilla", "Ánh sáng phát quang (PBR)", "Tối ưu FPS cao" }
        },
        new ShaderPackItem
        {
            Id = "photon-shader",
            Name = "Photon Shader",
            Author = "SixthSurge",
            Description = "Chất lượng hình ảnh chân thực, mô phỏng quang học và khí quyển tiên tiến với chi phí phần cứng hợp lý.",
            Style = "realistic",
            StyleLabel = "Quang học",
            Version = "v1.0b",
            Size = "2.8 MB",
            ImageUri = "ms-appx:///Assets/shaders/photon-shader.jpg",
            AccentColor = "#818cf8",
            Features = new() { "Mô phỏng khí quyển", "Tone mapping điện ảnh", "Hiệu năng ổn định" }
        },
        new ShaderPackItem
        {
            Id = "solas-shader",
            Name = "Solas Shader",
            Author = "SeptFox",
            Description = "Hiệu ứng ánh sáng rực rỡ, tia nắng xuyên qua tán cây và bầu trời hoàng hôn huyền ảo.",
            Style = "cinematic",
            StyleLabel = "Điện ảnh",
            Version = "V2.0",
            Size = "4.5 MB",
            ImageUri = "ms-appx:///Assets/shaders/solas-shader.jpg",
            AccentColor = "#f59e0b",
            Features = new() { "Tia nắng God Rays", "Sương mù thể tích", "Mặt nước sinh động" }
        },
        new ShaderPackItem
        {
            Id = "bliss-shader",
            Name = "Bliss Shaders",
            Author = "Xonk",
            Description = "Fantasy giàu chi tiết, nhiều biến thể bầu trời và thời tiết sống động.",
            Style = "fantasy",
            StyleLabel = "Fantasy",
            Version = "2.1.2",
            Size = "1.7 MB",
            ImageUri = "ms-appx:///Assets/shaders/bliss-shader.jpg",
            AccentColor = "#2fb7a8",
            Features = new() { "Nhiều preset", "Thời tiết động", "Tùy chỉnh sâu" }
        },
        new ShaderPackItem
        {
            Id = "makeup-ultra-fast-shaders",
            Name = "MakeUp - Ultra Fast",
            Author = "KDXavier",
            Description = "Tỉ lệ chất lượng/hiệu năng cực tốt, phù hợp máy cấu hình khiêm tốn vẫn muốn có ánh sáng bóng đổ.",
            Style = "performance",
            StyleLabel = "Hiệu năng",
            Version = "9.6",
            Size = "0.4 MB",
            ImageUri = "ms-appx:///Assets/shaders/makeup-ultra-fast-shaders.jpg",
            AccentColor = "#ed6a5a",
            Features = new() { "Siêu nhẹ", "Preset linh hoạt", "Tốt cho máy yếu" }
        },
        new ShaderPackItem
        {
            Id = "astralex",
            Name = "AstraLex Shaders",
            Author = "LexBoosT",
            Description = "Hiệu ứng phong phú, màu sắc rực rỡ và các preset nổi bật.",
            Style = "fantasy",
            StyleLabel = "Fantasy",
            Version = "93.0",
            Size = "3.0 MB",
            ImageUri = "ms-appx:///Assets/shaders/astralex.jpg",
            AccentColor = "#d86ee8",
            Features = new() { "Tùy biến cao", "Hiệu ứng mạnh", "Phong cách đậm" }
        },
        new ShaderPackItem
        {
            Id = "miniature-shader",
            Name = "Miniature Shader",
            Author = "ukrech",
            Description = "Siêu nhẹ, chỉ thêm bóng đổ và phản chiếu nước - gần như không ảnh hưởng FPS.",
            Style = "performance",
            StyleLabel = "Hiệu năng",
            Version = "2.19",
            Size = "<0.1 MB",
            ImageUri = "ms-appx:///Assets/shaders/miniature-shader.jpg",
            AccentColor = "#8da2bf",
            Features = new() { "Nhẹ nhất", "Bóng đổ cơ bản", "Phản chiếu nước" }
        }
    };
}
