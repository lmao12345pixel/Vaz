using System.Threading;

namespace Vazer.Core.Abstractions;

/// <summary>
/// Interface cho provider xác thực Minecraft.
/// </summary>
public interface IAuthProvider
{
    /// <summary>
    /// Loại tài khoản mà provider này hỗ trợ.
    /// </summary>
    AccountKind Kind { get; }

    /// <summary>
    /// Đăng nhập tương tác (interactive login) và trả về tài khoản Minecraft.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tài khoản Minecraft đã xác thực.</returns>
    Task<MinecraftAccount> LoginAsync(CancellationToken ct = default);

    /// <summary>
    /// Làm mới access token bằng refresh token (silent refresh).
    /// </summary>
    /// <param name="account">Tài khoản cần refresh.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tài khoản với token mới.</returns>
    /// <exception cref="InvalidOperationException">Nếu không thể refresh (cần login lại).</exception>
    Task<MinecraftAccount> RefreshAsync(MinecraftAccount account, CancellationToken ct = default);

    /// <summary>
    /// Đăng xuất tài khoản (xóa token local, không xóa record khỏi store).
    /// </summary>
    /// <param name="account">Tài khoản cần đăng xuất.</param>
    /// <param name="ct">Cancellation token.</param>
    Task LogoutAsync(MinecraftAccount account, CancellationToken ct = default);
}
