using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PasswordKeeper.App.Infrastructure.Converters;

// Multi-binding: [strength, cellIndex]. Returns Brush.Good if strength > cellIndex, else Brush.Panel3.
public sealed class StrengthCellBrushConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        int strength = values.Length > 0 && values[0] is int s ? s : 0;
        int index    = values.Length > 1 && values[1] is int i ? i : 0;
        object resource = strength > index ? "Brush.Good" : "Brush.Panel3";
        return Application.Current?.TryFindResource(resource) ?? Brushes.Gray;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
