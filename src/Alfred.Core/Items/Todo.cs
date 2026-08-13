namespace Alfred.Core.Items;

public sealed class Todo
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Title { get; set; }

    public DateOnly? Due { get; set; }

    public bool Done { get; set; }

    public string? Notes { get; set; }
}
