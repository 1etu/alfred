using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using Alfred.App.Preferences;
using Alfred.Core.Agenda;
using Alfred.Core.Ledger;
using Alfred.Core.Storage;
using Alfred.Localization;
using Alfred.UIKit;
using Alfred.UIKit.Controls;
using Alfred.UIKit.Icons;
using Alfred.UIKit.Input;

namespace Alfred.App.ViewModels;

public sealed class ShellViewModel : Observable
{
    private readonly ObservableCollection<SidebarItem> _items = [];
    private readonly Dictionary<string, Func<PageViewModel>> _factories = [];
    private readonly Dictionary<string, PageViewModel> _pages = [];
    private readonly Dictionary<string, string> _titleKeys = [];
    private readonly Dictionary<SidebarItem, string> _ids = [];
    private readonly UserPreferences _preferences;
    private SidebarItem _selectedItem;
    private string _currentPageId;
    private bool _isSettingsOpen;
    private object _currentContent;

    public ShellViewModel(UserPreferences preferences, ShortcutRegistry shortcuts, Vault vault)
    {
        _preferences = preferences;
        Vault = vault;
        Shortcuts = shortcuts;
        Settings = new SettingsViewModel(preferences, shortcuts, vault);

        Add("overview", "today", LocalizationKeys.NavToday, "TodayIcon");
        Add("overview", "upcoming", LocalizationKeys.NavUpcoming, "UpcomingIcon");
        Add("work", "plans", LocalizationKeys.NavPlans, "PlansIcon");
        Add("work", "todos", LocalizationKeys.NavTodos, "TodosIcon");
        Add("work", "board", LocalizationKeys.NavFreeBoard, "FreeBoardIcon");
        Add("time", "calendar", LocalizationKeys.NavCalendar, "CalendarIcon");
        Add("time", "reminders", LocalizationKeys.NavReminders, "RemindersIcon");
        Add("money", "payments", LocalizationKeys.NavPayments, "PaymentsIcon");
        Add("money", "wishes", LocalizationKeys.NavWishList, "WishListIcon");
        Add("life", "meals", LocalizationKeys.NavMeals, "MealsIcon");
        Register("trash", LocalizationKeys.NavTrash, "TrashIcon");

        _factories["meals"] = () => new MealsViewModel(vault);

        Items = CollectionViewSource.GetDefaultView(_items);
        Items.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SidebarItem.Group)));

        _selectedItem = _items[0];
        _currentPageId = "today";
        _currentContent = Resolve(_currentPageId);

        vault.Changed += (_, _) => RefreshCounts();
        LocalizationService.Changed += (_, _) => OnLanguageChanged();
        RefreshCounts();
    }

    public event EventHandler? CaptureRequested;

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
                Raise(nameof(ActionBarItems));
            }
        }
    }

    public IReadOnlyList<ActionBarItem> ActionBarItems
    {
        get
        {
            List<ActionBarItem> items =
            [
                new(LocalizationService.Text(LocalizationKeys.ActionNew), "PlusGlyph", RequestCapture, isProminent: true),
            ];

            if (_currentContent is PageViewModel page)
            {
                items.AddRange(page.Actions);
            }

            items.Add(new ActionBarItem(LocalizationService.Text(LocalizationKeys.ActionSearch), "SearchGlyph", RequestCapture));
            return items;
        }
    }

    private void RequestCapture() => CaptureRequested?.Invoke(this, EventArgs.Empty);

    public SidebarItem SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (Set(ref _selectedItem, value) && value is not null)
            {
                _isSettingsOpen = false;
                Raise(nameof(IsSettingsOpen));
                _currentPageId = _ids[value];
                CurrentContent = Resolve(_currentPageId);
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
                CurrentContent = value ? Settings : Resolve(_currentPageId);
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
        _currentPageId = "trash";
        CurrentContent = Resolve(_currentPageId);
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

    private PageViewModel Resolve(string id)
    {
        if (!_pages.TryGetValue(id, out PageViewModel? page))
        {
            page = _factories[id]();
            _pages[id] = page;
        }

        return page;
    }

    private void OnLanguageChanged()
    {
        foreach ((SidebarItem item, string id) in _ids)
        {
            item.Title = LocalizationService.Text(_titleKeys[id]);
        }

        _pages.Clear();

        if (!_isSettingsOpen)
        {
            CurrentContent = Resolve(_currentPageId);
        }

        Raise(nameof(ActionBarItems));
    }

    private void RefreshCounts()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        IReadOnlyList<AgendaItem> todayItems = AgendaService.Today(Vault.Data, today);

        SetCount("today", todayItems.Count(item => !item.IsDone && item.Kind != AgendaKind.Know));
        SetCount("todos", Vault.Data.Todos.Count(todo => !todo.Done));
        SetCount("reminders", Vault.Data.Reminders.Count(reminder => !reminder.Done && reminder.Due <= today));
        SetCount("payments", todayItems.Count(item => item is { Kind: AgendaKind.Settle, IsDone: false }));
    }

    private void SetCount(string id, int count)
    {
        SidebarItem? item = _ids.FirstOrDefault(entry => entry.Value == id).Key;
        item?.Count = count;
    }

    private void Add(string group, string id, string titleKey, string iconKey)
    {
        SidebarItem item = new(group, LocalizationService.Text(titleKey), IconLibrary.Resolve(iconKey));
        _items.Add(item);
        _ids[item] = id;
        Register(id, titleKey, iconKey);
    }

    private void Register(string id, string titleKey, string iconKey)
    {
        _titleKeys[id] = titleKey;
        _factories[id] = () => new DefaultViewModel(LocalizationService.Text(titleKey), iconKey);
    }
}
