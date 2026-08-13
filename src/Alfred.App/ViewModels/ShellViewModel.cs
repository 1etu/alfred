using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Alfred.App.Input;
using Alfred.App.Preferences;

namespace Alfred.App.ViewModels;

public sealed class ShellViewModel : Observable
{
    private readonly ObservableCollection<SidebarItem> _items = [];
    private readonly UserPreferences _preferences;
    private SidebarItem _selectedItem;

    public ShellViewModel(UserPreferences preferences, ShortcutRegistry shortcuts)
    {
        _preferences = preferences;
        Shortcuts = shortcuts;
        Settings = new SettingsViewModel(preferences);

        Add("overview", "Today", "TodayIcon", 3);
        Add("overview", "Upcoming", "UpcomingIcon", 5);

        Add("work", "Plans", "PlansIcon", 0);
        Add("work", "TODOs", "TodosIcon", 12);
        Add("work", "Free Board", "FreeBoardIcon", 0);

        Add("time", "Calendar", "CalendarIcon", 0);
        Add("time", "Reminders", "RemindersIcon", 2);

        Add("money", "Payments", "PaymentsIcon", 1);
        Add("money", "Subscriptions", "SubscriptionsIcon", 0);
        Add("money", "Wish List", "WishListIcon", 0);

        Add("life", "Meals", "MealsIcon", 0);

        Items = CollectionViewSource.GetDefaultView(_items);
        Items.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SidebarItem.Group)));

        _selectedItem = _items[0];
    }

    public ICollectionView Items { get; }

    public ShortcutRegistry Shortcuts { get; }

    public SettingsViewModel Settings { get; }

    public SidebarItem SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (Set(ref _selectedItem, value))
            {
                IsSettingsOpen = false;
            }
        }
    }

    public bool IsSettingsOpen
    {
        get;
        set => Set(ref field, value);
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

    private void Add(string group, string title, string iconKey, int count)
    {
        ImageSource icon = (ImageSource)Application.Current.Resources[iconKey];
        _items.Add(new SidebarItem(group, title, icon) { Count = count });
    }
}

