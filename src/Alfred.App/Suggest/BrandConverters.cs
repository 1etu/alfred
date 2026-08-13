using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Alfred.App.Suggest;

public sealed class BrandTileImageConverter : IValueConverter
{
    private static readonly Dictionary<string, DrawingImage> Cache = [];

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string slug || BrandCatalog.Find(slug) is not Brand brand)
        {
            return null;
        }

        if (Cache.TryGetValue(slug, out DrawingImage? cached))
        {
            return cached;
        }

        DrawingGroup group = new();
        SolidColorBrush tint = Brush(brand.Hex);

        foreach (BrandPath path in brand.Paths)
        {
            Geometry geometry = Geometry.Parse(path.Data);
            geometry.Freeze();
            group.Children.Add(new GeometryDrawing(
                path.Fill is null ? tint : Brush(path.Fill.TrimStart('#')),
                null,
                geometry));
        }

        group.Freeze();
        DrawingImage image = new(group);
        image.Freeze();
        Cache[slug] = image;
        return image;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static SolidColorBrush Brush(string hex)
    {
        SolidColorBrush brush = new((Color)ColorConverter.ConvertFromString('#' + hex));
        brush.Freeze();
        return brush;
    }
}

public sealed class IsNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
