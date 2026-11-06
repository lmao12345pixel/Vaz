using FluentAssertions;
using Moq;
using Vazer.Core.Java;

namespace Vazer.Core.Tests;

/// <summary>
/// Phase E (18.7): map Minecraft version → Java major và cảnh báo mismatch.
/// Thuần logic, không I/O.
/// </summary>
public sealed class JavaVersionMapperTests
{
    private readonly JavaVersionMapper _mapper = new();

    [Theory]
    [InlineData("1.8.9", 8)]
    [InlineData("1.16.5", 8)]
    [InlineData("1.17", 17)]
    [InlineData("1.20.4", 17)]
    [InlineData("1.20.5", 21)]
    [InlineData("1.21.1", 21)]
    public void GetRequiredJavaVersion_MapsRequiredCases(string mcVersion, int expected)
    {
        _mapper.GetRequiredJavaVersion(mcVersion).Should().Be(expected);
    }

    [Theory]
    [InlineData("1.17.1", 17)]
    [InlineData("1.18.2", 17)]
    [InlineData("1.19.4", 17)]
    [InlineData("1.20", 17)]
    [InlineData("1.21", 21)]
    [InlineData("1.21.1-pre1", 21)]
    public void GetRequiredJavaVersion_MapsBoundaryAndSuffixCases(string mcVersion, int expected)
    {
        _mapper.GetRequiredJavaVersion(mcVersion).Should().Be(expected);
    }

    [Theory]
    [InlineData("snapshot-24w14a")]
    [InlineData("24w14a")]
    [InlineData("custom-build")]
    [InlineData("")]
    [InlineData(null)]
    public void GetRequiredJavaVersion_Unparseable_ReturnsUnknown(string? mcVersion)
    {
        _mapper.GetRequiredJavaVersion(mcVersion).Should().Be(JavaRequirements.Unknown);
    }

    [Fact]
    public void JavaRequirements_ForKnownVersion_IsKnown()
    {
        var requirements = JavaRequirements.For("1.20.5");

        requirements.IsKnown.Should().BeTrue();
        requirements.MajorVersion.Should().Be(21);
        requirements.MinecraftVersion.Should().Be("1.20.5");
    }

    [Fact]
    public void JavaRequirements_ForSnapshot_IsUnknownAndNeverMismatch()
    {
        var requirements = JavaRequirements.For("snapshot-24w14a");

        requirements.IsKnown.Should().BeFalse();
        requirements.IsMismatch(actualMajorVersion: 8).Should().BeFalse();
    }

    [Fact]
    public void WarnIfMismatch_Notifies_WhenActualJavaDiffers()
    {
        var notifier = new Mock<IJavaCompatibilityNotifier>();

        var warned = JavaVersionMapper.WarnIfMismatch(
            "1.21.1 Fabric",
            "1.21.1",
            actualJavaMajor: 8,
            notifier.Object);

        warned.Should().BeTrue();
        notifier.Verify(
            n => n.ShowWarning(
                "Java version không phù hợp",
                It.Is<string>(m => m.Contains("cần Java 21") && m.Contains("Java 8"))),
            Times.Once);
    }

    [Fact]
    public void WarnIfMismatch_DoesNotNotify_WhenJavaMatches()
    {
        var notifier = new Mock<IJavaCompatibilityNotifier>();

        var warned = JavaVersionMapper.WarnIfMismatch("instance", "1.20.4", 17, notifier.Object);

        warned.Should().BeFalse();
        notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public void WarnIfMismatch_DoesNotNotify_ForSnapshot()
    {
        var notifier = new Mock<IJavaCompatibilityNotifier>();

        var warned = JavaVersionMapper.WarnIfMismatch("instance", "snapshot-24w14a", 8, notifier.Object);

        warned.Should().BeFalse();
        notifier.VerifyNoOtherCalls();
    }
}
