// File location: DjMixOrganizer.App/ViewModels/MixDetailViewModel.cs
//
// TEACHING NOTES
// ---------------
// IQueryAttributable is MAUI Shell's way of passing data across a
// navigation boundary without two pages needing a direct reference to each
// other. MixListPage doesn't construct MixDetailViewModel and hand it a
// Mix — it just navigates to a route with a query string
// (GoToAsync($"MixDetailPage?mixId={id}")), and Shell calls
// ApplyQueryAttributes on whatever ViewModel is bound to the page it
// routed to. This is the same decoupling idea as passing an id through a
// URL instead of passing an object through props/navigation state.
//
// Tapping a track in the shared sidebar (TrackListPanel) sets
// SelectedTrackToAdd via its two-way-bound SelectedItem — that's the same
// selection mechanism LibraryPage uses, just wired to a different reaction.
// OnSelectedTrackToAddChanged (a partial method [ObservableProperty]
// generates a hook for) is where "selecting a track" becomes "add a node to
// the canvas," then immediately resets the selection to null so tapping the
// same row again re-adds it.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DjMixOrganizer.Core.Models;
using DjMixOrganizer.Core.Repositories;

namespace DjMixOrganizer.App.ViewModels;

public partial class MixDetailViewModel : ObservableObject, IQueryAttributable
{
    // Exposed as a static, compile-time-safe {x:Static} binding target for
    // the Pitch Picker in MixDetailPage.xaml — it never changes per
    // instance, so it doesn't need to be an [ObservableProperty].
    public static IReadOnlyList<string> CamelotKeys { get; } = BuildCamelotKeys();

    private static List<string> BuildCamelotKeys() =>
        Enumerable.Range(1, 12).Select(n => $"{n}A")
            .Concat(Enumerable.Range(1, 12).Select(n => $"{n}B"))
            .ToList();

    // Cycled through by EditColorCommand and assigned round-robin as nodes
    // are added — same three colors used for the Mixes page's colorized
    // titles (NodeMagenta/NodeBlue/NodeGreen in Colors.xaml).
    private static readonly string[] NodeColorPalette = ["#C21E7A", "#1E3FE0", "#1E9E3C"];

    private readonly IMixRepository _mixRepository;
    private readonly ITrackRepository _trackRepository;

    [ObservableProperty]
    private Mix? _mix;

    [ObservableProperty]
    private bool _isNewMix = true;

    [ObservableProperty]
    private ObservableCollection<TrackNode> _nodes = [];

    [ObservableProperty]
    private ObservableCollection<Track> _availableTracks = [];

    [ObservableProperty]
    private Track? _selectedTrackToAdd;

    // Set by MixDetailPage.xaml.cs when a node's drag gesture starts —
    // that's the natural "which node am I working with" signal, so
    // EditColorCommand doesn't need its own separate tap-to-select UI.
    [ObservableProperty]
    private TrackNode? _selectedNode;

    public MixDetailViewModel(IMixRepository mixRepository, ITrackRepository trackRepository)
    {
        _mixRepository = mixRepository;
        _trackRepository = trackRepository;
    }

    partial void OnSelectedTrackToAddChanged(Track? value)
    {
        if (value is null)
        {
            return;
        }

        AddNode(value);
        SelectedTrackToAdd = null;
    }

    private void AddNode(Track track)
    {
        var index = Nodes.Count;
        Nodes.Add(new TrackNode
        {
            Track = track,
            Bpm = track.Bpm ?? 120,
            Pitch = track.CamelotKey ?? "8A",
            HasMusic = true,
            Position = new CanvasPosition(20 + (index % 2) * 260, 20 + (index / 2) * 280),
            AccentColorHex = NodeColorPalette[index % NodeColorPalette.Length],
        });
    }

    [RelayCommand]
    private async Task LoadAvailableTracksAsync()
    {
        if (AvailableTracks.Count > 0)
        {
            return; // already loaded — OnAppearing can fire more than once
        }

        var tracks = await _trackRepository.GetAllAsync();
        AvailableTracks = new ObservableCollection<Track>(tracks);
    }

    // TrackNode.AccentColorHex raises PropertyChanged itself (see
    // TrackNode.cs), so mutating it here is enough for the bound Border
    // stroke to update — no need to touch the Nodes collection.
    [RelayCommand]
    private void EditColor()
    {
        var node = SelectedNode ?? Nodes.LastOrDefault();
        if (node is null)
        {
            return;
        }

        var currentIndex = Array.IndexOf(NodeColorPalette, node.AccentColorHex);
        node.AccentColorHex = NodeColorPalette[(currentIndex + 1) % NodeColorPalette.Length];
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("mixId", out var idValue) ||
            !Guid.TryParse(idValue?.ToString(), out var mixId))
        {
            IsNewMix = true;
            return;
        }

        IsNewMix = false;
        var mixes = await _mixRepository.GetAllAsync();
        Mix = mixes.FirstOrDefault(m => m.Id == mixId);

        if (Mix is null)
        {
            return;
        }

        var seeded = new List<TrackNode>();
        for (var i = 0; i < Mix.Tracks.Count; i++)
        {
            var track = Mix.Tracks[i].Track;
            seeded.Add(new TrackNode
            {
                Track = track,
                Bpm = track.Bpm ?? 120,
                Pitch = track.CamelotKey ?? "8A",
                HasMusic = true,
                Position = new CanvasPosition(20 + (i % 2) * 260, 20 + (i / 2) * 280),
                AccentColorHex = NodeColorPalette[i % NodeColorPalette.Length],
            });
        }

        Nodes = new ObservableCollection<TrackNode>(seeded);
    }
}
