using System.Collections.ObjectModel;
using System.Globalization;
using Alfred.Core.Storage;

namespace Alfred.App.ViewModels;

public sealed class TrashViewModel : Observable, IToolbarHost
{
    private readonly Vault _vault;

    public TrashViewModel(Vault vault)
    {
        _vault = vault;
        Actions =
        [
            new ToolbarAction("Empty trash", "TrashGlyph", Empty),
        ];

        _vault.Changed += (_, _) => Refresh();
        Refresh();
    }

    public ObservableCollection<TrashRow> Rows { get; } = [];

    public IReadOnlyList<ToolbarAction> Actions { get; }

    public string? PrimaryActionName => null;

    public string Subtitle { get; private set; } = string.Empty;

    public bool IsEmpty => Rows.Count == 0;

    public void InvokePrimary()
    {
    }

    public void Empty()
    {
        _vault.Data.Trash.Clear();
        _vault.Save();
    }

    internal void Restore(TrashEntry entry)
    {
        if (Recycler.Restore(_vault.Data, entry))
        {
            _vault.Save();
        }
    }

    internal void Forget(TrashEntry entry)
    {
        _vault.Data.Trash.Remove(entry);
        _vault.Save();
    }

    private void Refresh()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Recycler.Purge(_vault.Data, now);

        Rows.Clear();

        foreach (TrashEntry entry in _vault.Data.Trash.OrderByDescending(entry => entry.DeletedUtc))
        {
            Rows.Add(new TrashRow(entry, this, now));
        }

        Subtitle = Rows.Count == 0
            ? "Deleted things wait here for 30 days"
            : $"{Rows.Count} recoverable · kept for 30 days";

        Raise(nameof(Subtitle));
        Raise(nameof(IsEmpty));
    }

    public sealed class TrashRow
    {
        private readonly TrashViewModel _owner;

        public TrashRow(TrashEntry entry, TrashViewModel owner, DateTimeOffset now)
        {
            Entry = entry;
            _owner = owner;

            int left = entry.DaysLeft(now);
            string kind = entry.Kind switch
            {
                TrashKind.LedgerEntry => "Payments",
                TrashKind.Todo => "TODOs",
                TrashKind.Reminder => "Reminders",
                TrashKind.Plan => "Plans",
                TrashKind.Meal => "Meals",
                TrashKind.Wish => "Wish List",
                _ => "Free Board",
            };

            Meta = $"{kind} · deleted {entry.DeletedUtc.LocalDateTime.ToString("d MMM", CultureInfo.InvariantCulture)}";
            Remaining = left == 1 ? "1 day left" : $"{left} days left";
            IsExpiring = left <= 5;
        }

        public TrashEntry Entry { get; }

        public string Title => Entry.Title;

        public string Meta { get; }

        public string Remaining { get; }

        public bool IsExpiring { get; }

        public void Restore() => _owner.Restore(Entry);

        public void Forget() => _owner.Forget(Entry);
    }
}
