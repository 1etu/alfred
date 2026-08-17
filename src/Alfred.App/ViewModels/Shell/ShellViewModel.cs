using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Alfred.App.Preferences;
using Alfred.Core.Agenda;
using Alfred.Core.Ledger;
using Alfred.Core.Storage;
using Alfred.UIKit;
using Alfred.UIKit.Controls;
using Alfred.UIKit.Input;

namespace Alfred.App.ViewModels;

public sealed class ShellViewModel : Observable
{
    private readonly ObservableCollection<SidebarItem> _items = [];
    private readonly Dictionary<string, Func<object>> _factories;
    private readonly Dictionary<string, object> _pages = [];
    private readonly UserPreferences _preferences;
    private SidebarItem _selectedItem;
    private bool _isSettingsOpen;
    private object _currentContent = null!;

    public ShellViewModel(UserPreferences preferences, ShortcutRegistry shortcuts, Vault vault)
    {
        _preferences = preferences;
        Vault = vault;
        Shortcuts = shortcuts;
        Settings = new SettingsViewModel(preferences, shortcuts, vault);

        _factories = new Dictionary<string, Func<object>>
        {
            ["Today"] = () => new AgendaViewModel(vault, AgendaMode.Today),
            ["Upcoming"] = () => new AgendaViewModel(vault, AgendaMode.Upcoming),
            ["Plans"] = () => new PlansViewModel(vault),
            ["TODOs"] = () => new TodosViewModel(vault),
            ["Free Board"] = () => new BoardViewModel(vault),
            ["Calendar"] = () => new CalendarViewModel(vault),
            ["Reminders"] = () => new RemindersViewModel(vault),
            ["Payments"] = () => new PaymentsViewModel(vault),
            ["Wish List"] = () => new WishesViewModel(vault),
            ["Meals"] = () => new MealsViewModel(vault),
            ["Trash"] = () => new TrashViewModel(vault),
        };

        Add("overview", "Today", "TodayIcon");
        Add("overview", "Upcoming", "UpcomingIcon");
        Add("work", "Plans", "PlansIcon");
        Add("work", "TODOs", "TodosIcon");
        Add("work", "Free Board", "FreeBoardIcon");
        Add("time", "Calendar", "CalendarIcon");
        Add("time", "Reminders", "RemindersIcon");
        Add("money", "Payments", "PaymentsIcon");
        Add("money", "Wish List", "WishListIcon");
        Add("life", "Meals", "MealsIcon");

        Items = CollectionViewSource.GetDefaultView(_items);
        Items.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SidebarItem.Group)));

        _selectedItem = _items[0];
        _currentContent = Resolve("Today");

        vault.Changed += (_, _) => RefreshCounts();
        RefreshCounts();
    }

    public ICollectionView Items { get; }

    public Vault Vault { get; }

    public ShortcutRegistry Shortcuts { get; }

    public SettingsViewModel Settings { get; }

    public object CurrentContent
    {
        get => _currentContent;
        private set
        {
            if (Set(ref _currentContent, value))
            {
                Raise(nameof(ToolbarActions));
                Raise(nameof(HasToolbar));
                Raise(nameof(HasPrimaryAction));
                Raise(nameof(PrimaryActionName));
            }
        }
    }

    public IReadOnlyList<ToolbarAction>? ToolbarActions =>
        (_currentContent as IToolbarHost)?.Actions;

    public bool HasToolbar => ToolbarActions is { Count: > 0 };

    public string? PrimaryActionName => (_currentContent as IToolbarHost)?.PrimaryActionName;

    public bool HasPrimaryAction => PrimaryActionName is not null;

    public void InvokePrimary() => (_currentContent as IToolbarHost)?.InvokePrimary();

    public SidebarItem SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (Set(ref _selectedItem, value) && value is not null)
            {
                _isSettingsOpen = false;
                Raise(nameof(IsSettingsOpen));
                CurrentContent = Resolve(value.Title);
            }
        }
    }

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set
        {
            if (Set(ref _isSettingsOpen, value))
            {
                CurrentContent = value ? Settings : Resolve(_selectedItem.Title);
            }
        }
    }

    public bool IsSidebarExpanded
    {
        get => _preferences.IsSidebarExpanded;
        set
        {
            if (_preferences.IsSidebarExpanded == value)
            {
                return;
            }

            _preferences.IsSidebarExpanded = value;
            PreferencesStore.Save(_preferences);
            Raise();
        }
    }

    public void ShowTrash()
    {
        _isSettingsOpen = false;
        Raise(nameof(IsSettingsOpen));
        CurrentContent = Resolve("Trash");
    }

    public void Capture(CaptureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        DateOnly date = request.Date ?? DateOnly.FromDateTime(DateTime.Now);

        switch (request.Kind)
        {
            case CaptureKind.Todo:
                Vault.Data.Todos.Add(new Core.Items.Todo { Title = request.Title, Due = request.Date });
                break;

            case CaptureKind.Reminder:
                Vault.Data.Reminders.Add(new Core.Items.Reminder { Title = request.Title, Due = date, At = request.Time });
                break;

            case CaptureKind.Wish:
                Vault.Data.Wishes.Add(new Core.Items.WishItem
                {
                    Title = request.Title,
                    Price = request.Amount is { } price ? Money.Lira(price) : null,
                    BrandSlug = request.BrandSlug,
                });
                break;

            default:
                Vault.Data.Entries.Add(new LedgerEntry
                {
                    Title = request.Title,
                    Money = Money.Lira(request.Amount ?? 0),
                    Kind = request.Kind switch
                    {
                        CaptureKind.Payment => EntryKind.Payment,
                        CaptureKind.Income => EntryKind.Income,
                        _ => EntryKind.Expense,
                    },
                    Schedule = Schedule.Once(date),
                    BrandSlug = request.BrandSlug,
                });
                break;
        }

        Vault.Save();
    }

    public void Navigate(int index)
    {
        if (index >= 0 && index < _items.Count)
        {
            SelectedItem = _items[index];
        }
    }

    private object Resolve(string title)
    {
        if (!_pages.TryGetValue(title, out object? page))
        {
            page = _factories[title]();
            _pages[title] = page;
        }

        return page;
    }

    private void RefreshCounts()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        IReadOnlyList<AgendaItem> todayItems = AgendaService.Today(Vault.Data, today);

        SetCount("Today", todayItems.Count(item => !item.IsDone && item.Kind != AgendaKind.Know));
        SetCount("TODOs", Vault.Data.Todos.Count(todo => !todo.Done));
        SetCount("Reminders", Vault.Data.Reminders.Count(reminder => !reminder.Done && reminder.Due <= today));
        SetCount("Payments", todayItems.Count(item => item is { Kind: AgendaKind.Settle, IsDone: false }));
    }

    private void SetCount(string title, int count)
    {
        SidebarItem? item = _items.FirstOrDefault(item => item.Title == title);
        item?.Count = count;
    }

    private void Add(string group, string title, string iconKey)
    {
        ImageSource icon = (ImageSource)Application.Current.Resources[iconKey];
        _items.Add(new SidebarItem(group, title, icon));
    }
}
