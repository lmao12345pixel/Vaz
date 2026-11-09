namespace Vazer.Core.Mods;

public static class InstallNotificationHelper
{
    public static string GetReadyNotificationText(string? projectType)
    {
        return (projectType?.ToLowerInvariant()) switch
        {
            "shader" => "Shader đã sẵn sàng trong instance.",
            "resourcepack" => "Resource Pack đã sẵn sàng trong instance.",
            _ => "Mod đã sẵn sàng trong instance."
        };
    }

    public static string? ResolveNotificationImage(string? iconUrl, string[]? gallery)
    {
        if (!string.IsNullOrWhiteSpace(iconUrl)) return iconUrl;
        if (gallery != null && gallery.Length > 0 && !string.IsNullOrWhiteSpace(gallery[0]))
        {
            return gallery[0];
        }
        return null;
    }
}
