using Alfred.Core.Ledger;

namespace Alfred.Core.Agenda;

public enum AgendaKind
{
    Do,
    Settle,
    Know,
}

public sealed record AgendaItem(
    Guid SourceId,
    DateOnly Date,
    AgendaKind Kind,
    string Title,
    string Source,
    Money? Money,
    CashFlow? Flow,
    string? BrandSlug,
    bool IsDone,
    bool IsOverdue);
