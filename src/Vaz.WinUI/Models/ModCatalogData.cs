using System;
using System.Collections.Generic;
using System.Linq;

namespace Vaz.WinUI.Models;

public sealed record ModCatalogItem
{
    public string Id { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Desc { get; init; } = string.Empty;
    public string Description => Desc;
    public long Downloads { get; init; }
    public string Version { get; init; } = string.Empty;
    public IReadOnlyList<string> Loaders { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Versions { get; init; } = Array.Empty<string>();
    public string Category { get; init; } = "technology";
    public string CategoryLabel { get; init; } = "Khác";
    public string Size { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public string Accent { get; init; } = "#38bdf8";
}

public sealed record ModDependencyStatusItem
{
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string DisplayName => Name;
    public string Status { get; init; } = "active"; // "active", "disabled", "missing"
    public string RequiredBy { get; init; } = "Bắt buộc";
    public bool IsWarning => !string.Equals(Status, "active", StringComparison.OrdinalIgnoreCase);
    public string StatusText => Status.ToLowerInvariant() switch
    {
        "active" => "Đã bật",
        "disabled" => "Đang tắt",
        _ => "Chưa cài"
    };
    public string TagBackground => Status.ToLowerInvariant() switch
    {
        "active" => "#1e3a2f",
        "disabled" => "#3a311e",
        _ => "#3a1e1e"
    };
    public string TagForeground => Status.ToLowerInvariant() switch
    {
        "active" => "#4ade80",
        "disabled" => "#facc15",
        _ => "#f87171"
    };

    public ModDependencyStatusItem() { }
    public ModDependencyStatusItem(string slug, string name, string status, bool isWarning = false)
    {
        Slug = slug;
        Name = name;
        Status = status;
    }
}

public sealed record ModDependencyInfo
{
    public IReadOnlyList<ModDependencyStatusItem> Required { get; init; } = Array.Empty<ModDependencyStatusItem>();
    public IReadOnlyList<ModDependencyStatusItem> Missing { get; init; } = Array.Empty<ModDependencyStatusItem>();
    public IReadOnlyList<ModDependencyStatusItem> Disabled { get; init; } = Array.Empty<ModDependencyStatusItem>();
    public bool Satisfied => Missing.Count == 0 && Disabled.Count == 0;
}

public static class ModCatalogData
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Dependencies =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["sodium-extra"] = new[] { "sodium" },
            ["reeses-sodium-options"] = new[] { "sodium" },
            ["iris"] = new[] { "sodium", "fabric-api" },
            ["indium"] = new[] { "sodium", "fabric-api" },
            ["modmenu"] = new[] { "fabric-api" },
            ["continuity"] = new[] { "fabric-api" },
            ["create"] = new[] { "fabric-api" },
            ["xaeros-minimap"] = new[] { "fabric-api" },
            ["jei"] = new[] { "fabric-api" },
            ["entityculling"] = new[] { "fabric-api" },
            ["immediatelyfast"] = new[] { "fabric-api" },
            ["ferrite-core"] = new[] { "fabric-api" },
            ["starlight"] = new[] { "fabric-api" }
        };

    public static readonly IReadOnlyList<ModCatalogItem> Catalog = new List<ModCatalogItem>
    {
        new()
        {
            Id = "AANobbMI",
            Slug = "sodium",
            Name = "Sodium",
            Author = "jellysquid3",
            Desc = "A high-performance rendering engine replacement for Minecraft, which greatly improves frame rates and reduces micro-stutter.",
            Downloads = 248_000_000,
            Version = "0.6.13",
            Loaders = new[] { "fabric", "neoforge" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "performance",
            CategoryLabel = "Hiệu năng",
            Size = "0.8 MB",
            Icon = "ms-appx:///Assets/mods/sodium.webp",
            Accent = "#38bdf8"
        },
        new()
        {
            Id = "gvQqBUqZ",
            Slug = "lithium",
            Name = "Lithium",
            Author = "jellysquid3",
            Desc = "No-compromises game logic/server optimization mod.",
            Downloads = 189_000_000,
            Version = "0.15.0",
            Loaders = new[] { "fabric", "neoforge" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "performance",
            CategoryLabel = "Hiệu năng",
            Size = "0.6 MB",
            Icon = "ms-appx:///Assets/mods/lithium.webp",
            Accent = "#f59e0b"
        },
        new()
        {
            Id = "PpjUUSmh",
            Slug = "sodium-extra",
            Name = "Sodium Extra",
            Author = "FlashyReese",
            Desc = "A Sodium addon that adds features that shouldn't be in Sodium.",
            Downloads = 104_300_000,
            Version = "0.6.0",
            Loaders = new[] { "fabric" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "graphics",
            CategoryLabel = "Đồ họa",
            Size = "0.5 MB",
            Icon = "ms-appx:///Assets/mods/sodium-extra.webp",
            Accent = "#38bdf8"
        },
        new()
        {
            Id = "Bh37bMuy",
            Slug = "reeses-sodium-options",
            Name = "Reese's Sodium Options",
            Author = "FlashyReese",
            Desc = "Alternative Options Menu for Sodium.",
            Downloads = 86_900_000,
            Version = "1.8.3",
            Loaders = new[] { "fabric" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "graphics",
            CategoryLabel = "Đồ họa",
            Size = "0.3 MB",
            Icon = "ms-appx:///Assets/mods/reeses-sodium-options.png",
            Accent = "#38bdf8"
        },
        new()
        {
            Id = "YL57xq9U",
            Slug = "iris",
            Name = "Iris Shaders",
            Author = "coderbot",
            Desc = "A modern shader pack loader for Minecraft, designed to be compatible with Sodium.",
            Downloads = 74_200_000,
            Version = "1.8.1",
            Loaders = new[] { "fabric", "neoforge" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "graphics",
            CategoryLabel = "Đồ họa",
            Size = "1.5 MB",
            Icon = "ms-appx:///Assets/mods/iris.webp",
            Accent = "#a78bfa"
        },
        new()
        {
            Id = "uXXizFIs",
            Slug = "ferrite-core",
            Name = "FerriteCore",
            Author = "malte0811",
            Desc = "Memory usage optimizations for Minecraft.",
            Downloads = 71_500_000,
            Version = "7.0.1",
            Loaders = new[] { "fabric", "neoforge" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "performance",
            CategoryLabel = "Hiệu năng",
            Size = "0.4 MB",
            Icon = "ms-appx:///Assets/mods/ferrite-core.webp",
            Accent = "#ef4444"
        },
        new()
        {
            Id = "u6dRKJwZ",
            Slug = "jei",
            Name = "Just Enough Items (JEI)",
            Author = "mezz",
            Desc = "View items and recipes.",
            Downloads = 65_800_000,
            Version = "19.21.0",
            Loaders = new[] { "fabric", "neoforge" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "survival",
            CategoryLabel = "Sinh tồn/QoL",
            Size = "1.2 MB",
            Icon = "ms-appx:///Assets/mods/jei.webp",
            Accent = "#3b82f6"
        },
        new()
        {
            Id = "P7dR8mSH",
            Slug = "fabric-api",
            Name = "Fabric API",
            Author = "modmuss50",
            Desc = "Lightweight and modular API providing common hooks and intercompatibility measures utilized by mods using the Fabric toolchain.",
            Downloads = 61_400_000,
            Version = "0.115.0",
            Loaders = new[] { "fabric" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "technology",
            CategoryLabel = "Công nghệ",
            Size = "2.1 MB",
            Icon = "ms-appx:///Assets/mods/fabric-api.png",
            Accent = "#e0e7ff"
        },
        new()
        {
            Id = "mOgUt4GM",
            Slug = "modmenu",
            Name = "Mod Menu",
            Author = "Prospector",
            Desc = "Adds a mod menu to view the list of mods you have installed.",
            Downloads = 54_900_000,
            Version = "11.0.3",
            Loaders = new[] { "fabric" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "survival",
            CategoryLabel = "Sinh tồn/QoL",
            Size = "0.5 MB",
            Icon = "ms-appx:///Assets/mods/modmenu.png",
            Accent = "#22c55e"
        },
        new()
        {
            Id = "5ZwdcRci",
            Slug = "immediatelyfast",
            Name = "ImmediatelyFast",
            Author = "RaphiMC",
            Desc = "Speed up immediate mode rendering in Minecraft.",
            Downloads = 43_700_000,
            Version = "1.3.1",
            Loaders = new[] { "fabric", "neoforge" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "performance",
            CategoryLabel = "Hiệu năng",
            Size = "0.3 MB",
            Icon = "ms-appx:///Assets/mods/immediatelyfast.webp",
            Accent = "#eab308"
        },
        new()
        {
            Id = "NNAgCjsB",
            Slug = "entityculling",
            Name = "Entity Culling",
            Author = "tr7zw",
            Desc = "Using async path-tracing to skip rendering blocks and entities which are not visible.",
            Downloads = 39_800_000,
            Version = "1.7.3",
            Loaders = new[] { "fabric", "neoforge" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "performance",
            CategoryLabel = "Hiệu năng",
            Size = "0.2 MB",
            Icon = "ms-appx:///Assets/mods/entityculling.webp",
            Accent = "#10b981"
        },
        new()
        {
            Id = "Orvt0mRa",
            Slug = "indium",
            Name = "Indium",
            Author = "comp500",
            Desc = "Sodium addon providing support for the Fabric Rendering API.",
            Downloads = 33_100_000,
            Version = "1.0.35",
            Loaders = new[] { "fabric" },
            Versions = new[] { "1.21.1" },
            Category = "technology",
            CategoryLabel = "Công nghệ",
            Size = "0.4 MB",
            Icon = "ms-appx:///Assets/mods/indium.webp",
            Accent = "#6366f1"
        },
        new()
        {
            Id = "9s6osm5g",
            Slug = "cloth-config",
            Name = "Cloth Config API",
            Author = "shedaniel",
            Desc = "Configuration library for Minecraft mods.",
            Downloads = 31_600_000,
            Version = "15.0.140",
            Loaders = new[] { "fabric", "neoforge" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "technology",
            CategoryLabel = "Công nghệ",
            Size = "1.1 MB",
            Icon = "ms-appx:///Assets/mods/cloth-config.webp",
            Accent = "#ec4899"
        },
        new()
        {
            Id = "1IjD5062",
            Slug = "continuity",
            Name = "Continuity",
            Author = "PepperCode1",
            Desc = "A Fabric mod that allows for efficient connected textures.",
            Downloads = 27_400_000,
            Version = "3.0.0",
            Loaders = new[] { "fabric" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "graphics",
            CategoryLabel = "Đồ họa",
            Size = "0.7 MB",
            Icon = "ms-appx:///Assets/mods/continuity.png",
            Accent = "#14b8a6"
        },
        new()
        {
            Id = "lhGA9TYQ",
            Slug = "architectury-api",
            Name = "Architectury API",
            Author = "shedaniel",
            Desc = "An intermediary api aimed to ease developing multiplatform mods.",
            Downloads = 24_900_000,
            Version = "13.0.8",
            Loaders = new[] { "fabric", "neoforge" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "technology",
            CategoryLabel = "Công nghệ",
            Size = "0.6 MB",
            Icon = "ms-appx:///Assets/mods/architectury-api.webp",
            Accent = "#8b5cf6"
        },
        new()
        {
            Id = "H8CaAYZC",
            Slug = "starlight",
            Name = "Starlight",
            Author = "PaperMC",
            Desc = "Rewrites the light engine to fix lighting performance and lighting bugs.",
            Downloads = 21_200_000,
            Version = "1.1.5",
            Loaders = new[] { "fabric" },
            Versions = new[] { "1.21.1" },
            Category = "performance",
            CategoryLabel = "Hiệu năng",
            Size = "0.5 MB",
            Icon = "ms-appx:///Assets/mods/starlight.webp",
            Accent = "#fbbf24"
        },
        new()
        {
            Id = "LNytGWDc",
            Slug = "create",
            Name = "Create",
            Author = "simibubi",
            Desc = "Building tools and aesthetic technology for Minecraft.",
            Downloads = 19_600_000,
            Version = "6.0.4",
            Loaders = new[] { "fabric", "neoforge" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "technology",
            CategoryLabel = "Công nghệ",
            Size = "16.4 MB",
            Icon = "ms-appx:///Assets/mods/create.webp",
            Accent = "#d97706"
        },
        new()
        {
            Id = "1bokaNcj",
            Slug = "xaeros-minimap",
            Name = "Xaero's Minimap",
            Author = "xaero96",
            Desc = "Displays the nearby terrain, and entities such as mobs, players and dropped items in a minimap.",
            Downloads = 18_300_000,
            Version = "24.2.0",
            Loaders = new[] { "fabric", "neoforge" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "survival",
            CategoryLabel = "Sinh tồn/QoL",
            Size = "1.8 MB",
            Icon = "ms-appx:///Assets/mods/xaeros-minimap.webp",
            Accent = "#84cc16"
        },
        new()
        {
            Id = "EsAfCjCV",
            Slug = "appleskin",
            Name = "AppleSkin",
            Author = "squeek502",
            Desc = "Food-related HUD improvements: hunger/saturation values and tooltips.",
            Downloads = 17_100_000,
            Version = "3.0.6",
            Loaders = new[] { "fabric", "neoforge" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "survival",
            CategoryLabel = "Sinh tồn/QoL",
            Size = "0.2 MB",
            Icon = "ms-appx:///Assets/mods/appleskin.png",
            Accent = "#ef4444"
        },
        new()
        {
            Id = "nvQzSEkH",
            Slug = "jade",
            Name = "Jade",
            Author = "Snownee",
            Desc = "Shows information about what you are looking at.",
            Downloads = 16_400_000,
            Version = "15.3.0",
            Loaders = new[] { "fabric", "neoforge" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "survival",
            CategoryLabel = "Sinh tồn/QoL",
            Size = "0.9 MB",
            Icon = "ms-appx:///Assets/mods/jade.webp",
            Accent = "#06b6d4"
        },
        new()
        {
            Id = "fRiHVvU7",
            Slug = "emi",
            Name = "EMI",
            Author = "Emi",
            Desc = "A featureful and accessible item and recipe viewer.",
            Downloads = 15_200_000,
            Version = "1.1.22",
            Loaders = new[] { "fabric", "neoforge" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "survival",
            CategoryLabel = "Sinh tồn/QoL",
            Size = "1.4 MB",
            Icon = "ms-appx:///Assets/mods/emi.webp",
            Accent = "#f43f5e"
        },
        new()
        {
            Id = "nmDcB62a",
            Slug = "modernfix",
            Name = "ModernFix",
            Author = "embeddedt",
            Desc = "Dynamic performance improvements and bug fixes for modern Minecraft.",
            Downloads = 14_600_000,
            Version = "5.23.1",
            Loaders = new[] { "fabric", "neoforge" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "performance",
            CategoryLabel = "Hiệu năng",
            Size = "1.0 MB",
            Icon = "ms-appx:///Assets/mods/modernfix.webp",
            Accent = "#0284c7"
        },
        new()
        {
            Id = "g96Z4WVZ",
            Slug = "badoptimizations",
            Name = "BadOptimizations",
            Author = "ThetaDev",
            Desc = "A collection of micro-optimizations for the client.",
            Downloads = 12_900_000,
            Version = "2.2.1",
            Loaders = new[] { "fabric" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "performance",
            CategoryLabel = "Hiệu năng",
            Size = "0.1 MB",
            Icon = "ms-appx:///Assets/mods/badoptimizations.webp",
            Accent = "#64748b"
        },
        new()
        {
            Id = "1eAoo2KR",
            Slug = "yacl",
            Name = "YetAnotherConfigLib",
            Author = "isXander",
            Desc = "A builder-based configuration library for Fabric mods.",
            Downloads = 11_800_000,
            Version = "3.7.1",
            Loaders = new[] { "fabric" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "technology",
            CategoryLabel = "Công nghệ",
            Size = "1.3 MB",
            Icon = "ms-appx:///Assets/mods/yacl.webp",
            Accent = "#8b5cf6"
        },
        new()
        {
            Id = "ironspells",
            Slug = "irons-spells",
            Name = "Iron's Spells 'n Spellbooks",
            Author = "IronSpells",
            Desc = "A complete magic progression mod with RPG-style spells, wizard armor and bosses.",
            Downloads = 9_200_000,
            Version = "3.4.1",
            Loaders = new[] { "fabric", "neoforge" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "magic",
            CategoryLabel = "Phép thuật",
            Size = "12.8 MB",
            Icon = "ms-appx:///Assets/mods/irons-spells.png",
            Accent = "#9333ea"
        },
        new()
        {
            Id = "botania",
            Slug = "botania",
            Name = "Botania",
            Author = "Vazkii",
            Desc = "An innovative natural magic mod themed around magical flora and botanical mana.",
            Downloads = 14_500_000,
            Version = "4.4.1",
            Loaders = new[] { "fabric", "neoforge" },
            Versions = new[] { "1.21.1", "1.20.1" },
            Category = "magic",
            CategoryLabel = "Phép thuật",
            Size = "9.5 MB",
            Icon = "ms-appx:///Assets/mods/botania.webp",
            Accent = "#10b981"
        }
    };

    public static string GetSlug(string? fileName, string? projectId)
    {
        if (!string.IsNullOrEmpty(projectId))
        {
            var p = Catalog.FirstOrDefault(c => string.Equals(c.Id, projectId, StringComparison.OrdinalIgnoreCase) ||
                                                string.Equals(c.Slug, projectId, StringComparison.OrdinalIgnoreCase));
            if (p != null) return p.Slug;
        }

        if (string.IsNullOrEmpty(fileName)) return string.Empty;

        var lower = fileName.ToLowerInvariant();
        foreach (var c in Catalog)
        {
            if (lower.Contains(c.Slug.ToLowerInvariant()))
                return c.Slug;
        }

        // Clean filename up to version or hyphen
        var clean = fileName.Replace(".jar", "", StringComparison.OrdinalIgnoreCase);
        var idx = clean.IndexOfAny(new[] { '-', '_' });
        return idx > 0 ? clean[..idx].ToLowerInvariant() : clean.ToLowerInvariant();
    }

    public static ModCatalogItem? FindProject(string? slugOrId)
    {
        if (string.IsNullOrEmpty(slugOrId)) return null;

        return Catalog.FirstOrDefault(c => string.Equals(c.Slug, slugOrId, StringComparison.OrdinalIgnoreCase) ||
                                           string.Equals(c.Id, slugOrId, StringComparison.OrdinalIgnoreCase) ||
                                           string.Equals(c.Name, slugOrId, StringComparison.OrdinalIgnoreCase));
    }

    public static string GetCategory(string? fileName, string? projectId)
    {
        var slug = GetSlug(fileName, projectId);
        var p = FindProject(slug);
        return p?.Category ?? "technology";
    }

    public static string GetCategoryLabel(string category) => category.ToLowerInvariant() switch
    {
        "performance" => "Hiệu năng",
        "graphics" => "Đồ họa",
        "technology" => "Công nghệ",
        "survival" => "Sinh tồn/QoL",
        "magic" => "Phép thuật",
        _ => "Tất cả"
    };

    public static IReadOnlyList<string> GetRequiredDependencies(string slug)
    {
        if (Dependencies.TryGetValue(slug, out var list))
            return list;
        return Array.Empty<string>();
    }
}
