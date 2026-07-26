// File location: DjMixOrganizer.Core/Models/TrackNode.cs
//
// TEACHING NOTES
// ---------------
// This is the node-based mix editor brainstormed in README.md, made
// concrete. A TrackNode is "this Track, placed in this one Mix's canvas" —
// BPM/pitch/vocals-percussion-music toggles live here, not on Track itself,
// because they're per-use-in-this-mix, not intrinsic to the audio (the same
// track could appear pitched differently in two different mixes). Same
// reasoning as MixTrackEntry.StartTime in Mix.cs — a property of the
// track's PLACEMENT, not the track.
//
// CanvasPosition is presentation state (where the node sits on screen), not
// a domain invariant — same distinction the README already draws for why
// MixCanvas is kept separate from Mix.
//
// WHY INotifyPropertyChanged IS IMPLEMENTED BY HAND HERE
// The App layer's ViewModels use CommunityToolkit.Mvvm's [ObservableProperty]
// to generate this same plumbing. Core stays free of that (or any) NuGet
// dependency — but INotifyPropertyChanged itself lives in
// System.ComponentModel, part of the base class library, not a package, so
// implementing it directly doesn't violate Core's zero-dependency rule.
// AccentColorHex needs this: the Mix editor's "Edit Color" button changes it
// on an existing node, and the bound Border.Stroke needs to know to
// re-render. Bpm/Pitch/the toggles don't strictly need it (nothing else
// reads them reactively), but they're wired the same way for consistency.
// Position is the one exception — it's deliberately NOT observable; see
// MixDetailPage.xaml.cs for why (drag uses a visual transform, not binding).

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DjMixOrganizer.Core.Models;

public record CanvasPosition(double X, double Y);

public class TrackNode : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public Guid Id { get; init; } = Guid.NewGuid();

    public required Track Track { get; set; }

    private double _bpm;
    public double Bpm
    {
        get => _bpm;
        set { _bpm = value; OnPropertyChanged(); }
    }

    private string _pitch = "8A";
    public string Pitch
    {
        get => _pitch;
        set { _pitch = value; OnPropertyChanged(); }
    }

    private bool _hasVocals;
    public bool HasVocals
    {
        get => _hasVocals;
        set { _hasVocals = value; OnPropertyChanged(); }
    }

    private bool _hasPercussion;
    public bool HasPercussion
    {
        get => _hasPercussion;
        set { _hasPercussion = value; OnPropertyChanged(); }
    }

    private bool _hasMusic = true;
    public bool HasMusic
    {
        get => _hasMusic;
        set { _hasMusic = value; OnPropertyChanged(); }
    }

    // Not observable on purpose — see the file-level teaching note.
    public CanvasPosition Position { get; set; } = new(0, 0);

    private string _accentColorHex = "#C21E7A";
    public string AccentColorHex
    {
        get => _accentColorHex;
        set { _accentColorHex = value; OnPropertyChanged(); }
    }
}
