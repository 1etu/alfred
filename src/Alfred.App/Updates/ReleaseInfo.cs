namespace Alfred.App.Updates;

public sealed record ReleaseInfo(
    Version Version,
    string Tag,
    string Notes,
    string DownloadUrl,
    long SizeBytes,
    DateTimeOffset PublishedUtc);
