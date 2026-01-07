using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DailyReflection.Avalonia.Converters;

/// <summary>
/// Returns true if all values are false
/// </summary>
public class AllFalseMultiConverter : IMultiValueConverter
{
    public static readonly AllFalseMultiConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Count == 0)
        {
            return false;
        }

        return values.All(v => v is bool b && !b);
    }
}
