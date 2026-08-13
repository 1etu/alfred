using System.Collections.ObjectModel;
using System.Globalization;
using Alfred.Core.Items;
using Alfred.Core.Storage;

namespace Alfred.App.ViewModels;

public sealed class TodosViewModel : Observable
{
    private readonly Vault _vault;

    public TodosViewModel(Vault vault)
    {
        _vault = vault;
        _vault.Changed += (_, _) => Refresh();
        Refresh();
    }

    public ObservableCollection<TodoRow> Open { get; } = [];

    public ObservableCollection<TodoRow> Logbook { get; } = [];

    public string LogbookHeader => $"Logbook · {Logbook.Count}";

    public bool HasLogbook => Logbook.Count > 0;

    public bool IsEmpty => Open.Count == 0 && Logbook.Count == 0;

    public void Add(string title, DateOnly? due)
    {
        _vault.Data.Todos.Add(new Todo { Title = title, Due = due });
        _vault.Save();
    }

    internal void Remove(Todo todo)
    {
        _vault.Data.Todos.Remove(todo);
        _vault.Save();
    }

    internal void Persist() => _vault.Save();

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
            Meta = todo.Due is { } dueDate
                ? dueDate == today ? "Today" : dueDate.ToString("d MMM", CultureInfo.InvariantCulture)
                : string.Empty;
        }

        public Todo Todo { get; }

        public string Title => Todo.Title;

        public string Meta { get; }

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
