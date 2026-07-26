using DjMixOrganizer.App.ViewModels;
using DjMixOrganizer.App.Views;
using DjMixOrganizer.Core.Repositories;
using DjMixOrganizer.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace DjMixOrganizer.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Composition root: this is the one place App is allowed to reference
		// Data's concrete classes directly. Everything else only ever sees
		// IMixRepository. AddSingleton is fine here — it's just a list in
		// memory, no reason to recreate it per request.
		builder.Services.AddSingleton<IMixRepository, InMemoryMixRepository>();

		// Registering these with the DI container is what lets MixListPage's
		// constructor ask for a MixListViewModel and have MAUI hand it one,
		// instead of the page having to `new MixListViewModel()` itself.
		builder.Services.AddTransient<MixListViewModel>();
		builder.Services.AddTransient<MixListPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
