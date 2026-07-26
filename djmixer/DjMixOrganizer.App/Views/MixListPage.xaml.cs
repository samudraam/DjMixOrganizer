using DjMixOrganizer.App.ViewModels;

namespace DjMixOrganizer.App.Views;

public partial class MixListPage : ContentPage
{
    private readonly MixListViewModel _viewModel;

    // The ViewModel is constructor-injected (wired up in MauiProgram.cs)
    // rather than `new`'d here. That's what makes it swappable in tests —
    // a unit test can construct MixListPage with a fake ViewModel instead
    // of the real one.
    public MixListPage(MixListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadMixesCommand.Execute(null);
    }
}
