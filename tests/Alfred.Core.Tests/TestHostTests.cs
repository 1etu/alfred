using Xunit;

namespace Alfred.Core.Tests;

public sealed class TestHostTests
{
    [Fact]
    public void TestHostDiscoversAndRunsTests()
    {
        Assert.NotNull(typeof(TestHostTests).Assembly.GetName().Name);
    }
}
