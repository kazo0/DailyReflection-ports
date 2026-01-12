using Avalonia.Data.Converters;
using DailyReflection.Data.Models;
using System;
using System.Globalization;

namespace DailyReflection.Avalonia.Converters;

public class SoberTimeDisplayEnumConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SoberTimeDisplayPreference preference)
        {
            return preference switch
            {
                SoberTimeDisplayPreference.DaysMonthsYears => "Days, Months, Years",
                SoberTimeDisplayPreference.DaysOnly => "Days Only",
                _ => value.ToString()
            };
        }

        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
