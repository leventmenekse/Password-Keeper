using System.Globalization;
using System.Windows.Data;

namespace PasswordKeeper.App.Infrastructure.Converters;

public sealed class TitleToInitialConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = (value as string)?.Trim();
        if (string.IsNullOrEmpty(s)) return "?";
        return char.ToUpperInvariant(s[0]).ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
