using DailyReflection.DependencyInjection;
using DailyReflection.Presentation.DependencyInjection;
using DailyReflection.Presentation.ViewModels;
using DailyReflection.Views;
using Uno.Extensions;
using Uno.Extensions.Configuration;
using Uno.Extensions.Hosting;
using Uno.Extensions.Navigation;
using Uno.Resizetizer;

namespace DailyReflection;

/// <summary>
/// Daily Reflection app for Uno Platform.
/// Wires up the canonical Uno.Extensions stack:
///   - UseConfiguration with EmbeddedSource&lt;App&gt;() loads appsettings.json
///   - UseToolkitNavigation + UseNavigation drive region-based TabBar routing
///   - ConfigureServices delegates to the shared AddPresentationDependencies chain
/// </summary>
public partial class App : Application
{
    public IHost? Host { get; private set; }

    /// <summary>
    /// Resolve a service from the host's DI container.
    /// </summary>
    public static T GetService<T>() where T : class
    {
        var host = ((App)Current).Host
            ?? throw new InvalidOperationException("Host is not initialized.");

        return host.Services.GetService<T>()
            ?? throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
    }

    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .UseToolkitNavigation()
            .Configure(host => host
                .UseConfiguration(configure: configBuilder =>
                    configBuilder.EmbeddedSource<App>())
                .ConfigureServices((context, services) =>
                {
                    services.AddPlatformServices();
                    services.AddPresentationDependencies();

                    services.AddTransient<MainPage>();
                    services.AddTransient<DailyReflectionPage>();
                    services.AddTransient<SettingsPage>();
                    services.AddTransient<SobrietyTimePage>();
                })
                .UseNavigation(RegisterRoutes));

        MainWindow = builder.Window;
#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        Host = builder.Build();
    }

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap<MainPage>(),
            new ViewMap<DailyReflectionPage, DailyReflectionViewModel>(),
            new ViewMap<SobrietyTimePage, SobrietyTimeViewModel>(),
            new ViewMap<SettingsPage, SettingsViewModel>());

        routes.Register(
            new RouteMap("", View: views.FindByView<MainPage>(),
                Nested:
                [
                    new RouteMap("Reflection", View: views.FindByViewModel<DailyReflectionViewModel>(), IsDefault: true),
                    new RouteMap("SoberTime", View: views.FindByViewModel<SobrietyTimeViewModel>()),
                    new RouteMap("Settings", View: views.FindByViewModel<SettingsViewModel>()),
                ]));
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
