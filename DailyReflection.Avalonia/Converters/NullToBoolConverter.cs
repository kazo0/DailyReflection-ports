using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace DailyReflection.Avalonia.Converters;

/// <summary>
/// Returns true if value is not null (equivalent to IsNotNullConverter in MAUI Toolkit)
/// </summary>
public class NullToBoolConverter : IValueConverter
{
    public static readonly NullToBoolConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
