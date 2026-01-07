using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace DailyReflection.Avalonia.Converters;

/// <summary>
/// Converts integer/nullable types to boolean for plurality display (singular vs plural)
/// </summary>
public class PluralityConverter : IValueConverter
{
    public string? PluralValue { get; set; }
    public string? SingularValue { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int num)
        {
            return num == 1 ? SingularValue : PluralValue;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
