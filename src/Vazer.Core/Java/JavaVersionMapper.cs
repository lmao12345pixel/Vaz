using System.Text.RegularExpressions;

namespace Vazer.Core.Java;

/// <summary>
/// Contract cho việc map version Minecraft sang Java major tối thiểu.
/// Tách interface để host/DI dùng chung và test thay thế dễ dàng.
/// </summary>
public interface IJavaVersionMapper
{
    /// <summary>
    /// Trả về Java major tối thiểu (8, 17, 21) hoặc <see cref="JavaRequirements.Unknown"/>.
    /// </summary>
    int GetRequiredJavaVersion(string? minecraftVersion);
}

/// <summary>
/// Map version Minecraft sang Java major tối thiểu.
/// <para>
/// Quy tắc (theo yêu cầu Phase E):
/// <list type="bullet">
/// <item>&lt; 1.17 → Java 8</item>
/// <item>1.17 – 1.20.4 → Java 17</item>
/// <item>&gt;= 1.20.5 → Java 21</item>
/// <item>Snapshot/custom không parse được → <see cref="JavaRequirements.Unknown"/> (không cảnh báo)</item>
/// </list>
/// </para>
/// </summary>
public sealed class JavaVersionMapper : IJavaVersionMapper
{
    /// <summary>Instance dùng chung cho code không cần DI.</summary>
    public static readonly JavaVersionMapper Default = new();

    // Chỉ đọc prefix số của semantic version; hậu tố -pre/-rc/build metadata bị bỏ qua.
    private static readonly Regex SemanticVersionPrefix = new(
        @"^(?<major>\d+)\.(?<minor>\d+)(?:\.(?<patch>\d+))?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <inheritdoc />
    public int GetRequiredJavaVersion(string? minecraftVersion)
    {
        if (!TryParse(minecraftVersion, out var major, out var minor, out var patch))
        {
            return JavaRequirements.Unknown;
        }

        // Tương lai (2.x) chắc chắn cần Java mới nhất đang hỗ trợ.
        if (major > 1)
        {
            return 21;
        }

        // major < 1 (ví dụ "0.30") là chuỗi lạ, không đủ tin cậy để cảnh báo.
        if (major < 1)
        {
            return JavaRequirements.Unknown;
        }

        if (minor >= 21)
        {
            return 21;
        }

        if (minor == 20)
        {
            return patch >= 5 ? 21 : 17;
        }

        return minor >= 17 ? 17 : 8;
    }

    /// <summary>
    /// Parse prefix số của version Minecraft. Trả false cho snapshot ("24w14a"),
    /// chuỗi custom hoặc input rỗng — các trường hợp này không được cảnh báo.
    /// </summary>
    public static bool TryParse(string? minecraftVersion, out int major, out int minor, out int patch)
    {
        major = 0;
        minor = 0;
        patch = 0;

        if (string.IsNullOrWhiteSpace(minecraftVersion))
        {
            return false;
        }

        var match = SemanticVersionPrefix.Match(minecraftVersion.Trim());
        if (!match.Success)
        {
            return false;
        }

        if (!int.TryParse(match.Groups["major"].Value, out major) ||
            !int.TryParse(match.Groups["minor"].Value, out minor))
        {
            return false;
        }

        if (match.Groups["patch"].Success &&
            !int.TryParse(match.Groups["patch"].Value, out patch))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// So sánh Java thực tế với yêu cầu và phát cảnh báo (không chặn launch).
    /// Không làm gì khi version không xác định hoặc Java khớp yêu cầu.
    /// </summary>
    /// <returns>True nếu đã phát cảnh báo.</returns>
    public static bool WarnIfMismatch(
        string? instanceName,
        string? minecraftVersion,
        int actualJavaMajor,
        IJavaCompatibilityNotifier? notifier)
    {
        var requirements = JavaRequirements.For(minecraftVersion);
        if (!requirements.IsMismatch(actualJavaMajor) || notifier is null)
        {
            return false;
        }

        notifier.ShowWarning(
            "Java version không phù hợp",
            requirements.BuildMismatchMessage(instanceName, actualJavaMajor));
        return true;
    }
}
