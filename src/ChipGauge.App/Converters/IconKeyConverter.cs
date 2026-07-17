using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ChipGauge.App.Converters;

/// <summary>
/// Resolves an icon key (e.g. <c>"cpu"</c>) to the <see cref="StreamGeometry"/>
/// registered as <c>Icon.cpu</c> in the application resources.
/// </summary>
public sealed class IconKeyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key &&
            Application.Current is { } app &&
            app.TryGetResource($"Icon.{key}", app.ActualThemeVariant, out object? geometry))
        {
            return geometry;
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
