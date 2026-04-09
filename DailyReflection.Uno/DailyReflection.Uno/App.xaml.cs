using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DailyReflection.Data.Databases;
using DailyReflection.Services.DailyReflection;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using DailyReflection.Services.Share;
using DailyReflection.Presentation.ViewModels;
using DailyReflection.Views;
using System.Reflection;
using Uno.Resizetizer;
using Uno.Extensions.Navigation;

namespace DailyReflection;

/// <summary>
/// Daily Reflection app for Uno Platform.
/// Migrated from .NET MAUI with the following key changes:
/// - Shell navigation replaced with TabBar from Uno.Toolkit using Region-based navigation (Uno.Extensions.Navigation)
/// - Preferences replaced with ApplicationData.LocalSettings (Uno Docs: features/settings.md)
/// - Share replaced with DataTransferManager (Uno Docs: features/windows-applicationmodel-datatransfer.md)
/// - DI configured via Uno.Extensions.Hosting with UseToolkitNavigation for TabBar region support
/// </summary>
public partial class App : Application
{
    public static IServiceProvider? ServiceProvider { get; private set; }
    public IHost? Host { get; private set; }

    /// <summary>
    /// Gets a service from the DI container.
    /// Helper method to simplify service resolution from pages.
    /// </summary>
    public static T GetService<T>() where T : class
    {
        if (ServiceProvider == null)
        {
            throw new InvalidOperationException("ServiceProvider is not initialized.");
        }
        
        return ServiceProvider.GetService<T>() 
            ?? throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
    }

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Build configuration from embedded appsettings.json
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("DailyReflection.appsettings.json");
        
        IConfiguration config;
        if (stream != null)
        {
            config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();
        }
        else
        {
            config = new ConfigurationBuilder().Build();
        }

        // Use Uno.Extensions builder for Navigation support
        // Based on Uno Platform Docs: HowTo-HostingSetup.md, HowTo-UseTabBar.md
        var builder = this.CreateBuilder(args)
            // Add navigation support for toolkit controls such as TabBar
            .UseToolkitNavigation()
            .Configure(host => host
                .ConfigureServices((context, services) =>
                {
                    // Add configuration
                    services.AddSingleton<IConfiguration>(config);
                    
                    // Add services from shared projects
                    services.AddSingleton<IDailyReflectionDatabase, DailyReflection.Data.Databases.DailyReflectionDatabase>();
                    services.AddTransient<IDailyReflectionService, DailyReflectionService>();
                    
                    // Add platform-specific services (Uno implementations)
                    services.AddSingleton<ISettingsService, PlatformServices.SettingsService>();
                    services.AddSingleton<IShareService, PlatformServices.ShareService>();
                    services.AddTransient<INotificationService, PlatformServices.NotificationService>();
                    
                    // Add ViewModels from shared Presentation project
                    services.AddSingleton<DailyReflectionViewModel>();
                    services.AddSingleton<SettingsViewModel>();
                    services.AddSingleton<SobrietyTimeViewModel>();
                })
                .UseNavigation(RegisterRoutes)
            );

        // Get the MainWindow from the builder before building
        MainWindow = builder.Window;

        // Build the host - returns IHost
        Host = builder.Build();
        ServiceProvider = Host.Services;

#if DEBUG
        MainWindow.UseStudio();
#endif

        MainWindow.SetWindowIcon();
        MainWindow.Activate();
    }

    /// <summary>
    /// Register navigation routes and view mappings for Region-based navigation.
    /// Based on Uno Platform Docs: DefineRoutes.md, RegisterRoutes.md
    /// </summary>
    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            // MainPage hosts the TabBar navigation shell
            new ViewMap<MainPage>(),
            // Tab pages for Region-based navigation
            new ViewMap<DailyReflectionPage, DailyReflectionViewModel>(),
            new ViewMap<SobrietyTimePage, SobrietyTimeViewModel>(),
            new ViewMap<SettingsPage, SettingsViewModel>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByView<MainPage>(),
                Nested:
                [
                    // Tab routes - names match uen:Region.Name in MainPage.xaml
                    new RouteMap("Reflection", View: views.FindByView<DailyReflectionPage>(), IsDefault: true),
                    new RouteMap("SoberTime", View: views.FindByView<SobrietyTimePage>()),
                    new RouteMap("Settings", View: views.FindByView<SettingsPage>())
                ]
            )
        );
    }

    public static void InitializeLogging()
    {
#if DEBUG
        var factory = LoggerFactory.Create(builder =>
        {
#if __WASM__
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__
            builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
            builder.AddConsole();
#else
            builder.AddConsole();
#endif

            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddFilter("Uno", LogLevel.Warning);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);
        });

        global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
        global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
#endif
    }
}
