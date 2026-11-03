namespace Vazer.Core.GameOptions;

/// <summary>
/// Service quản lý đọc/ghi cấu hình options.txt của instance Minecraft.
/// </summary>
public interface IGameOptionsService
{
    /// <summary>
    /// Đọc và parse options.txt từ thư mục instance. Trả về GameOptions mặc định nếu file chưa tồn tại.
    /// </summary>
    Task<GameOptions> LoadAsync(string instancePath, CancellationToken ct = default);

    /// <summary>
    /// Serialize và lưu GameOptions ra options.txt trong thư mục instance.
    /// </summary>
    Task SaveAsync(string instancePath, GameOptions options, CancellationToken ct = default);

    /// <summary>
    /// Đọc nội dung thô (raw text) của options.txt. Trả về rỗng nếu chưa tồn tại.
    /// </summary>
    Task<string> LoadRawAsync(string instancePath, CancellationToken ct = default);

    /// <summary>
    /// Lưu nội dung thô (raw text) ra options.txt trong thư mục instance.
    /// </summary>
    Task SaveRawAsync(string instancePath, string raw, CancellationToken ct = default);
}
