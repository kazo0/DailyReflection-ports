using DailyReflection.DependencyInjection;
using DailyReflection.Presentation.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DailyReflection;

public static class Startup
{
		private static void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
	{
		services.AddPages();
		services.AddPresentationDependencies();
	}
}