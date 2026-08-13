using Alfred.Core.Items;
using Alfred.Core.Ledger;

namespace Alfred.Core.Storage;

public sealed class VaultData
{
    public List<LedgerEntry> Entries { get; set; } = [];

    public List<Todo> Todos { get; set; } = [];

    public List<Reminder> Reminders { get; set; } = [];

    public List<Plan> Plans { get; set; } = [];

    public List<Meal> Meals { get; set; } = [];

    public List<WishItem> Wishes { get; set; } = [];

    public List<BoardCard> Cards { get; set; } = [];
}
