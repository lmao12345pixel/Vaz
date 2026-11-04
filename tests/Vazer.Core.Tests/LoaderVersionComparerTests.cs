using FluentAssertions;
using Vazer.Core.Versions;

namespace Vazer.Core.Tests;

public sealed class LoaderVersionComparerTests
{
    [Theory]
    [InlineData("21.1.206", "21.1.99", 1)]
    [InlineData("21.1.99", "21.1.206", -1)]
    [InlineData("0.15.11", "0.15.9", 1)]
    [InlineData("1.0", "1.0.0", 0)]
    [InlineData("0.17.0-beta.1", "0.15.11", 1)]
    [InlineData("0.15.11-beta", "0.15.11", -1)] // prerelease < stable cùng core
    [InlineData("0.15.11", "0.15.11-beta", 1)]
    [InlineData("0.15.11-beta.2", "0.15.11-beta.1", 1)]
    [InlineData("0.15.11-alpha", "0.15.11-beta", -1)] // alpha < beta ordinal
    [InlineData("21.1.10-beta", "21.1.10", -1)]
    [InlineData("21.1.10", "21.1.10-beta", 1)]
    public void Compare_ReturnsExpectedSign(string x, string y, int expectedSign)
        => Math.Sign(LoaderVersionComparer.CompareVersions(x, y)).Should().Be(expectedSign);

    [Fact]
    public void Compare_OrderByDescending_GivesHighestFirst()
    {
        var versions = new[] { "21.1.99", "21.1.206", "21.1.10-beta", "21.1.10", "20.2.3-beta" };

        var sorted = versions.OrderByDescending(v => v, new LoaderVersionComparer()).ToList();

        sorted.Should().Equal("21.1.206", "21.1.99", "21.1.10", "21.1.10-beta", "20.2.3-beta");
    }
}
