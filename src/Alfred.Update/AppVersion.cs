using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Alfred.Update;

public static class AppVersion
{
    public static Version Current { get; } = ReadCurrent();

    public static bool TryParse(string? value, [MaybeNullWhen(false)] out Version version)
    {
        version = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        ReadOnlySpan<char> candidate = value.AsSpan().Trim();
        if (candidate[0] is 'v' or 'V')
        {
            candidate = candidate[1..];
        }

        if (!Version.TryParse(candidate, out Version? parsed))
        {
            return false;
        }

        version = Normalize(parsed);
        return true;
    }

    private static Version Normalize(Version version)
        => new(Math.Max(version.Major, 0), Math.Max(version.Minor, 0), Math.Max(version.Build, 0));

    private static Version ReadCurrent()
    {
        Assembly host = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        return host.GetName().Version is Version assemblyVersion ? Normalize(assemblyVersion) : new Version(0, 0, 0);
    }
}
