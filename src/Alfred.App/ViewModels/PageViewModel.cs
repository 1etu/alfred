using System.Windows.Media;
using Alfred.UIKit;
using Alfred.UIKit.Icons;

namespace Alfred.App.ViewModels;

public abstract class PageViewModel : Observable
{
    protected PageViewModel(string title, string iconKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title;
        Icon = IconLibrary.Resolve(iconKey);
    }

    public string Title { get; }

    public ImageSource Icon { get; }
}
