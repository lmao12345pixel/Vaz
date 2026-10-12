using System.Collections.Generic;
using System.Threading;

namespace Vazer.Core.Abstractions;

/// <summary>
/// Store quản lý nhiều tài khoản Minecraft (multi-account).
/// </summary>
public interface IAccountStore
{
    /// <summary>
    /// Lấy danh sách tất cả tài khoản đã lưu.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Danh sách tài khoản (không bao gồm token nhạy cảm).</returns>
    Task<IReadOnlyList<MinecraftAccount>> ListAsync(CancellationToken ct = default);

    /// <summary>
    /// Lấy tài khoản theo ID.
    /// </summary>
    /// <param name="id">UUID của tài khoản.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tài khoản nếu tìm thấy, null nếu không.</returns>
    Task<MinecraftAccount?> GetAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Lưu hoặc cập nhật tài khoản.
    /// </summary>
    /// <param name="account">Tài khoản cần lưu.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SaveAsync(MinecraftAccount account, CancellationToken ct = default);

    /// <summary>
    /// Xóa tài khoản theo ID.
    /// </summary>
    /// <param name="id">UUID của tài khoản.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Lấy tài khoản đang active (được chọn mặc định khi launch).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tài khoản active hoặc null nếu chưa set.</returns>
    Task<MinecraftAccount?> GetActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Set tài khoản làm active.
    /// </summary>
    /// <param name="id">UUID của tài khoản.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetActiveAsync(string id, CancellationToken ct = default);
}
