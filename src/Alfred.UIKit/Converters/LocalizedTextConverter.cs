using System.Globalization;
using System.Windows.Data;
using Alfred.Localization;

namespace Alfred.UIKit.Converters;

public sealed class LocalizedTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string key ? LocalizationService.Text(key) : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
