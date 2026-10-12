using System.Text.RegularExpressions;

namespace Vazer.Core.Markdown;

/// <summary>Loại token sinh ra từ markdown-lite.</summary>
public enum MarkdownTokenType
{
    Text,
    Bold,
    Link,
    LineBreak
}

/// <summary>Một token inline (text, bold, link hoặc line break).</summary>
/// <param name="Type">Loại token.</param>
/// <param name="Text">Nội dung hiển thị.</param>
/// <param name="Url">Đích link (chỉ set cho <see cref="MarkdownTokenType.Link"/>).</param>
public sealed record MarkdownToken(MarkdownTokenType Type, string Text, string? Url = null);

/// <summary>Một đoạn văn đã parse thành danh sách token.</summary>
/// <param name="Tokens">Các token theo thứ tự.</param>
public sealed record MarkdownParagraph(IReadOnlyList<MarkdownToken> Tokens);

/// <summary>
/// Markdown-lite: chỉ hỗ trợ **bold**, [text](url) và xuống dòng.
/// Mọi cú pháp khác giữ nguyên dạng literal (không throw).
/// </summary>
public static partial class MarkdownLiteParser
{
    // Nhóm URL cho phép 1 cấp ngoặc lồng để "javascript:alert(1)" không bị cắt.
    [GeneratedRegex(@"\[([^\]]+)\]\(((?:[^()]|\([^()]*\))*)\)", RegexOptions.CultureInvariant)]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"\*\*([^\*]+)\*\*", RegexOptions.CultureInvariant)]
    private static partial Regex BoldRegex();

    /// <summary>
    /// Lọc HTML: bỏ script/style (kể cả nội dung), đổi &lt;br&gt; thành \n,
    /// &lt;p&gt;...&lt;/p&gt; thành text + \n\n, rồi bỏ tag còn lại.
    /// </summary>
    public static string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var text = input;

        // Bỏ script/style kèm nội dung bên trong.
        text = Regex.Replace(text, @"<script[^>]*>.*?</script>", string.Empty,
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        text = Regex.Replace(text, @"<style[^>]*>.*?</style>", string.Empty,
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        // <br>, <br/>, <br /> -> \n
        text = Regex.Replace(text, @"<br\s*/?>", "\n",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        // <p>...</p> -> nội dung + \n\n
        text = Regex.Replace(text, @"<p\s*>(.*?)</p\s*>", "$1\n\n",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        // Bỏ mọi tag còn lại.
        text = Regex.Replace(text, @"<[^>]+>", string.Empty, RegexOptions.CultureInvariant);

        // Normalize dòng: CRLF -> LF, rồi gộp 3+ newline thành 2.
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        text = Regex.Replace(text, @"\n{3,}", "\n\n", RegexOptions.CultureInvariant);

        return text;
    }

    /// <summary>
    /// Parse markdown-lite thành danh sách đoạn, mỗi đoạn gồm các token inline.
    /// </summary>
    public static IReadOnlyList<MarkdownParagraph> Parse(string input)
    {
        var sanitized = Sanitize(input);
        if (sanitized.Length == 0) return Array.Empty<MarkdownParagraph>();

        var rawParagraphs = sanitized.Split("\n\n", StringSplitOptions.None);
        var results = new List<MarkdownParagraph>(rawParagraphs.Length);
        foreach (var raw in rawParagraphs)
        {
            var tokens = ParseInlines(raw);
            if (tokens.Count > 0)
            {
                results.Add(new MarkdownParagraph(tokens));
            }
        }

        return results;
    }

    /// <summary>
    /// Parse một đoạn văn (chưa tách paragraph) thành token inline.
    /// </summary>
    public static IReadOnlyList<MarkdownToken> ParseInlines(string paragraphText)
    {
        var tokens = new List<MarkdownToken>();
        if (string.IsNullOrEmpty(paragraphText)) return tokens;

        var lines = paragraphText.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0) tokens.Add(new MarkdownToken(MarkdownTokenType.LineBreak, "\n"));
            tokens.AddRange(ParseLine(lines[i]));
        }

        return tokens;
    }

    private static IReadOnlyList<MarkdownToken> ParseLine(string line)
    {
        var tokens = new List<MarkdownToken>();
        if (line.Length == 0) return tokens;

        // Thu thập mọi match (link trước, bold sau) rồi sort theo index.
        var matches = new List<(int Index, int Length, MarkdownToken Token)>();

        foreach (Match m in LinkRegex().Matches(line))
        {
            var label = m.Groups[1].Value;
            var url = m.Groups[2].Value.Trim();

            if (IsAllowedLinkScheme(url))
            {
                matches.Add((m.Index, m.Length, new MarkdownToken(MarkdownTokenType.Link, label, url)));
            }
            else
            {
                // javascript:, data:, file: ... -> plain text, không clickable.
                matches.Add((m.Index, m.Length, new MarkdownToken(MarkdownTokenType.Text, label)));
            }
        }

        foreach (Match m in BoldRegex().Matches(line))
        {
            matches.Add((m.Index, m.Length, new MarkdownToken(MarkdownTokenType.Bold, m.Groups[1].Value)));
        }

        matches.Sort((a, b) => a.Index.CompareTo(b.Index));

        var cursor = 0;
        foreach (var (index, length, token) in matches)
        {
            if (index < cursor) continue; // match chồng lấn -> bỏ qua match sau

            if (index > cursor)
            {
                var gap = line[cursor..index];
                AddPlainText(gap, tokens);
            }

            tokens.Add(token);
            cursor = index + length;
        }

        if (cursor < line.Length)
        {
            AddPlainText(line[cursor..], tokens);
        }

        return tokens;
    }

    private static void AddPlainText(string text, List<MarkdownToken> tokens)
    {
        if (text.Length == 0) return;

        var parts = text.Split('\n');
        for (var i = 0; i < parts.Length; i++)
        {
            if (i > 0) tokens.Add(new MarkdownToken(MarkdownTokenType.LineBreak, "\n"));
            if (parts[i].Length > 0)
            {
                tokens.Add(new MarkdownToken(MarkdownTokenType.Text, parts[i]));
            }
        }
    }

    private static bool IsAllowedLinkScheme(string url)
    {
        return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }
}
