namespace Vazer.Core.Java;

/// <summary>
/// Yêu cầu Java của một phiên bản Minecraft.
/// <para>
/// Model thuần (không I/O) để launcher so sánh Java thực tế với Java tối thiểu
/// mà không phải parse lại version string ở nhiều nơi.
/// </para>
/// </summary>
public sealed record JavaRequirements
{
    /// <summary>
    /// Không xác định được Java yêu cầu (snapshot, version custom, chuỗi rỗng).
    /// Giá trị này không bao giờ kích hoạt cảnh báo.
    /// </summary>
    public const int Unknown = 0;

    /// <summary>Version Minecraft đã dùng để tính yêu cầu (giữ nguyên input).</summary>
    public required string MinecraftVersion { get; init; }

    /// <summary>Java major version tối thiểu (8, 17, 21) hoặc <see cref="Unknown"/>.</summary>
    public required int MajorVersion { get; init; }

    /// <summary>True khi map được version Minecraft sang một Java major cụ thể.</summary>
    public bool IsKnown => MajorVersion != Unknown;

    /// <summary>Tạo yêu cầu Java từ version Minecraft.</summary>
    public static JavaRequirements For(string? minecraftVersion) => new()
    {
        MinecraftVersion = minecraftVersion ?? string.Empty,
        MajorVersion = JavaVersionMapper.Default.GetRequiredJavaVersion(minecraftVersion)
    };

    /// <summary>
    /// True khi Java thực tế không tương thích với yêu cầu (18.7c):
    /// <list type="bullet">
    /// <item><c>actual &lt; required</c> → thiếu Java, cảnh báo.</item>
    /// <item><c>actual &gt; required</c> và <c>required &lt; 21</c> → MC cũ chạy Java mới, cảnh báo.</item>
    /// <item><c>actual &gt; required</c> và <c>required &gt;= 21</c> → Java 22–25 chạy spec Java 21, OK.</item>
    /// <item><c>actual == required</c> → OK.</item>
    /// <item>Một trong hai &lt;= 0 (không xác định) → không cảnh báo.</item>
    /// </list>
    /// </summary>
    public bool IsMismatch(int actualMajorVersion) =>
        IsKnown && IsMismatch(actualMajorVersion, MajorVersion);

    /// <summary>
    /// Phiên bản static của quy tắc mismatch, cho phép test trực tiếp cặp
    /// <paramref name="actualMajorVersion"/> / <paramref name="requiredMajorVersion"/>.
    /// </summary>
    public static bool IsMismatch(int actualMajorVersion, int requiredMajorVersion)
    {
        if (actualMajorVersion <= 0 || requiredMajorVersion <= 0)
        {
            return false;
        }

        if (actualMajorVersion < requiredMajorVersion)
        {
            return true;
        }

        return actualMajorVersion > requiredMajorVersion && requiredMajorVersion < 21;
    }

    /// <summary>Message hiển thị cho user khi mismatch; không chứa quyết định block launch.</summary>
    public string BuildMismatchMessage(string? instanceName, int actualMajorVersion)
    {
        var name = string.IsNullOrWhiteSpace(instanceName) ? "Instance" : instanceName.Trim();
        return $"Instance {name} cần Java {MajorVersion}, nhưng app đang dùng Java {actualMajorVersion}. Có thể gây lỗi.";
    }
}

/// <summary>
/// Kênh cảnh báo Java không tương thích. Core không tham chiếu UI; host (WinUI)
/// cài đặt để hiển thị toast/notification. Không có implementation → chỉ log.
/// </summary>
public interface IJavaCompatibilityNotifier
{
    /// <summary>Hiển thị cảnh báo không chặn launch.</summary>
    void ShowWarning(string title, string message);
}
