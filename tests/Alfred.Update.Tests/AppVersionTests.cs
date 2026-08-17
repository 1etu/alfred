using Xunit;

namespace Alfred.Update.Tests;

public class AppVersionTests
{
    [Theory]
    [InlineData("v1.2.3", 1, 2, 3)]
    [InlineData("V2.0.1", 2, 0, 1)]
    [InlineData("1.2.3", 1, 2, 3)]
    [InlineData(" v1.2.3 ", 1, 2, 3)]
    [InlineData("1.2", 1, 2, 0)]
    public void ParsesTags(string tag, int major, int minor, int build)
    {
        Assert.True(AppVersion.TryParse(tag, out Version? version));
        Assert.Equal(new Version(major, minor, build), version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("release")]
    [InlineData("v")]
    public void RejectsJunk(string? tag)
    {
        Assert.False(AppVersion.TryParse(tag, out _));
    }
}
