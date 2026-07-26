// File location: DjMixOrganizer.App/ViewModels/LibraryViewModel.cs
//
// TEACHING NOTES
// ---------------
// SelectedTrack is bound one-way, model -> UI: the right-hand detail panel
// just displays whatever CollectionView.SelectedItem currently is. Contrast
// this with the Mix editor in Phase 4, where BPM/Pitch fields are bound
// two-way (UI -> model too, because you can type into them there). Same
// binding system, different mode, because the two panels do different jobs.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DjMixOrganizer.Core.Models;
using DjMixOrganizer.Core.Repositories;

namespace DjMixOrganizer.App.ViewModels;

public partial class LibraryViewModel : ObservableObject
{
    private readonly ITrackRepository _trackRepository;

    [ObservableProperty]
    private ObservableCollection<Track> _tracks = [];

    [ObservableProperty]
    private Track? _selectedTrack;

    [ObservableProperty]
    private bool _isLoading;

    public LibraryViewModel(ITrackRepository trackRepository)
    {
        _trackRepository = trackRepository;
    }

    // Called from LibraryPage.OnAppearing rather than waiting for a button
    // tap — the more realistic pattern noted as a stretch goal in
    // task1.md, now the default here and in MixDetailViewModel.
    [RelayCommand]
    private async Task LoadTracksAsync()
    {
        if (Tracks.Count > 0)
        {
            return; // already loaded — OnAppearing can fire more than once
        }

        IsLoading = true;
        try
        {
            var tracks = await _trackRepository.GetAllAsync();
            Tracks = new ObservableCollection<Track>(tracks);
            SelectedTrack = Tracks.FirstOrDefault();
        }
        finally
        {
            IsLoading = false;
        }
    }
}
