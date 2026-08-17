using System.Globalization;

namespace Alfred.Localization;

public sealed record Language(string Code, string NativeName, IReadOnlyDictionary<string, string> Strings)
{
    public CultureInfo Culture { get; } = CultureInfo.GetCultureInfo(Code);
}
