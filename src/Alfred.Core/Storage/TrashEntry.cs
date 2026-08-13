namespace Alfred.Core.Storage;

public enum TrashKind
{
    LedgerEntry,
    Todo,
    Reminder,
    Plan,
    Meal,
    Wish,
    Card,
}

public sealed class TrashEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required TrashKind Kind { get; set; }

    public required string Title { get; set; }

    public required string Payload { get; set; }

    public required DateTimeOffset DeletedUtc { get; set; }

    public int DaysLeft(DateTimeOffset now) =>
        Math.Max(0, TrashPolicy.RetentionDays - (int)(now - DeletedUtc).TotalDays);
}

public static class TrashPolicy
{
    public const int RetentionDays = 30;

    public static bool IsExpired(TrashEntry entry, DateTimeOffset now) =>
        (now - entry.DeletedUtc).TotalDays >= RetentionDays;
}
