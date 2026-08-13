namespace Alfred.Core.Items;

public enum BoardColumn
{
    Backlog,
    Doing,
    Done,
}

public sealed class BoardCard
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Title { get; set; }

    public BoardColumn Column { get; set; }

    public int Tint { get; set; }

    public int Order { get; set; }
}
