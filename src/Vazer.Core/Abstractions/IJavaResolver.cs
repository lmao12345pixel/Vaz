using System.Threading;

namespace Vazer.Core.Abstractions;

/// <summary>
/// Thông tin một Java runtime đã detect hoặc cài đặt.
/// </summary>
public sealed record JavaInstallation
{
    /// <summary>
    /// Đường dẫn đầy đủ đến java.exe.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Major version (8, 11, 17, 21, ...).
    /// </summary>
    public required int MajorVersion { get; init; }

    /// <summary>
    /// Full version string (ví dụ: "21.0.4+7").
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Vendor (Eclipse Temurin, Oracle, Azul, ...).
    /// </summary>
    public required string Vendor { get; init; }

    /// <summary>
    /// Architecture (x64, arm64).
    /// </summary>
    public required string Architecture { get; init; }

    /// <summary>
    /// Có phải JDK không (true) hay chỉ JRE (false).
    /// </summary>
    public required bool IsJdk { get; init; }

    /// <summary>
    /// Nguồn gốc: "system", "bundled", "adoptium".
    /// </summary>
    public required string Source { get; init; }
}

/// <summary>
/// Progress report cho việc download/install Java.
/// </summary>
public sealed record InstallProgress
{
    /// <summary>
    /// Tổng bytes cần tải.
    /// </summary>
    public long TotalBytes { get; init; }

    /// <summary>
    /// Bytes đã tải.
    /// </summary>
    public long DownloadedBytes { get; init; }

    /// <summary>
    /// Phần trăm hoàn thành (0-100).
    /// </summary>
    public int PercentComplete => TotalBytes > 0 ? (int)(DownloadedBytes * 100 / TotalBytes) : 0;

    /// <summary>
    /// Trạng thái text (downloading, verifying, extracting, done).
    /// </summary>
    public string? Status { get; init; }
}

/// <summary>
/// Interface để resolve và auto-download Java runtime.
/// </summary>
public interface IJavaResolver
{
    /// <summary>
    /// Quét hệ thống tìm các Java runtime có sẵn.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Danh sách Java installation tìm thấy.</returns>
    Task<IReadOnlyList<JavaInstallation>> DiscoverAsync(CancellationToken ct = default);

    /// <summary>
    /// Đảm bảo có Java version yêu cầu, auto-download từ Adoptium nếu thiếu.
    /// </summary>
    /// <param name="requiredMajor">Major version cần thiết (8, 11, 17, 21).</param>
    /// <param name="progress">Progress reporter (optional).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Java installation đã sẵn sàng dùng.</returns>
    /// <exception cref="InvalidOperationException">Nếu không thể download/cài đặt.</exception>
    Task<JavaInstallation> EnsureAsync(int requiredMajor, IProgress<InstallProgress>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Chọn Java phù hợp nhất cho version Minecraft.
    /// </summary>
    /// <param name="minecraftVersion">Version Minecraft (ví dụ: "1.21.1").</param>
    /// <param name="available">Danh sách Java có sẵn (từ DiscoverAsync).</param>
    /// <returns>Java installation tốt nhất hoặc null nếu không có phù hợp.</returns>
    JavaInstallation? SelectBest(string minecraftVersion, IReadOnlyList<JavaInstallation> available);

    /// <summary>
    /// Xác định Java version tối thiểu yêu cầu cho Minecraft version.
    /// </summary>
    /// <param name="minecraftVersion">Version Minecraft.</param>
    /// <returns>Major version Java yêu cầu.</returns>
    int RequiredJava(string minecraftVersion);

    /// <summary>
    /// Probe một java.exe để lấy version info.
    /// </summary>
    /// <param name="javaPath">Đường dẫn java.exe.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>JavaInstallation nếu probe thành công, null nếu thất bại.</returns>
    Task<JavaInstallation?> TryProbeAsync(string javaPath, CancellationToken ct = default);
}
