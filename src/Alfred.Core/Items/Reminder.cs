namespace Alfred.Core.Items;

public sealed class Reminder
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Title { get; set; }

    public required DateOnly Due { get; set; }

    public TimeOnly? At { get; set; }

    public bool Done { get; set; }
}
