using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Alfred.App.Updates;

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
        Version? assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
        return assemblyVersion is null ? new Version(0, 0, 0) : Normalize(assemblyVersion);
    }
}
