namespace Alfred.App.ViewModels;

public enum CaptureKind
{
    Todo,
    Reminder,
    Expense,
    Payment,
    Income,
    Wish,
}

public sealed record CaptureRequest(
    CaptureKind Kind,
    string Title,
    DateOnly? Date,
    TimeOnly? Time,
    decimal? Amount,
    string? BrandSlug);
