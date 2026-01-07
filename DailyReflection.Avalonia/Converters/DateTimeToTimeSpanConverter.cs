using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace DailyReflection.Avalonia.Converters;

/// <summary>
/// Converts DateTime to TimeSpan and vice versa
/// </summary>
public class DateTimeToTimeSpanConverter : IValueConverter
{
    public static readonly DateTimeToTimeSpanConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            return dateTime.TimeOfDay;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan time)
        {
            return DateTime.MinValue.Add(time);
        }
        return null;
    }
}
