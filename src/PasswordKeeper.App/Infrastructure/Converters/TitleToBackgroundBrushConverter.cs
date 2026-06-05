using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PasswordKeeper.App.Infrastructure.Converters;

// Derives a deterministic dark-themed background brush from a title string.
// Matches the JSX algorithm: hue = (h*31 + charCode) % 360, then HSL(h, 35%, 22%).
public sealed class TitleToBackgroundBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double h = ComputeHue(value as string ?? "");
        return new SolidColorBrush(HslToRgb(h, 0.35, 0.22));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    public static double ComputeHue(string s)
    {
        long h = 0;
        foreach (char c in s) h = (h * 31 + c) % 360;
        if (h < 0) h += 360;
        return h;
    }

    public static Color HslToRgb(double h, double s, double l)
    {
        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double hp = h / 60.0;
        double x = c * (1 - Math.Abs(hp % 2 - 1));
        double r1, g1, b1;
        if (hp < 1)      { r1 = c; g1 = x; b1 = 0; }
        else if (hp < 2) { r1 = x; g1 = c; b1 = 0; }
        else if (hp < 3) { r1 = 0; g1 = c; b1 = x; }
        else if (hp < 4) { r1 = 0; g1 = x; b1 = c; }
        else if (hp < 5) { r1 = x; g1 = 0; b1 = c; }
        else             { r1 = c; g1 = 0; b1 = x; }
        double m = l - c / 2;
        return Color.FromRgb(
            (byte)Math.Clamp((r1 + m) * 255, 0, 255),
            (byte)Math.Clamp((g1 + m) * 255, 0, 255),
            (byte)Math.Clamp((b1 + m) * 255, 0, 255));
    }
}

public sealed class TitleToForegroundBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double h = TitleToBackgroundBrushConverter.ComputeHue(value as string ?? "");
        return new SolidColorBrush(TitleToBackgroundBrushConverter.HslToRgb(h, 0.45, 0.82));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
