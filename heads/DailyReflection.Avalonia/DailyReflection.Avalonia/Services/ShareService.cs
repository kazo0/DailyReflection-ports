using DailyReflection.Services.Share;
using System;
using System.Threading.Tasks;

namespace DailyReflection.Avalonia.Services;

/// <summary>
/// Avalonia implementation of IShareService.
/// Note: Full share functionality requires platform-specific implementation.
/// This is a basic implementation that copies text to clipboard.
/// </summary>
public class ShareService : IShareService
{
    private readonly global::Avalonia.Controls.TopLevel? _topLevel;

    public ShareService(global::Avalonia.Controls.TopLevel? topLevel = null)
    {
        _topLevel = topLevel;
    }

    public async Task ShareText(string title, string body)
    {
        try
        {
            var clipboard = _topLevel?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync($"{title}\n\n{body}");
            }
        }
        catch (Exception)
        {
            // Silently fail if clipboard is not available
        }
    }
}
