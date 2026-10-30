using System.IO.Compression;
using System.Text.Json;

namespace Vazer.Core.Mods;

/// <summary>
/// Metadata đọc trực tiếp từ bên trong file .jar mod (bước Load Mod).
/// Ưu tiên: fabric.mod.json → quilt.mod.json → META-INF/mods.toml (Forge /
/// NeoForge cũ) → META-INF/neoforge.mods.toml (NeoForge 20.5+).
/// Trả về null khi jar hỏng hoặc không có metadata nhận dạng được — caller
/// phải hiển thị cảnh báo riêng thay vì crash.
/// </summary>
public sealed record JarMetadata
{
    public string ModId { get; init; } = "";
    public string Name { get; init; } = "";
    public string Version { get; init; } = "";
    /// <summary>fabric / quilt / forge / neoforge — rỗng khi không nhận dạng được.</summary>
    public string Loader { get; init; } = "";
    public IReadOnlyList<string> Authors { get; init; } = Array.Empty<string>();
    public string Description { get; init; } = "";
}

public static class JarMetadataReader
{
    public static JarMetadata? Read(string jarPath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(jarPath);
            return Read(archive);
        }
        catch (Exception ex) when (ex is InvalidDataException
                                        or IOException
                                        or NotSupportedException
                                        or UnauthorizedAccessException)
        {
            return null; // jar hỏng / không phải zip / bị khóa — không crash
        }
    }

    public static JarMetadata? Read(Stream jarStream)
    {
        try
        {
            using var archive = new ZipArchive(jarStream, ZipArchiveMode.Read, leaveOpen: true);
            return Read(archive);
        }
        catch (Exception ex) when (ex is InvalidDataException
                                        or IOException
                                        or NotSupportedException)
        {
            return null;
        }
    }

    private static JarMetadata? Read(ZipArchive archive)
    {
        var fabric = archive.GetEntry("fabric.mod.json");
        if (fabric != null)
        {
            var meta = ReadFabricLike(fabric, loader: "fabric");
            if (meta != null) return meta;
        }

        var quilt = archive.GetEntry("quilt.mod.json");
        if (quilt != null)
        {
            var meta = ReadQuilt(quilt);
            if (meta != null) return meta;
        }

        var toml = archive.GetEntry("META-INF/mods.toml");
        if (toml != null)
        {
            var meta = ReadModsToml(toml, loader: "forge");
            if (meta != null) return meta;
        }

        var neoforgeToml = archive.GetEntry("META-INF/neoforge.mods.toml");
        if (neoforgeToml != null)
        {
            var meta = ReadModsToml(neoforgeToml, loader: "neoforge");
            if (meta != null) return meta;
        }

        return null; // không có metadata nhận dạng được
    }

    /// <summary>fabric.mod.json (schemaVersion 1): id, version, name tùy chọn, authors là mảng string hoặc object.</summary>
    private static JarMetadata? ReadFabricLike(ZipArchiveEntry entry, string loader)
    {
        try
        {
            using var stream = entry.Open();
            using var doc = JsonDocument.Parse(stream);
            var root = doc.RootElement;

            var id = root.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String
                ? idEl.GetString() ?? "" : "";
            if (string.IsNullOrWhiteSpace(id)) return null;

            var version = root.TryGetProperty("version", out var verEl) && verEl.ValueKind == JsonValueKind.String
                ? verEl.GetString() ?? "" : "";

            var name = root.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
                ? nameEl.GetString() ?? "" : "";

            var authors = ReadAuthors(root, "authors");

            var description = root.TryGetProperty("description", out var descEl)
                              && descEl.ValueKind == JsonValueKind.String
                ? descEl.GetString() ?? "" : "";

            return new JarMetadata
            {
                ModId = id,
                Name = string.IsNullOrWhiteSpace(name) ? id : name,
                Version = version,
                Loader = loader,
                Authors = authors,
                Description = description
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// quilt.mod.json: quilt_loader.id / version; metadata.name;
    /// metadata.contributors là map "tên-author" → vai trò.
    /// (Một số file quilt vẫn kèm fabric.mod.json tương thích — fabric đọc trước.)
    /// </summary>
    private static JarMetadata? ReadQuilt(ZipArchiveEntry entry)
    {
        try
        {
            using var stream = entry.Open();
            using var doc = JsonDocument.Parse(stream);
            var root = doc.RootElement;

            if (!root.TryGetProperty("quilt_loader", out var loaderEl)
                || loaderEl.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var id = loaderEl.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String
                ? idEl.GetString() ?? "" : "";
            if (string.IsNullOrWhiteSpace(id)) return null;

            var version = loaderEl.TryGetProperty("version", out var verEl) && verEl.ValueKind == JsonValueKind.String
                ? verEl.GetString() ?? "" : "";

            var name = "";
            var description = "";
            var authors = Array.Empty<string>();
            if (loaderEl.TryGetProperty("metadata", out var metaEl) && metaEl.ValueKind == JsonValueKind.Object)
            {
                if (metaEl.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
                {
                    name = nameEl.GetString() ?? "";
                }

                if (metaEl.TryGetProperty("description", out var descEl)
                    && descEl.ValueKind == JsonValueKind.String)
                {
                    description = descEl.GetString() ?? "";
                }

                if (metaEl.TryGetProperty("contributors", out var contribEl)
                    && contribEl.ValueKind == JsonValueKind.Object)
                {
                    authors = contribEl.EnumerateObject()
                        .Select(p => p.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .ToArray();
                }
            }

            return new JarMetadata
            {
                ModId = id,
                Name = string.IsNullOrWhiteSpace(name) ? id : name,
                Version = version,
                Loader = "quilt",
                Authors = authors,
                Description = description
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// META-INF/mods.toml / neoforge.mods.toml — parser TOML tối giản:
    /// lấy block [[mods]] đầu tiên và các cặp key = "value" của string.
    /// </summary>
    private static JarMetadata? ReadModsToml(ZipArchiveEntry entry, string loader)
    {
        try
        {
            using var stream = entry.Open();
            using var reader = new StreamReader(stream);

            var inModsBlock = false;
            string? modId = null, version = null, name = null, authors = null, description = null;

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;

                if (trimmed.StartsWith('['))
                {
                    if (string.Equals(trimmed, "[[mods]]", StringComparison.OrdinalIgnoreCase))
                    {
                        // Block [[mods]] đầu tiên wins; các block sau là phụ thuộc.
                        if (modId != null) break;
                        inModsBlock = true;
                    }
                    else if (inModsBlock)
                    {
                        break; // rời block [[mods]]
                    }
                    continue;
                }

                if (!inModsBlock) continue;

                var eq = trimmed.IndexOf('=');
                if (eq <= 0) continue;
                var key = trimmed[..eq].Trim();
                var value = trimmed[(eq + 1)..].Trim();
                value = UnquoteToml(value);
                if (value == null) continue;

                if (string.Equals(key, "modId", StringComparison.OrdinalIgnoreCase)) modId ??= value;
                else if (string.Equals(key, "version", StringComparison.OrdinalIgnoreCase)) version ??= value;
                else if (string.Equals(key, "displayName", StringComparison.OrdinalIgnoreCase)) name ??= value;
                else if (string.Equals(key, "authors", StringComparison.OrdinalIgnoreCase)) authors ??= value;
                else if (string.Equals(key, "description", StringComparison.OrdinalIgnoreCase)) description ??= value;
            }

            if (string.IsNullOrWhiteSpace(modId)) return null;

            return new JarMetadata
            {
                ModId = modId,
                Name = string.IsNullOrWhiteSpace(name) ? modId : name,
                Version = version ?? "",
                Loader = loader,
                Authors = SplitTomlAuthors(authors),
                Description = description ?? ""
            };
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static string? UnquoteToml(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
        {
            return value[1..^1].Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
        // Chuỗi TOML không nằm trong ngoặc kép (số, boolean, mảng…) — bỏ qua.
        return value.StartsWith('"') ? null : value;
    }

    private static IReadOnlyList<string> SplitTomlAuthors(string? authors)
    {
        if (string.IsNullOrWhiteSpace(authors)) return Array.Empty<string>();
        if (authors.StartsWith('[') && authors.EndsWith(']'))
        {
            // Mảng TOML: ["a", "b"] — tách thô theo dấu phẩy ngoài cùng.
            var inner = authors[1..^1];
            var items = new List<string>();
            foreach (var part in inner.Split(','))
            {
                var v = UnquoteToml(part.Trim());
                if (!string.IsNullOrWhiteSpace(v)) items.Add(v);
            }
            return items;
        }
        return new[] { authors };
    }

    private static IReadOnlyList<string> ReadAuthors(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var el)) return Array.Empty<string>();

        if (el.ValueKind == JsonValueKind.Array)
        {
            var items = new List<string>();
            foreach (var item in el.EnumerateArray())
            {
                switch (item.ValueKind)
                {
                    case JsonValueKind.String when !string.IsNullOrWhiteSpace(item.GetString()):
                        items.Add(item.GetString()!);
                        break;
                    case JsonValueKind.Object when item.TryGetProperty("name", out var n)
                                                   && n.ValueKind == JsonValueKind.String
                                                   && !string.IsNullOrWhiteSpace(n.GetString()):
                        items.Add(n.GetString()!);
                        break;
                }
            }
            return items;
        }

        if (el.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(el.GetString()))
        {
            return new[] { el.GetString()! };
        }

        return Array.Empty<string>();
    }
}
