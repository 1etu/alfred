namespace Alfred.Core.Ledger;

public sealed class LedgerEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Title { get; set; }

    public required Money Money { get; set; }

    public required EntryKind Kind { get; set; }

    public required Schedule Schedule { get; set; }

    public string? CategoryId { get; set; }

    public string? BrandSlug { get; set; }

    public List<string> Tags { get; set; } = [];

    public string? Notes { get; set; }

    public List<DateOnly> Settled { get; set; } = [];

    public CashFlow Flow => Kind == EntryKind.Income ? CashFlow.In : CashFlow.Out;

    public bool IsSettled(DateOnly occurrence) => Settled.Contains(occurrence);

    public void Settle(DateOnly occurrence, bool settled)
    {
        if (settled && !Settled.Contains(occurrence))
        {
            Settled.Add(occurrence);
        }
        else if (!settled)
        {
            Settled.Remove(occurrence);
        }
    }
}
