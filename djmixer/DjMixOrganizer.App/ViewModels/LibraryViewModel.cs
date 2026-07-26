// File location: DjMixOrganizer.App/ViewModels/LibraryViewModel.cs

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
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
}
