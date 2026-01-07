using DailyReflection.Services.Share;
using System.Threading.Tasks;

namespace DailyReflection.Avalonia.Services;

/// <summary>
/// Desktop implementation of IShareService using clipboard
/// </summary>
public class DesktopShareService : IShareService
{
    public async Task ShareText(string title, string body)
    {
        // On desktop, we copy to clipboard instead of native share
        var clipboard = global::Avalonia.Application.Current?.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync($"{title}\n\n{body}");
        }
    }
}
