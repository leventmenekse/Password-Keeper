using System.Globalization;
using System.Windows.Data;

namespace PasswordKeeper.App.Infrastructure.Converters;

// Used to render password text masked when IsPasswordRevealed = false.
// Multi-binding: [password, isRevealed].
public sealed class PasswordMaskConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        string pw = values.Length > 0 ? (values[0] as string ?? string.Empty) : string.Empty;
        bool revealed = values.Length > 1 && values[1] is bool b && b;
        if (revealed) return pw;
        return new string('•', pw.Length);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
