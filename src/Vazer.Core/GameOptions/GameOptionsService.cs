using System.Text;

namespace Vazer.Core.GameOptions;

/// <summary>
/// Triển khai IGameOptionsService cho file options.txt.
/// </summary>
public sealed class GameOptionsService : IGameOptionsService
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    public async Task<GameOptions> LoadAsync(string instancePath, CancellationToken ct = default)
    {
        var raw = await LoadRawAsync(instancePath, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new GameOptions();
        }

        return GameOptionsParser.Parse(raw);
    }

    public async Task SaveAsync(string instancePath, GameOptions options, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var text = GameOptionsParser.Serialize(options);
        await SaveRawAsync(instancePath, text, ct).ConfigureAwait(false);
    }

    public async Task<string> LoadRawAsync(string instancePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(instancePath))
        {
            return string.Empty;
        }

        var filePath = GetOptionsFilePath(instancePath);
        if (!File.Exists(filePath))
        {
            return string.Empty;
        }

        return await File.ReadAllTextAsync(filePath, Encoding.UTF8, ct).ConfigureAwait(false);
    }

    public async Task SaveRawAsync(string instancePath, string raw, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(instancePath))
        {
            throw new ArgumentException("Instance path cannot be null or empty.", nameof(instancePath));
        }

        var filePath = GetOptionsFilePath(instancePath);
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllTextAsync(filePath, raw ?? string.Empty, Utf8NoBom, ct).ConfigureAwait(false);
    }

    private static string GetOptionsFilePath(string instancePath) =>
        Path.Combine(instancePath, "options.txt");
}
