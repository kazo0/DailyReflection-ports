using DailyReflection.Core.Extensions;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using DailyReflection.Services.Share;
using DailyReflection.Views;


namespace DailyReflection.DependencyInjection;

public static class Dependencies
{
	public static void AddPages(this IServiceCollection services)
	{
		services.AddAllSubclassesOf<Page>(typeof(AppShell).Assembly, ServiceLifetime.Singleton);
	}

	public static void AddPlatformServices(this IServiceCollection services)
	{
		services.AddSingleton<ISettingsService, PlatformServices.SettingsService>();
		services.AddSingleton<IShareService, PlatformServices.ShareService>();
		services.AddTransient<INotificationService, PlatformServices.NotificationService>();
	}
}
