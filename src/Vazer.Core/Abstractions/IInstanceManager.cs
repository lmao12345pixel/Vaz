using System.Collections.Generic;
using System.Threading;

namespace Vazer.Core.Abstractions;

/// <summary>
/// Thông tin một Minecraft instance.
/// </summary>
public sealed record MinecraftInstance
{
    /// <summary>
    /// Instance ID (unique, dùng làm folder name).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Tên hiển thị.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Minecraft version (ví dụ: "1.21.1").
    /// </summary>
    public required string MinecraftVersion { get; init; }

    /// <summary>
    /// Mod loader type (vanilla, fabric, neoforge, forge, quilt).
    /// </summary>
    public required string LoaderType { get; init; }

    /// <summary>
    /// Loader version (ví dụ: "0.15.11" cho Fabric).
    /// </summary>
    public string? LoaderVersion { get; init; }

    /// <summary>
    /// Đường dẫn thư mục instance.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Icon path (optional).
    /// </summary>
    public string? IconPath { get; init; }

    /// <summary>
    /// Thời điểm tạo.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Thời điểm lần cuối chơi.
    /// </summary>
    public DateTimeOffset? LastPlayedAt { get; init; }
}

/// <summary>
/// Interface quản lý các Minecraft instance.
/// </summary>
public interface IInstanceManager
{
    /// <summary>
    /// Lấy danh sách tất cả instance.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Danh sách instance.</returns>
    Task<IReadOnlyList<MinecraftInstance>> ListAsync(CancellationToken ct = default);

    /// <summary>
    /// Lấy instance theo ID.
    /// </summary>
    /// <param name="id">Instance ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Instance nếu tìm thấy, null nếu không.</returns>
    Task<MinecraftInstance?> GetAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Tạo instance mới (vanilla).
    /// </summary>
    /// <param name="name">Tên instance.</param>
    /// <param name="minecraftVersion">Minecraft version.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Instance vừa tạo.</returns>
    Task<MinecraftInstance> CreateVanillaAsync(string name, string minecraftVersion, CancellationToken ct = default);

    /// <summary>
    /// Tạo instance với Fabric loader.
    /// </summary>
    /// <param name="name">Tên instance.</param>
    /// <param name="minecraftVersion">Minecraft version.</param>
    /// <param name="fabricVersion">Fabric loader version (optional, lấy latest nếu null).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Instance vừa tạo.</returns>
    Task<MinecraftInstance> CreateFabricAsync(string name, string minecraftVersion, string? fabricVersion = null, CancellationToken ct = default);

    /// <summary>
    /// Tạo instance với NeoForge loader.
    /// </summary>
    /// <param name="name">Tên instance.</param>
    /// <param name="minecraftVersion">Minecraft version.</param>
    /// <param name="neoforgeVersion">NeoForge version (optional, lấy latest nếu null).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Instance vừa tạo.</returns>
    Task<MinecraftInstance> CreateNeoForgeAsync(string name, string minecraftVersion, string? neoforgeVersion = null, CancellationToken ct = default);

    /// <summary>
    /// Xóa instance.
    /// </summary>
    /// <param name="id">Instance ID.</param>
    /// <param name="deleteFiles">Có xóa luôn file trên disk không.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(string id, bool deleteFiles = false, CancellationToken ct = default);

    /// <summary>
    /// Cập nhật last played time.
    /// </summary>
    /// <param name="id">Instance ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateLastPlayedAsync(string id, CancellationToken ct = default);
}
