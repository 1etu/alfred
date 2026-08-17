using System.Collections.ObjectModel;
using Alfred.App.Interop;
using Alfred.Core.Items;
using Alfred.Core.Storage;
using Alfred.Localization;
using Alfred.UIKit;
using Alfred.UIKit.Controls;

namespace Alfred.App.ViewModels;

public sealed class RemindersViewModel : PageViewModel
{
    private readonly Vault _vault;

    public RemindersViewModel(Vault vault)
        : base(LocalizationService.Text(LocalizationKeys.NavReminders), "RemindersIcon")
    {
        _vault = vault;
        Actions =
        [
            new ActionBarItem(LocalizationService.Text(LocalizationKeys.ActionCopy), "CopyGlyph", CopyToClipboard),
        ];

        _vault.Changed += (_, _) => Refresh();
        Refresh();
    }

    public override IReadOnlyList<ActionBarItem> Actions { get; }

    public ObservableCollection<object> Rows { get; } = [];

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

    private void CopyToClipboard() =>
        Clipboards.Set(string.Join(Environment.NewLine, Rows
            .OfType<ReminderRow>()
            .Select(row => $"- {row.Title}  ({row.Meta})")));

    private void Refresh()
    {
        Rows.Clear();
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        string? currentSection = null;

        foreach (Reminder reminder in _vault.Data.Reminders
            .Where(reminder => !reminder.Done)
            .OrderBy(reminder => reminder.Due)
            .ThenBy(reminder => reminder.At ?? TimeOnly.MaxValue))
        {
            (string section, bool isOverdue) = SectionFor(reminder.Due, today);

            if (section != currentSection)
            {
                currentSection = section;
                Rows.Add(new SectionRow(section, isOverdue));
            }

            Rows.Add(new ReminderRow(reminder, this, today));
        }

        Raise(nameof(IsEmpty));
    }

    private static (string Title, bool IsOverdue) SectionFor(DateOnly due, DateOnly today)
    {
        if (due < today)
        {
            return (LocalizationService.Text(LocalizationKeys.SectionOverdue), true);
        }

        if (due == today)
        {
            return (LocalizationService.Text(LocalizationKeys.NavToday), false);
        }

        if (due == today.AddDays(1))
        {
            return (LocalizationService.Text(LocalizationKeys.SectionTomorrow), false);
        }

        if (due <= today.AddDays(7))
        {
            return (LocalizationService.Text(LocalizationKeys.SectionThisWeek), false);
        }

        return (LocalizationService.Text(LocalizationKeys.SectionLater), false);
    }

    public sealed class SectionRow
    {
        public SectionRow(string title, bool isOverdue)
        {
            Title = title;
            IsOverdue = isOverdue;
        }

        public string Title { get; }

        public bool IsOverdue { get; }

        public string TintKey => IsOverdue ? "Overdue" : "TextSecondary";
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
                ? LocalizationService.Text(LocalizationKeys.NavToday)
                : reminder.Due == today.AddDays(1)
                    ? LocalizationService.Text(LocalizationKeys.SectionTomorrow)
                    : reminder.Due.ToString("ddd, d MMM", LocalizationService.Current.Culture);

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
