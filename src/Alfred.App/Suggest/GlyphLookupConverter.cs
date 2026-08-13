using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Alfred.App.Suggest;

public sealed class GlyphLookupConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string key && Application.Current.Resources[key] is Geometry geometry ? geometry : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
