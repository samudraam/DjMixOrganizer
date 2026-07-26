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
		// DjMixConnectionString.FromEnvironment() (below) reads process
		// environment variables — that's the right call for `dotnet ef`
		// commands run from a shell where you've `source .env`'d, but a
		// GUI-launched app (double-clicking the .app, Xcode, the iOS
		// Simulator) gets a fresh environment from launchd, not your
		// terminal's. Without this, CreateMauiApp() throws before the UI
		// ever appears — a same-process crash at launch, no dialog, nothing.
		// LoadBundledEnvVars reads a copy of .env bundled straight into the
		// app package (Resources/Raw/local.env, gitignored — never
		// committed) via FileSystem.OpenAppPackageFileAsync, the one API
		// that reads bundled assets identically across every MAUI platform,
		// including Android where MauiAsset files aren't real files on disk.
		//
		// CreateMauiApp() runs on the main thread, which already has a
		// platform UI SynchronizationContext attached — awaiting straight
		// on that thread and then blocking on .GetResult() deadlocks,
		// because the awaited continuation wants to resume on the very
		// thread that's stuck waiting for it. Task.Run moves the whole
		// operation onto a thread-pool thread first, which has no captured
		// context to deadlock against.
		Task.Run(LoadBundledEnvVars).GetAwaiter().GetResult();

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

	private static async Task LoadBundledEnvVars()
	{
		try
		{
			using var stream = await FileSystem.OpenAppPackageFileAsync("local.env");
			using var reader = new StreamReader(stream);

			string? line;
			while ((line = await reader.ReadLineAsync()) is not null)
			{
				var parts = line.Split('=', 2);
				// Never overwrites a value already set by the real process
				// environment — a genuine deployment's env vars still win.
				if (parts.Length == 2 && Environment.GetEnvironmentVariable(parts[0]) is null)
				{
					Environment.SetEnvironmentVariable(parts[0], parts[1]);
				}
			}
		}
		catch (FileNotFoundException)
		{
			// No bundled local.env — rely on whatever's already in the
			// process environment instead.
		}
	}
}
