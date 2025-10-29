using System.Globalization;

namespace FocusDeck.Mobile.Converters;

/// <summary>
/// Converter that inverts boolean values (true → false, false → true).
/// Useful for toggling button enabled/disabled states.
/// </summary>
public class InvertedBoolConverter : IValueConverter
{
    /// <summary>
    /// Convert boolean to its inverted value.
    /// </summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }

    /// <summary>
    /// Convert back (inverse of Convert).
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}
