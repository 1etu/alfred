using System.Windows.Media;

namespace Alfred.App.ViewModels;

public sealed class SidebarItem : Observable
{
    public SidebarItem(string group, string title, ImageSource icon)
    {
        Group = group;
        Title = title;
        Icon = icon;
    }

    public string Group { get; }

    public string Title { get; }

    public ImageSource Icon { get; }

    public int Count
    {
        get;
        set => Set(ref field, value);
    }
}
