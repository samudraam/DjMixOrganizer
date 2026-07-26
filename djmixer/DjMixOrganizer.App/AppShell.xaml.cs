using DjMixOrganizer.App.Views;

namespace DjMixOrganizer.App;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// MixDetailPage isn't declared as a ShellContent (it's not a tab),
		// so Shell needs an explicit route registration to know what page
		// to create when something calls GoToAsync("MixDetailPage?...").
		Routing.RegisterRoute(nameof(MixDetailPage), typeof(MixDetailPage));
	}
}
