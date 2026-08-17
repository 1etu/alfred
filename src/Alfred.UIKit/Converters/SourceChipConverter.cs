using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Alfred.UIKit.Converters;

public sealed class SourceChipConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string suffix = parameter as string == "text" ? "Text" : "Back";

        string family = (value as string) switch
        {
            "Payments" => "Payments",
            "TODOs" => "Todos",
            "Reminders" => "Reminders",
            "Meals" => "Meals",
            "Plans" => "Plans",
            _ => "Neutral",
        };

        return Application.Current.Resources["Chip" + family + suffix] is Brush brush
            ? brush
            : Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
