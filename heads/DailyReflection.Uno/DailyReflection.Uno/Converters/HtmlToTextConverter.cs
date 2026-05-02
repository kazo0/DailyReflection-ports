using DailyReflection.Core.Extensions;
using Microsoft.UI.Xaml.Data;

namespace DailyReflection.Converters;

/// <summary>
/// Strips HTML tags and decodes entities from a bound string.
/// The reflection DB stores Reading wrapped in italic HTML; the XAML
/// already applies FontStyle="Italic" to that block, so stripping the
/// tags preserves the intended appearance without needing an HTML renderer.
/// </summary>
public class HtmlToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        return value is string s ? s.StripHtml() : value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
