using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vazer.Core.Modrinth;

namespace Vaz.WinUI.Services;

/// <summary>
/// Triển khai IModrinthSearchService: gọi IModrinthClient, xử lý pagination 20 item/lần,
/// và gợi ý dynamic category chips.
/// </summary>
public sealed class ModrinthSearchService : IModrinthSearchService
{
    private readonly IModrinthClient _modrinthClient;

    private static readonly string[] DefaultCategories =
    {
        "Tất cả", "Performance", "Utility", "Visual", "Technology", "Magic", "Adventure", "Storage"
    };

    public ModrinthSearchService(IModrinthClient modrinthClient)
    {
        _modrinthClient = modrinthClient ?? throw new ArgumentNullException(nameof(modrinthClient));
    }

    public async Task<SearchResult> SearchAsync(SearchFilterOptions options, CancellationToken ct = default)
    {
        var query = BuildSearchQuery(options, isBrowseMode: false);
        return await _modrinthClient.SearchAsync(query, ct);
    }

    public async Task<SearchResult> GetFeaturedModsAsync(SearchFilterOptions options, CancellationToken ct = default)
    {
        var query = BuildSearchQuery(options, isBrowseMode: true);
        return await _modrinthClient.GetFeaturedModsAsync(query, ct);
    }

    public IReadOnlyList<string> GetSuggestedCategories(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return DefaultCategories;
        }

        var lower = query.Trim().ToLowerInvariant();

        var suggestions = new List<string> { "Tất cả" };

        if (lower.Contains("sodium") || lower.Contains("iris") || lower.Contains("optifine") ||
            lower.Contains("fps") || lower.Contains("lag") || lower.Contains("perf") ||
            lower.Contains("fast") || lower.Contains("render") || lower.Contains("cull") ||
            lower.Contains("lithium") || lower.Contains("phosphor") || lower.Contains("memory"))
        {
            suggestions.AddRange(new[] { "Performance", "Optimization", "Utility" });
        }
        else if (lower.Contains("shader") || lower.Contains("light") || lower.Contains("visual") ||
                 lower.Contains("texture") || lower.Contains("graph") || lower.Contains("camera"))
        {
            suggestions.AddRange(new[] { "Shaders", "Visual", "Optimization" });
        }
        else if (lower.Contains("inventor") || lower.Contains("chest") || lower.Contains("storage") ||
                 lower.Contains("bag") || lower.Contains("pack") || lower.Contains("sort") ||
                 lower.Contains("jei") || lower.Contains("rei") || lower.Contains("emi") ||
                 lower.Contains("recipe") || lower.Contains("item"))
        {
            suggestions.AddRange(new[] { "Storage", "Utility", "Inventory" });
        }
        else if (lower.Contains("map") || lower.Contains("mini") || lower.Contains("point") ||
                 lower.Contains("journey") || lower.Contains("xaero") || lower.Contains("hud") ||
                 lower.Contains("gui") || lower.Contains("ui"))
        {
            suggestions.AddRange(new[] { "Utility", "Information", "Visual" });
        }
        else if (lower.Contains("magic") || lower.Contains("spell") || lower.Contains("mana") ||
                 lower.Contains("wizard") || lower.Contains("botania") || lower.Contains("iron"))
        {
            suggestions.AddRange(new[] { "Magic", "Adventure", "Technology" });
        }
        else if (lower.Contains("tech") || lower.Contains("pipe") || lower.Contains("machine") ||
                 lower.Contains("energy") || lower.Contains("generator") || lower.Contains("create") ||
                 lower.Contains("mekanism") || lower.Contains("applied"))
        {
            suggestions.AddRange(new[] { "Technology", "Automation", "Utility" });
        }
        else if (lower.Contains("world") || lower.Contains("biome") || lower.Contains("dimension") ||
                 lower.Contains("cave") || lower.Contains("dungeon") || lower.Contains("struct"))
        {
            suggestions.AddRange(new[] { "World Generation", "Adventure", "Visual" });
        }
        else
        {
            suggestions.AddRange(new[] { "Performance", "Utility", "Visual", "Technology", "Magic", "Adventure" });
        }

        return suggestions.Distinct().ToList();
    }

    private static SearchQuery BuildSearchQuery(SearchFilterOptions options, bool isBrowseMode)
    {
        string[]? categories = null;
        if (!string.IsNullOrWhiteSpace(options.Category) &&
            !string.Equals(options.Category, "Tất cả", StringComparison.OrdinalIgnoreCase))
        {
            categories = new[] { options.Category.Trim().ToLowerInvariant() };
        }

        return new SearchQuery
        {
            Query = isBrowseMode ? null : options.Query?.Trim(),
            ProjectType = options.ProjectType,
            Loaders = options.Loaders,
            GameVersions = options.GameVersions,
            Categories = categories,
            SortIndex = isBrowseMode ? (options.SortIndex ?? "downloads") : options.SortIndex,
            Offset = options.Offset,
            Limit = Math.Clamp(options.Limit > 0 ? options.Limit : 20, 1, 100)
        };
    }
}
