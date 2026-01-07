using Avalonia.Data.Converters;
using DailyReflection.Data.Models;
using System;
using System.Globalization;

namespace DailyReflection.Avalonia.Converters;

/// <summary>
/// Converts SoberTimeDisplayPreference enum to display string
/// </summary>
public class SoberTimeDisplayEnumConverter : IValueConverter
{
    public static readonly SoberTimeDisplayEnumConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SoberTimeDisplayPreference displayPref)
        {
            return displayPref switch
            {
                SoberTimeDisplayPreference.DaysMonthsYears => "Days, Months, and Years",
                SoberTimeDisplayPreference.DaysOnly => "Days Only",
                _ => null,
            };
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
