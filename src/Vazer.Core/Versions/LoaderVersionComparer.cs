namespace Vazer.Core.Versions;

/// <summary>
/// So sánh version loader (Fabric/Quilt/NeoForge) theo ngữ nghĩa SemVer.
/// Phần lõi (core) so sánh số theo từng segment; stable luôn xếp trên prerelease
/// cùng core; prerelease so sánh theo luật SemVer: identifier số &lt; identifier chữ,
/// identifier số so sánh số, identifier chữ so sánh ordinal (bỏ qua hoa thường),
/// prerelease ngắn hơn xếp thấp hơn khi các identifier đầu giống nhau.
/// Build metadata sau dấu '+' bị bỏ qua khi so sánh.
/// </summary>
public sealed class LoaderVersionComparer : IComparer<string>
{
    public int Compare(string? x, string? y) => CompareVersions(x, y);

    /// <summary>So sánh hai version; trả về &lt;0 nếu x &lt; y, 0 nếu bằng, &gt;0 nếu x &gt; y.</summary>
    public static int CompareVersions(string? x, string? y)
    {
        var (xCore, xPre) = Parse(x);
        var (yCore, yPre) = Parse(y);

        var coreCompare = CompareCore(xCore, yCore);
        if (coreCompare != 0)
        {
            return coreCompare;
        }

        // Stable > prerelease cùng core.
        if (xPre.Length == 0 && yPre.Length == 0)
        {
            return 0;
        }

        if (xPre.Length == 0)
        {
            return 1;
        }

        if (yPre.Length == 0)
        {
            return -1;
        }

        return ComparePrerelease(xPre, yPre);
    }

    private static (int[] Core, string[] Prerelease) Parse(string? version)
    {
        var value = (version ?? string.Empty).Trim();

        // Bỏ build metadata sau '+'.
        var plusIndex = value.IndexOf('+');
        if (plusIndex >= 0)
        {
            value = value[..plusIndex];
        }

        var dashIndex = value.IndexOf('-');
        var corePart = dashIndex < 0 ? value : value[..dashIndex];
        var prePart = dashIndex < 0 ? null : value[(dashIndex + 1)..];

        var core = corePart
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseNumericSegment)
            .ToArray();

        var prerelease = string.IsNullOrEmpty(prePart)
            ? Array.Empty<string>()
            : prePart.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return (core, prerelease);
    }

    private static int ParseNumericSegment(string segment)
    {
        // Lấy phần số dẫn đầu ("206" → 206; "1" → 1). Không có số → 0.
        var digits = new string(segment.TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) ? number : 0;
    }

    private static int CompareCore(int[] x, int[] y)
    {
        var max = Math.Max(x.Length, y.Length);
        for (var i = 0; i < max; i++)
        {
            var xv = i < x.Length ? x[i] : 0;
            var yv = i < y.Length ? y[i] : 0;
            var cmp = xv.CompareTo(yv);
            if (cmp != 0)
            {
                return cmp;
            }
        }

        return 0;
    }

    private static int ComparePrerelease(string[] x, string[] y)
    {
        var max = Math.Max(x.Length, y.Length);
        for (var i = 0; i < max; i++)
        {
            if (i >= x.Length)
            {
                return -1; // prerelease ngắn hơn xếp thấp hơn
            }

            if (i >= y.Length)
            {
                return 1;
            }

            var xi = x[i];
            var yi = y[i];
            var xNumeric = int.TryParse(xi, out var xn);
            var yNumeric = int.TryParse(yi, out var yn);

            int cmp;
            if (xNumeric && yNumeric)
            {
                cmp = xn.CompareTo(yn);
            }
            else if (xNumeric)
            {
                cmp = -1; // numeric < alphanumeric
            }
            else if (yNumeric)
            {
                cmp = 1;
            }
            else
            {
                cmp = string.Compare(xi, yi, StringComparison.OrdinalIgnoreCase);
            }

            if (cmp != 0)
            {
                return cmp;
            }
        }

        return 0;
    }
}
