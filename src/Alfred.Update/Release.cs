namespace Alfred.Update;

public sealed record Release(
    Version Version,
    string Tag,
    string Notes,
    string DownloadUrl,
    long SizeBytes,
    DateTimeOffset PublishedUtc);
