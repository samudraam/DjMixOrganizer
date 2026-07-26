using DjMixOrganizer.App.ViewModels;

namespace DjMixOrganizer.App.Views;

public partial class MixDetailPage : ContentPage
{
    public MixDetailPage(MixDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
