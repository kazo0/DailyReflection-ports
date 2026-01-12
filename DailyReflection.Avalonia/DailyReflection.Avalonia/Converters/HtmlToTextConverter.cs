using Avalonia.Data.Converters;
using HtmlAgilityPack;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DailyReflection.Avalonia.Converters;

public partial class HtmlToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string html && !string.IsNullOrEmpty(html))
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            
            // Get text content, preserving some structure
            var text = doc.DocumentNode.InnerText;
            
            // Decode HTML entities
            text = System.Net.WebUtility.HtmlDecode(text);
            
            // Clean up excessive whitespace
            text = MultipleWhitespaceRegex().Replace(text, " ").Trim();
            
            return text;
        }

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleWhitespaceRegex();
}
