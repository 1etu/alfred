namespace Alfred.Core.Time;

public sealed record DateMatch(DateOnly Date, string Label, int Start, int Length, string Cleaned);
