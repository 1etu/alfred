namespace Alfred.Core.Items;

public sealed class PlanStep
{
    public required string Title { get; set; }

    public bool Done { get; set; }
}

public sealed class Plan
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Title { get; set; }

    public DateOnly? Target { get; set; }

    public List<PlanStep> Steps { get; set; } = [];

    public string? Notes { get; set; }

    public bool Done { get; set; }
}
