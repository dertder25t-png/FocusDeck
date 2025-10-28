using System.Globalization;

namespace FocusDeck.Mobile.Converters;

/// <summary>
/// Converts a percentage value (0-100) to a progress value (0-1) for ProgressBar binding
/// </summary>
public class PercentageToProgressConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double percentage)
        {
            return percentage / 100.0;
        }
        return 0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double progress)
        {
            return progress * 100.0;
        }
        return 0;
    }
}
