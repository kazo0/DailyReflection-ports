using DailyReflection.Services.Share;

namespace DailyReflection.PlatformServices;

public class ShareService : IShareService
{
	public async Task ShareText(string title, string body)
		=> await Share.Default.RequestAsync(text: body, title: title);
}
