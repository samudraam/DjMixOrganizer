using DjMixOrganizer.App.ViewModels;

namespace DjMixOrganizer.App.Views;

public partial class LibraryPage : ContentPage
{
    public LibraryPage(LibraryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
