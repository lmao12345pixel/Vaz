using System.Threading;
using Vazer.Core.Abstractions;
using Vazer.Core.Loaders;

namespace Vazer.Core.Instances;

/// <summary>
/// Facade cho "tạo instance": chọn đúng <see cref="ILoaderInstaller"/> theo loại loader,
/// cài đặt, ghi instance.json, trả về <see cref="InstanceInfo"/>.
/// </summary>
public interface IInstanceCreationService
{
    /// <summary>
    /// Tạo instance mới với loader chỉ định.
    /// </summary>
    /// <param name="name">Tên instance (dùng làm folder name, đã được sanitize).</param>
    /// <param name="mcVersion">Minecraft version.</param>
    /// <param name="loaderType">vanilla | fabric | quilt | neoforge | forge.</param>
    /// <param name="loaderVersion">Loader version; null = bản mới nhất (loader không hỗ trợ null sẽ tự resolve).</param>
    /// <param name="progress">Progress reporter (Stage/Percent/Message).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>InstanceInfo của instance vừa tạo.</returns>
    /// <exception cref="InvalidOperationException">Loader không hỗ trợ hoặc không resolve được version.</exception>
    Task<InstanceInfo> CreateAsync(
        string name,
        string mcVersion,
        string loaderType,
        string? loaderVersion,
        IProgress<InstallerProgress>? progress,
        CancellationToken ct = default);
}
