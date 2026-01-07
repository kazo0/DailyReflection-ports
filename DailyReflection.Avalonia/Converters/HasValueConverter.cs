using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace DailyReflection.Avalonia.Converters;

/// <summary>
/// Returns true if value is not null
/// </summary>
public class HasValueConverter : IValueConverter
{
    public static readonly HasValueConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
