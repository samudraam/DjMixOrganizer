using DjMixOrganizer.App.ViewModels;
using DjMixOrganizer.App.Views;
using DjMixOrganizer.Core.Repositories;
using DjMixOrganizer.Data;
using DjMixOrganizer.Data.Repositories;
using Microsoft.EntityFrameworkCore;
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

				// Jura: titles/headings only (see Styles.xaml's ScreenTitle
				// and Section/ItemTitle styles). BPM/key/info/chip text
				// stays OpenSans.
				fonts.AddFont("JuraDemiBold.ttf", "JuraDemiBold");
				fonts.AddFont("JuraMedium.ttf", "JuraMedium");
				fonts.AddFont("JuraBook.ttf", "JuraBook");
				fonts.AddFont("JuraLight.ttf", "JuraLight");
			});

		// Composition root: this is the one place App is allowed to reference
		// Data's concrete classes directly. Everything else only ever sees
		// IMixRepository/ITrackRepository.
		//
		// AddDbContextFactory (not AddDbContext) because DbContext isn't
		// thread-safe and isn't meant to live for the app's whole lifetime —
		// the factory hands out a fresh, short-lived DjMixDbContext per
		// operation instead (see MySqlMixRepository/MySqlTrackRepository).
		// The server version is hardcoded rather than using
		// ServerVersion.AutoDetect(...), which would open a blocking network
		// connection during app startup just to ask MySQL its own version.
		var connectionString = DjMixConnectionString.FromEnvironment();
		builder.Services.AddDbContextFactory<DjMixDbContext>(options =>
			options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 46))));

		builder.Services.AddSingleton<IMixRepository, MySqlMixRepository>();
		builder.Services.AddSingleton<ITrackRepository, MySqlTrackRepository>();

		// Registering these with the DI container is what lets a page's
		// constructor ask for its ViewModel and have MAUI hand it one,
		// instead of the page having to `new SomeViewModel()` itself.
		builder.Services.AddTransient<MixListViewModel>();
		builder.Services.AddTransient<MixListPage>();
		builder.Services.AddTransient<LibraryViewModel>();
		builder.Services.AddTransient<LibraryPage>();
		builder.Services.AddTransient<MixDetailViewModel>();
		builder.Services.AddTransient<MixDetailPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
