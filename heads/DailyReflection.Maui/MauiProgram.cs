using CommunityToolkit.Maui;
using DailyReflection.DependencyInjection;
using DailyReflection.Presentation.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DailyReflection;

public static class MauiProgram
{
	public static MauiApp? App { get; private set; }

	public static IServiceProvider? ServiceProvider { get; private set; }

	public static MauiApp CreateMauiApp()
	{
		var a = Assembly.GetExecutingAssembly();
		using var stream = a.GetManifestResourceStream("DailyReflection.appsettings.json");

		var config = new ConfigurationBuilder()
					.AddJsonStream(stream)
					.Build();

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("FontAwesomeBrands.otf", "FaBrandsFont");
				fonts.AddFont("FontAwesomeRegular.otf", "FaRegularFont");
				fonts.AddFont("FontAwesomeSolid.otf", "FaSolidFont");
			});

		builder.Configuration.AddConfiguration(config);



		builder.Services.AddPages();
		builder.Services.AddPlatformServices();
		builder.Services.AddPresentationDependencies();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		App = builder.Build();
		ServiceProvider = App.Services;

		return App;
	}
}
