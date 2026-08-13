using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Alfred.App.ViewModels;

public sealed class ShellViewModel : Observable
{
    private readonly ObservableCollection<SidebarItem> _items = [];
    private SidebarItem _selectedItem;

    public ShellViewModel()
    {
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

    public SidebarItem SelectedItem
    {
        get => _selectedItem;
        set => Set(ref _selectedItem, value);
    }

    private void Add(string group, string title, string iconKey, int count)
    {
        ImageSource icon = (ImageSource)Application.Current.Resources[iconKey];
        _items.Add(new SidebarItem(group, title, icon) { Count = count });
    }
}
