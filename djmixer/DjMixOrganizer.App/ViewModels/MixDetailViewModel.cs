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
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DjMixOrganizer.Core.Models;
using DjMixOrganizer.Core.Repositories;

namespace DjMixOrganizer.App.ViewModels;

public partial class MixDetailViewModel : ObservableObject, IQueryAttributable
{
    // Exposed as a static, compile-time-safe {x:Static} binding target for
    // the Key Picker in MixDetailPage.xaml — it never changes per
    // instance, so it doesn't need to be an [ObservableProperty].
    public static IReadOnlyList<string> MusicalKeys { get; } = MusicalKey.All;

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
    private string _mixTitle = string.Empty;

    /// <summary>User-visible save / validation feedback (success or error).</summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>True while a save is in flight — disables the Save button.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveMixCommand))]
    private bool _isSaving;

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
            Key = track.MusicalKey?.ToString() ?? "C",
            HasMusic = true,
            Position = new CanvasPosition(20 + (index % 2) * 260, 20 + (index / 2) * 280),
            AccentColorHex = NodeColorPalette[index % NodeColorPalette.Length],
        });
    }

    [RelayCommand]
    private void RemoveNode(TrackNode node)
    {
        Nodes.Remove(node);
    }

    // Sequential StartTime for now: track N+1 starts the instant track N
    // ends, same scheme InMemoryMixRepository's seed data already uses.
    // Letting tracks play simultaneously (a stem-layering/mashup mix) is a
    // separate, larger follow-up — it needs a UI for picking which section
    // of a track's audio plays, and a change to Mix.AddTrack's current
    // "no duplicate StartTime" invariant.
    /// <summary>Validates canvas nodes and persists the mix; errors stay on-screen (no rethrow).</summary>
    [RelayCommand(CanExecute = nameof(CanSaveMix))]
    private async Task SaveMixAsync()
    {
        StatusMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(MixTitle))
        {
            StatusMessage = "Mix title is required.";
            System.Diagnostics.Debug.WriteLine(
                "[MixDetail] Save skipped — MixTitle is empty.");
            return;
        }

        // MixTrackEntries use composite PK (MixId, TrackId) — one slot per track.
        var duplicateIds = Nodes
            .GroupBy(n => n.Track.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.First().Track.DisplayName)
            .ToList();
        if (duplicateIds.Count > 0)
        {
            StatusMessage =
                $"The same track can't appear twice yet: {string.Join(", ", duplicateIds)}.";
            return;
        }

        var mix = new Mix
        {
            Id = Mix?.Id ?? Guid.NewGuid(),
            Title = MixTitle.Trim(),
            RecordedDate = Mix?.RecordedDate ?? DateOnly.FromDateTime(DateTime.Today),
        };

        try
        {
            //TODO FIX THIS UPLOAD ISSUE
            // Library uploads often still have Duration = Zero (no file-length
            // probe yet). Mix.AddTrack forbids duplicate StartTimes, so a zero
            // step would crash the save. Placeholder 1s keeps order for now —
            // follow-up: https://github.com/samudraam/DjMixOrganizer/issues/17
            var cumulativeStart = TimeSpan.Zero;
            foreach (var node in Nodes)
            {
                mix.AddTrack(node.Track, cumulativeStart);
                var step = node.Track.Duration > TimeSpan.Zero
                    ? node.Track.Duration
                    : TimeSpan.FromSeconds(1);
                cumulativeStart += step;
            }

            IsSaving = true;
            StatusMessage = "Saving...";
            System.Diagnostics.Debug.WriteLine(
                $"[MixDetail] Saving mix Id={mix.Id}, Title='{mix.Title}', Tracks={mix.Tracks.Count}...");

            // Run EF/MySQL on a thread-pool thread so a stuck connection can't
            // freeze the iOS UI (gesture-gate timeouts / "app broken" feel).
            // WaitAsync caps how long we block the user on a dead Docker host.
            // Important: async lambda — `Task.Run(() => SaveAsync())` returns
            // Task<Task> and would finish before the DB write actually completes.
            await Task.Run(async () => await _mixRepository.SaveAsync(mix).ConfigureAwait(false))
                .WaitAsync(TimeSpan.FromSeconds(15));

            Mix = mix;
            IsNewMix = false;
            StatusMessage = $"Saved \"{mix.Title}\".";
            System.Diagnostics.Debug.WriteLine(
                $"[MixDetail] Save succeeded for '{mix.Title}' ({mix.Id}).");
        }
        catch (TimeoutException)
        {
            StatusMessage =
                "Save timed out — is Docker MySQL running? (docker compose up -d)";
            System.Diagnostics.Debug.WriteLine(
                $"[MixDetail] Save TIMED OUT for '{mix.Title}'.");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {FormatException(ex)}";
            System.Diagnostics.Debug.WriteLine(
                $"[MixDetail] Save FAILED for '{mix.Title}': {ex}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool CanSaveMix() => !IsSaving;

    /// <summary>Walks InnerException so EF/MySQL failures show the root cause.</summary>
    private static string FormatException(Exception ex)
    {
        var parts = new StringBuilder();
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (parts.Length > 0)
            {
                parts.Append(" → ");
            }

            parts.Append(current.Message);
        }

        return parts.ToString();
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

        MixTitle = Mix.Title;

        var seeded = new List<TrackNode>();
        for (var i = 0; i < Mix.Tracks.Count; i++)
        {
            var track = Mix.Tracks[i].Track;
            seeded.Add(new TrackNode
            {
                Track = track,
                Bpm = track.Bpm ?? 120,
                Key = track.MusicalKey?.ToString() ?? "C",
                HasMusic = true,
                Position = new CanvasPosition(20 + (i % 2) * 260, 20 + (i / 2) * 280),
                AccentColorHex = NodeColorPalette[i % NodeColorPalette.Length],
            });
        }

        Nodes = new ObservableCollection<TrackNode>(seeded);
    }
}
