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
    /// <summary>Yêu cầu phiên bản Minecraft (ví dụ ">=1.21 <1.22", "~1.20.1", "1.20.1"). Rỗng khi không chỉ định.</summary>
    public string MinecraftRequirement { get; init; } = "";
    /// <summary>Đường dẫn tệp icon bên trong archive jar (ví dụ "assets/modid/icon.png" hoặc "icon.png").</summary>
    public string IconEntryPath { get; init; } = "";
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
            var meta = ReadFabricLike(fabric, loader: "fabric", archive);
            if (meta != null) return meta;
        }

        var quilt = archive.GetEntry("quilt.mod.json");
        if (quilt != null)
        {
            var meta = ReadQuilt(quilt, archive);
            if (meta != null) return meta;
        }

        var toml = archive.GetEntry("META-INF/mods.toml");
        if (toml != null)
        {
            var meta = ReadModsToml(toml, loader: "forge", archive);
            if (meta != null) return meta;
        }

        var neoforgeToml = archive.GetEntry("META-INF/neoforge.mods.toml");
        if (neoforgeToml != null)
        {
            var meta = ReadModsToml(neoforgeToml, loader: "neoforge", archive);
            if (meta != null) return meta;
        }

        return null; // không có metadata nhận dạng được
    }

    /// <summary>fabric.mod.json (schemaVersion 1): id, version, name tùy chọn, authors là mảng string hoặc object.</summary>
    private static JarMetadata? ReadFabricLike(ZipArchiveEntry entry, string loader, ZipArchive archive)
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

            string? declaredIcon = null;
            if (root.TryGetProperty("icon", out var iconEl))
            {
                if (iconEl.ValueKind == JsonValueKind.String)
                {
                    declaredIcon = iconEl.GetString();
                }
                else if (iconEl.ValueKind == JsonValueKind.Object)
                {
                    declaredIcon = iconEl.EnumerateObject()
                        .OrderByDescending(p => int.TryParse(p.Name, out var size) ? size : 0)
                        .Select(p => p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : null)
                        .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
                }
            }
            var iconPath = ResolveIconEntry(archive, id, declaredIcon);

            string mcReq = "";
            if (root.TryGetProperty("depends", out var dependsEl) && dependsEl.ValueKind == JsonValueKind.Object)
            {
                if (dependsEl.TryGetProperty("minecraft", out var mcEl))
                {
                    if (mcEl.ValueKind == JsonValueKind.String)
                    {
                        mcReq = mcEl.GetString() ?? "";
                    }
                    else if (mcEl.ValueKind == JsonValueKind.Array)
                    {
                        var arr = mcEl.EnumerateArray()
                            .Where(e => e.ValueKind == JsonValueKind.String)
                            .Select(e => e.GetString()!)
                            .ToArray();
                        mcReq = string.Join(", ", arr);
                    }
                }
            }

            return new JarMetadata
            {
                ModId = id,
                Name = string.IsNullOrWhiteSpace(name) ? id : name,
                Version = version,
                Loader = loader,
                Authors = authors,
                Description = description,
                MinecraftRequirement = mcReq,
                IconEntryPath = iconPath
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
    private static JarMetadata? ReadQuilt(ZipArchiveEntry entry, ZipArchive archive)
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
            string? declaredIcon = null;
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

                if (metaEl.TryGetProperty("icon", out var iconEl))
                {
                    if (iconEl.ValueKind == JsonValueKind.String)
                    {
                        declaredIcon = iconEl.GetString();
                    }
                    else if (iconEl.ValueKind == JsonValueKind.Object)
                    {
                        declaredIcon = iconEl.EnumerateObject()
                            .OrderByDescending(p => int.TryParse(p.Name, out var size) ? size : 0)
                            .Select(p => p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : null)
                            .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
                    }
                }
            }
            var iconPath = ResolveIconEntry(archive, id, declaredIcon);

            return new JarMetadata
            {
                ModId = id,
                Name = string.IsNullOrWhiteSpace(name) ? id : name,
                Version = version,
                Loader = "quilt",
                Authors = authors,
                Description = description,
                IconEntryPath = iconPath
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
    private static JarMetadata? ReadModsToml(ZipArchiveEntry entry, string loader, ZipArchive archive)
    {
        try
        {
            using var stream = entry.Open();
            using var reader = new StreamReader(stream);

            var inModsBlock = false;
            string? modId = null, version = null, name = null, authors = null, description = null, logoFile = null;

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
                else if (string.Equals(key, "logoFile", StringComparison.OrdinalIgnoreCase)) logoFile ??= value;
            }

            if (string.IsNullOrWhiteSpace(modId)) return null;

            var iconPath = ResolveIconEntry(archive, modId, logoFile);

            return new JarMetadata
            {
                ModId = modId,
                Name = string.IsNullOrWhiteSpace(name) ? modId : name,
                Version = version ?? "",
                Loader = loader,
                Authors = SplitTomlAuthors(authors),
                Description = description ?? "",
                IconEntryPath = iconPath
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

    /// <summary>
    /// Kiểm tra phiên bản instance Minecraft có đáp ứng điều kiện chỉ định trong metadata mod không.
    /// Hỗ trợ các biểu thức phổ biến: exact ("1.20.1"), range (">=1.21 <1.22", "<=1.20.4"), tilde ("~1.21"), wildcard ("*").
    /// </summary>
    public static bool IsMinecraftVersionCompatible(string? requirement, string instanceMcVersion)
    {
        if (string.IsNullOrWhiteSpace(requirement) || requirement.Trim() == "*")
        {
            return true;
        }
        if (string.IsNullOrWhiteSpace(instanceMcVersion))
        {
            return true;
        }

        var cleanReq = requirement.Trim();
        var parts = cleanReq.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (Version.TryParse(NormalizeVersion(instanceMcVersion), out var instVer))
        {
            foreach (var part in parts)
            {
                var p = part.Trim();
                if (p.StartsWith(">=") && Version.TryParse(NormalizeVersion(p[2..]), out var minVer))
                {
                    if (instVer < minVer) return false;
                }
                else if (p.StartsWith("<=") && Version.TryParse(NormalizeVersion(p[2..]), out var maxVer))
                {
                    if (instVer > maxVer) return false;
                }
                else if (p.StartsWith(">") && Version.TryParse(NormalizeVersion(p[1..]), out var strictlyAbove))
                {
                    if (instVer <= strictlyAbove) return false;
                }
                else if (p.StartsWith("<") && Version.TryParse(NormalizeVersion(p[1..]), out var strictlyBelow))
                {
                    if (instVer >= strictlyBelow) return false;
                }
                else if (p.StartsWith("=") && Version.TryParse(NormalizeVersion(p[1..]), out var exactVer))
                {
                    if (instVer != exactVer) return false;
                }
                else if (p.StartsWith("~") && Version.TryParse(NormalizeVersion(p[1..]), out var tildeVer))
                {
                    if (instVer.Major != tildeVer.Major || instVer.Minor != tildeVer.Minor) return false;
                }
                else if (Version.TryParse(NormalizeVersion(p), out var simpleVer))
                {
                    if (instVer != simpleVer) return false;
                }
            }
        }

        return true;
    }

    private static string NormalizeVersion(string v)
    {
        var trimmed = v.Trim();
        var dash = trimmed.IndexOf('-');
        if (dash > 0) trimmed = trimmed[..dash];
        var parts = trimmed.Split('.');
        if (parts.Length == 2) return $"{parts[0]}.{parts[1]}.0";
        if (parts.Length >= 3) return $"{parts[0]}.{parts[1]}.{parts[2]}";
        return trimmed;
    }

    private static string ResolveIconEntry(ZipArchive archive, string modId, string? declaredIcon)
    {
        if (!string.IsNullOrWhiteSpace(declaredIcon))
        {
            var cleaned = declaredIcon.Trim().TrimStart('/', '\\');
            var entry = archive.GetEntry(cleaned);
            if (entry != null) return entry.FullName;

            entry = archive.GetEntry($"assets/{modId}/{cleaned}");
            if (entry != null) return entry.FullName;
        }

        string[] candidates =
        {
            $"assets/{modId}/icon.png",
            $"assets/{modId}/logo.png",
            $"assets/{modId}/textures/gui/icon.png",
            "icon.png",
            "logo.png",
            "pack.png"
        };

        foreach (var c in candidates)
        {
            var entry = archive.GetEntry(c);
            if (entry != null) return entry.FullName;
        }

        return "";
    }

    public static byte[]? ExtractIcon(string jarPath, string? iconEntryPath = null)
    {
        try
        {
            using var archive = ZipFile.OpenRead(jarPath);
            return ExtractIcon(archive, iconEntryPath);
        }
        catch (Exception ex) when (ex is InvalidDataException
                                        or IOException
                                        or NotSupportedException
                                        or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static byte[]? ExtractIcon(ZipArchive archive, string? iconEntryPath = null)
    {
        try
        {
            ZipArchiveEntry? entry = null;
            if (!string.IsNullOrWhiteSpace(iconEntryPath))
            {
                entry = archive.GetEntry(iconEntryPath);
            }

            if (entry == null)
            {
                var candidates = new[] { "icon.png", "logo.png", "pack.png" };
                foreach (var c in candidates)
                {
                    entry = archive.GetEntry(c);
                    if (entry != null) break;
                }
                if (entry == null)
                {
                    entry = archive.Entries.FirstOrDefault(e => e.Name.Equals("icon.png", StringComparison.OrdinalIgnoreCase)
                                                             || e.Name.Equals("logo.png", StringComparison.OrdinalIgnoreCase));
                }
            }

            if (entry == null) return null;

            using var stream = entry.Open();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    public static bool ExtractIconToFile(string jarPath, string destinationFilePath, string? iconEntryPath = null)
    {
        try
        {
            if (File.Exists(destinationFilePath) && new FileInfo(destinationFilePath).Length > 0)
            {
                return true;
            }

            var bytes = ExtractIcon(jarPath, iconEntryPath);
            if (bytes == null || bytes.Length == 0) return false;

            var dir = Path.GetDirectoryName(destinationFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllBytes(destinationFilePath, bytes);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
