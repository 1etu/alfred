using System.Collections.ObjectModel;
using Alfred.App.Interop;
using Alfred.Core.Items;
using Alfred.Core.Storage;
using Alfred.Localization;
using Alfred.UIKit;
using Alfred.UIKit.Controls;

namespace Alfred.App.ViewModels;

public sealed class TodosViewModel : PageViewModel
{
    private readonly Vault _vault;

    public TodosViewModel(Vault vault)
        : base(LocalizationService.Text(LocalizationKeys.NavTodos), "TodosIcon")
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

    public ObservableCollection<TodoRow> Open { get; } = [];

    public ObservableCollection<TodoRow> Logbook { get; } = [];

    public string LogbookHeader =>
        LocalizationService.Text(LocalizationKeys.SectionLogbook) + " · " + Logbook.Count;

    public bool HasLogbook => Logbook.Count > 0;

    public bool IsEmpty => Open.Count == 0 && Logbook.Count == 0;

    public void Add(string title, DateOnly? due)
    {
        _vault.Data.Todos.Add(new Todo { Title = title, Due = due });
        _vault.Save();
    }

    internal void Remove(Todo todo)
    {
        Recycler.Delete(_vault.Data, todo);
        _vault.Save();
    }

    internal void Persist() => _vault.Save();

    private void CopyToClipboard() =>
        Clipboards.Set(string.Join(Environment.NewLine, Open.Select(row => "- " + row.Title)));

    private void Refresh()
    {
        Open.Clear();
        Logbook.Clear();

        DateOnly today = DateOnly.FromDateTime(DateTime.Now);

        foreach (Todo todo in _vault.Data.Todos
            .OrderBy(todo => todo.Due ?? DateOnly.MaxValue)
            .ThenBy(todo => todo.Title, StringComparer.OrdinalIgnoreCase))
        {
            (todo.Done ? Logbook : Open).Add(new TodoRow(todo, this, today));
        }

        Raise(nameof(LogbookHeader));
        Raise(nameof(HasLogbook));
        Raise(nameof(IsEmpty));
    }

    public sealed class TodoRow : Observable
    {
        private readonly TodosViewModel _owner;

        public TodoRow(Todo todo, TodosViewModel owner, DateOnly today)
        {
            Todo = todo;
            _owner = owner;
            IsOverdue = !todo.Done && todo.Due is { } due && due < today;
            Due = todo.Due is { } dueDate
                ? dueDate == today
                    ? LocalizationService.Text(LocalizationKeys.NavToday)
                    : dueDate.ToString("d MMM", LocalizationService.Current.Culture)
                : null;
        }

        public Todo Todo { get; }

        public string Title => Todo.Title;

        public string? Due { get; }

        public bool HasDue => Due is not null;

        public bool IsOverdue { get; }

        public bool IsDone
        {
            get => Todo.Done;
            set
            {
                Todo.Done = value;
                _owner.Persist();
            }
        }

        public void Remove() => _owner.Remove(Todo);
    }
}
