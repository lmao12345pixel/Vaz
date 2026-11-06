using FluentAssertions;
using Vazer.Core;
using Vazer.Core.Java;

namespace Vazer.Core.Tests;

/// <summary>
/// Đợt 18.7b: khoảng Java detect phải bao gồm Java 22–25 (tương thích ngược với
/// spec Java 21 cho MC 1.20.5+), nhưng vẫn lọc Java 26+ chưa xác nhận tương thích.
/// </summary>
public sealed class JavaResolverVersionRangeTests
{
    [Theory]
    [InlineData(8, true)]
    [InlineData(17, true)]
    [InlineData(21, true)]
    [InlineData(22, true)]
    [InlineData(23, true)]
    [InlineData(24, true)]
    [InlineData(25, true)]
    [InlineData(26, false)]
    [InlineData(7, false)]
    public void IsSupportedJavaMajor_UsesInclusiveRange_8_To_25(int major, bool expected)
    {
        JavaResolver.IsSupportedJavaMajor(major, minMajorVersion: 8, maxMajorVersion: 25)
            .Should().Be(expected);
    }

    [Fact]
    public void MinecraftOptions_Defaults_AllowJava25_AndRejectLowerThan8()
    {
        var options = new MinecraftOptions();

        options.MinJavaVersion.Should().Be(8);
        options.MaxJavaVersion.Should().Be(25);
    }
}
