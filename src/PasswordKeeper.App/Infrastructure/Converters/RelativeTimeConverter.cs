using System.Globalization;
using System.Windows.Data;

namespace PasswordKeeper.App.Infrastructure.Converters;

public sealed class RelativeTimeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTimeOffset dt) return string.Empty;
        var diff = DateTimeOffset.UtcNow - dt;
        if (diff.TotalSeconds < 30) return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24)   return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7)     return $"{(int)diff.TotalDays}d ago";
        if (diff.TotalDays < 30)    return $"{(int)(diff.TotalDays / 7)}w ago";
        if (diff.TotalDays < 365)   return $"{(int)(diff.TotalDays / 30)}mo ago";
        return $"{(int)(diff.TotalDays / 365)}y ago";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
