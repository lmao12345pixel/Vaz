using FluentAssertions;
using Vazer.Core.Mods;

namespace Vazer.Core.Tests;

public sealed class InstallNotificationHelperTests
{
    [Theory]
    [InlineData("shader", "Shader đã sẵn sàng trong instance.")]
    [InlineData("SHADER", "Shader đã sẵn sàng trong instance.")]
    [InlineData("resourcepack", "Resource Pack đã sẵn sàng trong instance.")]
    [InlineData("ResourcePack", "Resource Pack đã sẵn sàng trong instance.")]
    [InlineData("mod", "Mod đã sẵn sàng trong instance.")]
    [InlineData(null, "Mod đã sẵn sàng trong instance.")]
    [InlineData("", "Mod đã sẵn sàng trong instance.")]
    public void GetReadyNotificationText_ReturnsCorrectMessage_ForEachProjectType(string? projectType, string expected)
    {
        var result = InstallNotificationHelper.GetReadyNotificationText(projectType);
        result.Should().Be(expected);
    }

    [Fact]
    public void ResolveNotificationImage_PrefersIconUrl_WhenAvailable()
    {
        var iconUrl = "https://cdn.modrinth.com/icon.png";
        var gallery = new[] { "https://cdn.modrinth.com/gallery1.png", "https://cdn.modrinth.com/gallery2.png" };

        var result = InstallNotificationHelper.ResolveNotificationImage(iconUrl, gallery);

        result.Should().Be(iconUrl);
    }

    [Fact]
    public void ResolveNotificationImage_FallbacksToGalleryFirstImage_WhenIconUrlEmpty()
    {
        var gallery = new[] { "https://cdn.modrinth.com/gallery1.png", "https://cdn.modrinth.com/gallery2.png" };

        var result = InstallNotificationHelper.ResolveNotificationImage(null, gallery);

        result.Should().Be("https://cdn.modrinth.com/gallery1.png");

        var resultEmptyIcon = InstallNotificationHelper.ResolveNotificationImage("   ", gallery);
        resultEmptyIcon.Should().Be("https://cdn.modrinth.com/gallery1.png");
    }
}
