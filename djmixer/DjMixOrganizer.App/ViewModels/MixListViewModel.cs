// File location: DjMixOrganizer.App/ViewModels/MixListViewModel.cs
//
// TEACHING NOTES
// ---------------
// This is the MAUI analog of a Flutter StatefulWidget's state object, or a
// React component's `useState` + handler functions bundled together. The
// pattern is called MVVM (Model-View-ViewModel):
//
//   Model      -> Mix.cs (plain data, no UI knowledge at all)
//   ViewModel  -> this file (UI-facing state + commands, no XAML/UI code)
//   View       -> MixListPage.xaml (declarative markup that BINDS to this)
//
// The View never reaches into the ViewModel and calls methods directly the
// way a Flutter widget calls setState(). Instead, XAML *binds* to
// properties here, and the CommunityToolkit.Mvvm source generator wires up
// the plumbing (INotifyPropertyChanged) that tells the UI "hey, this
// property changed, re-render whatever's bound to it." That's the same
// underlying idea as React re-rendering when state changes — just wired
// through data-binding instead of a virtual DOM diff.
//
// [ObservableProperty] on a field generates a full public property (here,
// `Mixes` and `IsLoading`) plus the change-notification plumbing, so you
// write one line instead of ~10 lines of boilerplate.
//
// [RelayCommand] on a method generates an ICommand-wrapped version of it
// (here, `LoadMixesCommand`) that a Button in XAML can bind its `Command`
// property to. This is the MAUI equivalent of an onPressed callback in
// Flutter or an onClick handler in React — except it's declared where the
// button lives in XAML, not passed down as a prop/parameter.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DjMixOrganizer.App.Views;
using DjMixOrganizer.Core.Models;
using DjMixOrganizer.Core.Repositories;

namespace DjMixOrganizer.App.ViewModels;

// Inheriting from ObservableObject gives us INotifyPropertyChanged for free
// — required for any type whose properties the UI binds to and needs to
// react to changes on.
public partial class MixListViewModel : ObservableObject
{
    private readonly IMixRepository _mixRepository;

    // ObservableCollection, not List — ObservableCollection raises
    // CollectionChanged events when items are added/removed, which is what
    // lets a bound ListView/CollectionView in XAML update automatically
    // without you manually telling it to refresh. Plain List<T> gives you
    // no such notification.
    [ObservableProperty]
    private ObservableCollection<Mix> _mixes = [];

    [ObservableProperty]
    private bool _isLoading;

    public MixListViewModel(IMixRepository mixRepository)
    {
        _mixRepository = mixRepository;
    }

    // Wired as async even though InMemoryMixRepository has no real I/O yet
    // — that's deliberate: async-by-default is the real-time-systems habit
    // we want baked in from the start, not bolted on later. Swapping this
    // for a SQLite-backed IMixRepository later won't require touching this
    // method at all.
    [RelayCommand]
    private async Task LoadMixesAsync()
    {
        // Always re-fetch. OnAppearing fires every time you return to this
        // tab (including after Save on MixDetailPage). An early "already
        // loaded" return would hide newly saved mixes until app restart.
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        try
        {
            var mixes = await _mixRepository.GetAllAsync();
            Mixes = new ObservableCollection<Mix>(mixes);
            System.Diagnostics.Debug.WriteLine(
                $"[MixList] Loaded {Mixes.Count} mix(es) from repository.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Navigating from a ViewModel via Shell.Current (rather than an
    // injected INavigationService abstraction) is a pragmatic call, not a
    // "correct" one — a larger app would abstract this for testability.
    // For this app's size it's the standard, well-documented MAUI Shell
    // pattern, and adding an interface for one call site would be exactly
    // the kind of speculative abstraction the rest of this codebase avoids.
    [RelayCommand]
    private static async Task CreateMixAsync()
    {
        await Shell.Current.GoToAsync(nameof(MixDetailPage));
    }

    [RelayCommand]
    private static async Task OpenMixAsync(Mix mix)
    {
        await Shell.Current.GoToAsync($"{nameof(MixDetailPage)}?mixId={mix.Id}");
    }
}
