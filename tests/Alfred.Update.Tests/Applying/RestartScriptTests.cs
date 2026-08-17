using Alfred.Update.Applying;
using Xunit;

namespace Alfred.Update.Tests.Applying;

public class RestartScriptTests
{
    [Fact]
    public void WaitsCopiesKeepsPreviousAndRestarts()
    {
        IReadOnlyList<string> script = UpdateApplier.RestartScript(
            @"C:\staging\staged",
            @"C:\apps\Alfred",
            4242);

        Assert.Contains(script, line => line.Contains("PID eq 4242", StringComparison.Ordinal));
        Assert.Contains(script, line =>
            line.StartsWith("copy /y", StringComparison.Ordinal) &&
            line.Contains(@"C:\apps\Alfred\Alfred.exe.previous", StringComparison.Ordinal));
        Assert.Contains(script, line =>
            line.StartsWith("robocopy", StringComparison.Ordinal) &&
            line.Contains(@"C:\staging\staged", StringComparison.Ordinal) &&
            line.Contains(@"C:\apps\Alfred", StringComparison.Ordinal));
        Assert.Contains(script, line =>
            line.StartsWith("start", StringComparison.Ordinal) &&
            line.Contains(@"C:\apps\Alfred\Alfred.exe", StringComparison.Ordinal));
        Assert.Contains(script, line => line.Contains("del \"%~f0\"", StringComparison.Ordinal));
    }

    [Fact]
    public void KeepsThePreviousVersionBeforeOverwriting()
    {
        IReadOnlyList<string> script = UpdateApplier.RestartScript(@"C:\s", @"C:\i", 1);

        int keepIndex = IndexOf(script, "copy /y");
        int overwriteIndex = IndexOf(script, "robocopy");

        Assert.True(keepIndex >= 0 && overwriteIndex > keepIndex);
    }

    private static int IndexOf(IReadOnlyList<string> script, string prefix)
    {
        for (int index = 0; index < script.Count; index++)
        {
            if (script[index].StartsWith(prefix, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }
}
