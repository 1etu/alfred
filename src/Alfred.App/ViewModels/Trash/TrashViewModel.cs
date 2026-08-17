using System.Collections.ObjectModel;
using Alfred.Core.Storage;
using Alfred.Localization;
using Alfred.UIKit;
using Alfred.UIKit.Controls;

namespace Alfred.App.ViewModels;

public sealed class TrashViewModel : PageViewModel
{
    private readonly Vault _vault;

    public TrashViewModel(Vault vault)
        : base(LocalizationService.Text(LocalizationKeys.NavTrash), "TrashIcon")
    {
        _vault = vault;
        Actions =
        [
            new ActionBarItem(LocalizationService.Text(LocalizationKeys.ActionEmptyTrash), "TrashGlyph", Empty),
        ];

        _vault.Changed += (_, _) => Refresh();
        Refresh();
    }

    public override IReadOnlyList<ActionBarItem> Actions { get; }

    public ObservableCollection<TrashRow> Rows { get; } = [];

    public string Subtitle { get; private set; } = string.Empty;

    public bool IsEmpty => Rows.Count == 0;

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
            ? LocalizationService.Text(LocalizationKeys.TrashCaptionEmpty)
            : LocalizationService.Text(LocalizationKeys.TrashCaption, Rows.Count);

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
            string kind = LocalizationService.Text(entry.Kind switch
            {
                TrashKind.LedgerEntry => LocalizationKeys.NavPayments,
                TrashKind.Todo => LocalizationKeys.NavTodos,
                TrashKind.Reminder => LocalizationKeys.NavReminders,
                TrashKind.Plan => LocalizationKeys.NavPlans,
                TrashKind.Meal => LocalizationKeys.NavMeals,
                TrashKind.Wish => LocalizationKeys.NavWishList,
                _ => LocalizationKeys.NavFreeBoard,
            });

            string deleted = entry.DeletedUtc.LocalDateTime.ToString("d MMM", LocalizationService.Current.Culture);
            Meta = kind + " · " + LocalizationService.Text(LocalizationKeys.TrashDeleted, deleted);
            Remaining = left == 1
                ? LocalizationService.Text(LocalizationKeys.TrashOneDayLeft)
                : LocalizationService.Text(LocalizationKeys.TrashDaysLeft, left);
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
