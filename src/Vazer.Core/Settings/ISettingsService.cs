namespace Vazer.Core.Settings;

/// <summary>
/// Đọc / ghi cài đặt chung của launcher thành file JSON bền vững.
/// </summary>
public interface ISettingsService
{
    /// <summary>Đường dẫn file settings.json đang dùng (hữu ích cho test/chẩn đoán).</summary>
    string SettingsFilePath { get; }

    /// <summary>Bộ giá trị mặc định theo executable spec, gắn với thư mục dữ liệu hiện tại.</summary>
    GlobalSettings CreateDefault();

    /// <summary>
    /// Nạp cài đặt. File không tồn tại / JSON hỏng → trả defaults thay vì throw.
    /// Giá trị luôn được chuẩn hóa (clamp RAM, validate enum).
    /// </summary>
    Task<GlobalSettings> LoadAsync(CancellationToken ct = default);

    /// <summary>
    /// Chuẩn hóa, ghi atomic (tmp + move) rồi trả bản đã chuẩn hóa.
    /// </summary>
    Task<GlobalSettings> SaveAsync(GlobalSettings settings, CancellationToken ct = default);
}
