using FluentAssertions;
using Vazer.Core.Java;

namespace Vazer.Core.Tests;

/// <summary>
/// Đợt 18.7c: quy tắc mismatch mới — Java mới (22–25) chỉ bị cảnh báo khi
/// MC yêu cầu Java &lt; 21; với MC 1.20.5+ (required 21) thì Java mới là hợp lệ.
/// </summary>
public sealed class JavaRequirementsMismatchTests
{
    [Theory]
    [InlineData(17, 21, true)]  // thiếu Java
    [InlineData(25, 21, false)] // Java mới + MC mới → OK
    [InlineData(25, 8, true)]   // Java mới + MC cũ → warn
    [InlineData(21, 17, true)]  // Java mới + required < 21 → warn
    [InlineData(21, 21, false)] // khớp
    [InlineData(8, 8, false)]   // khớp
    public void IsMismatch_Static_Cases(int actual, int required, bool expected)
    {
        JavaRequirements.IsMismatch(actual, required).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 21, false)]  // actual không xác định
    [InlineData(21, 0, false)]  // required không xác định (snapshot/custom)
    [InlineData(-1, 21, false)]
    public void IsMismatch_UnknownVersions_NeverWarn(int actual, int required, bool expected)
    {
        JavaRequirements.IsMismatch(actual, required).Should().Be(expected);
    }

    [Fact]
    public void Instance_UsesRequirementsMajor()
    {
        JavaRequirements.For("1.21.1").IsMismatch(25).Should().BeFalse();
        JavaRequirements.For("1.20.4").IsMismatch(21).Should().BeTrue();
        JavaRequirements.For("1.16.5").IsMismatch(8).Should().BeFalse();
    }

    [Fact]
    public void Instance_Snapshot_NeverWarns()
    {
        JavaRequirements.For("snapshot-24w14a").IsMismatch(25).Should().BeFalse();
    }
}
