using System.Collections.Generic;
using FluentAssertions;
using Vazer.Core.Graphics;
using Xunit;

namespace Vazer.Core.Tests;

public sealed class ShaderLoaderResolverTests
{
    [Theory]
    [InlineData("iris-mc1.21.1-1.7.5.jar", true)]
    [InlineData("Iris Shaders", true)]
    [InlineData("oculus-mc1.20.1.jar", true)]
    [InlineData("OptiFine_1.21.1_HD_U_I1.jar", true)]
    [InlineData("sodium-fabric-0.5.8.jar", false)]
    [InlineData("fabric-api-0.100.0.jar", false)]
    public void DetectShaderLoader_IdentifiesLoadersCorrectly(string modIdentifier, bool expected)
    {
        var mods = new List<string> { modIdentifier };
        var result = ShaderLoaderResolver.DetectShaderLoader(mods);
        result.Should().Be(expected);
    }

    [Fact]
    public void DetectShaderLoader_ReturnsFalse_WhenEmptyOrNull()
    {
        ShaderLoaderResolver.DetectShaderLoader(null!).Should().BeFalse();
        ShaderLoaderResolver.DetectShaderLoader(new List<string>()).Should().BeFalse();
    }

    [Fact]
    public void DetectShaderLoader_ExcludesFilteredOutMissingOrDisabledMods()
    {
        // Khi mod Iris bị thiếu file hoặc bị disabled, danh sách mod thực tế còn lại không chứa Iris
        var installedModsWithMissingIris = new List<(string Name, bool IsMissingFile, bool Enabled)>
        {
            ("iris-fabric-1.8.8+mc1.21.1.jar", true, true), // Thiếu file
            ("sodium-fabric-0.6.13+mc1.21.1.jar", false, true)
        };

        var filteredActiveNames = installedModsWithMissingIris
            .Where(m => !m.IsMissingFile && m.Enabled)
            .Select(m => m.Name);

        ShaderLoaderResolver.DetectShaderLoader(filteredActiveNames).Should().BeFalse("Mod thiếu file không được coi là có Shader Loader");
    }

    [Theory]
    [InlineData(34, "1.21.1", true)]
    [InlineData(34, "1.21", true)]
    [InlineData(15, "1.21.1", false)] // Incompatible format 15 on MC 1.21.1
    [InlineData(18, "1.21.1", false)] // Incompatible format 18 on MC 1.21.1
    [InlineData(15, "1.20.4", true)]  // Compatible format 15 on MC 1.20.4
    [InlineData(32, "1.20.6", true)]  // Compatible format 32 on MC 1.20.6
    public void IsPackFormatCompatible_ValidatesMinecraftVersionAgainstFormat(int format, string mcVersion, bool expected)
    {
        var result = ShaderLoaderResolver.IsPackFormatCompatible(format, mcVersion);
        result.Should().Be(expected);
    }

    [Fact]
    public void ResolveIrisVersion_ReturnsExpectedUrlAndVersion_For1211()
    {
        var (version, url, fileName) = ShaderLoaderResolver.ResolveIrisVersion("1.21.1", "fabric");
        version.Should().Contain("1.7");
        url.Should().StartWith("https://cdn.modrinth.com/");
        fileName.Should().EndWith(".jar");
    }

    [Theory]
    [InlineData("Faithful 32x", "Faithful Team", "faithful", true)]
    [InlineData("Faithful 32x", "Faithful Team", "team", true)]
    [InlineData("Bare Bones", "RobotPantaloons", "shader", false)]
    [InlineData("Bare Bones", "RobotPantaloons", "", true)]
    [InlineData("Bare Bones", "RobotPantaloons", null, true)]
    public void MatchesSearchQuery_FiltersProperly(string name, string author, string? query, bool expected)
    {
        var result = ShaderLoaderResolver.MatchesSearchQuery(name, author, query);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("1.21.1", 34)]
    [InlineData("1.20.6", 32)]
    [InlineData("1.20.1", 15)]
    public void GetExpectedPackFormat_ReturnsExpectedStandardFormat(string mcVersion, int expected)
    {
        ShaderLoaderResolver.GetExpectedPackFormat(mcVersion).Should().Be(expected);
    }
}
