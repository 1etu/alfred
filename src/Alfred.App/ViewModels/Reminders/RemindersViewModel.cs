using System.Collections.ObjectModel;
using System.Globalization;
using Alfred.Core.Items;
using Alfred.Core.Storage;
using Alfred.UIKit;
using Alfred.UIKit.Controls;

namespace Alfred.App.ViewModels;

public sealed class RemindersViewModel : Observable, IToolbarHost
{
    private readonly Vault _vault;

    public RemindersViewModel(Vault vault)
    {
        _vault = vault;
        Actions =
        [
            new ToolbarAction("Copy list", "CopyGlyph", CopyToClipboard),
        ];

        _vault.Changed += (_, _) => Refresh();
        Refresh();
    }

    public IReadOnlyList<ToolbarAction> Actions { get; }

    public string? PrimaryActionName => "New reminder";

    public event EventHandler? PrimaryRequested;

    public void InvokePrimary() => PrimaryRequested?.Invoke(this, EventArgs.Empty);

    private void CopyToClipboard() =>
        Interop.Clipboards.Set(string.Join(Environment.NewLine, Rows.Select(row => $"- {row.Title}  ({row.Meta})")));

    public ObservableCollection<ReminderRow> Rows { get; } = [];

    public bool IsEmpty => Rows.Count == 0;

    public void Add(string title, DateOnly due, TimeOnly? at)
    {
        _vault.Data.Reminders.Add(new Reminder { Title = title, Due = due, At = at });
        _vault.Save();
    }

    internal void Remove(Reminder reminder)
    {
        Recycler.Delete(_vault.Data, reminder);
        _vault.Save();
    }

    internal void Persist() => _vault.Save();

    private void Refresh()
    {
        Rows.Clear();
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);

        foreach (Reminder reminder in _vault.Data.Reminders
            .Where(reminder => !reminder.Done)
            .OrderBy(reminder => reminder.Due)
            .ThenBy(reminder => reminder.At ?? TimeOnly.MaxValue))
        {
            Rows.Add(new ReminderRow(reminder, this, today));
        }

        Raise(nameof(IsEmpty));
    }

    public sealed class ReminderRow : Observable
    {
        private readonly RemindersViewModel _owner;

        public ReminderRow(Reminder reminder, RemindersViewModel owner, DateOnly today)
        {
            Reminder = reminder;
            _owner = owner;
            IsOverdue = reminder.Due < today;

            string day = reminder.Due == today
                ? "Today"
                : reminder.Due == today.AddDays(1)
                    ? "Tomorrow"
                    : reminder.Due.ToString("ddd, d MMM", CultureInfo.InvariantCulture);

            Meta = reminder.At is { } at ? $"{day} · {at:HH\\:mm}" : day;
        }

        public Reminder Reminder { get; }

        public string Title => Reminder.Title;

        public string Meta { get; }

        public bool IsOverdue { get; }

        public bool IsDone
        {
            get => Reminder.Done;
            set
            {
                Reminder.Done = value;
                _owner.Persist();
            }
        }

        public void Remove() => _owner.Remove(Reminder);
    }
}
