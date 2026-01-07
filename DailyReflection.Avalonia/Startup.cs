using DailyReflection.Avalonia.Services;
using DailyReflection.Avalonia.Views;
using DailyReflection.Presentation.DependencyInjection;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using DailyReflection.Services.Share;
using Microsoft.Extensions.DependencyInjection;

namespace DailyReflection.Avalonia;

public static class Startup
{
    public static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Add platform-specific services
        services.AddSingleton<IShareService, DesktopShareService>();
        services.AddSingleton<ISettingsService, DesktopSettingsService>();
        services.AddSingleton<INotificationService, DesktopNotificationService>();

        // Add presentation layer (ViewModels and their dependencies)
        services.AddPresentationDependencies();

        // Add views
        services.AddTransient<MainWindow>();

        return services.BuildServiceProvider();
    }
}
