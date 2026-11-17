namespace Vazer.Core.Abstractions;

/// <summary>
/// Thông tin instance vừa được tạo bởi <see cref="Vazer.Core.Instances.IInstanceCreationService"/>.
/// DTO nhẹ — bản ghi đầy đủ nằm trong <see cref="MinecraftInstance"/> (instance.json).
/// </summary>
public sealed record InstanceInfo
{
    /// <summary>ID instance (folder name).</summary>
    public required string Id { get; init; }

    /// <summary>Tên hiển thị.</summary>
    public required string Name { get; init; }

    /// <summary>Minecraft version.</summary>
    public required string MinecraftVersion { get; init; }

    /// <summary>Loader type (vanilla/fabric/quilt/neoforge/forge).</summary>
    public required string LoaderType { get; init; }

    /// <summary>Loader version đã cài (null cho vanilla).</summary>
    public string? LoaderVersion { get; init; }

    /// <summary>Tên version CmlLib dùng để launch (VD: "fabric-loader-0.19.5-1.21.1").</summary>
    public string? LaunchVersionName { get; init; }

    /// <summary>Thư mục instance.</summary>
    public required string Path { get; init; }

    /// <summary>Thời điểm tạo.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Chuyển sang <see cref="MinecraftInstance"/> đầy đủ (ghi instance.json).</summary>
    public MinecraftInstance ToMinecraftInstance() => new()
    {
        Id = Id,
        Name = Name,
        MinecraftVersion = MinecraftVersion,
        LoaderType = LoaderType,
        LoaderVersion = LoaderVersion,
        LaunchVersionName = LaunchVersionName,
        Path = Path,
        IconPath = null,
        CreatedAt = CreatedAt,
        LastPlayedAt = null
    };
}
