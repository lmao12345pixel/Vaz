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
    /// True khi biết Java yêu cầu và Java thực tế khác yêu cầu.
    /// Java thực tế không xác định (&lt;= 0) được coi là không đủ dữ kiện → không cảnh báo.
    /// </summary>
    public bool IsMismatch(int actualMajorVersion) =>
        IsKnown && actualMajorVersion > 0 && actualMajorVersion != MajorVersion;

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
