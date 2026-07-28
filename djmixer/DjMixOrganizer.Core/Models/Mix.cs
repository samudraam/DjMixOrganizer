// File location: DjMixOrganizer.Core/Models/Mix.cs
//
// NOTES
// ---------------
// A "Mix" is an ordered sequence of Tracks — this is your aggregate root.
// In domain-driven design terms, an "aggregate" is a cluster of objects
// (here: Mix + its list of Tracks) that should be treated as a single unit
// for consistency purposes. The Mix is responsible for guarding its own
// invariants (e.g. "tracks must have a valid order") rather than letting
// outside code mutate the track list in ways that could break it.
//
// This is why `Tracks` below is exposed as a read-only view, with mutation
// only happening through methods like AddTrack/RemoveTrack. It's a small
// discipline, but it's the same discipline that matters a lot more once
// you're touching something like a train's command queue — you don't want
// arbitrary code able to reorder or corrupt a sequence of commands from
// outside a single, auditable entry point.

using System;
using System.Collections.Generic;
using System.Linq;

namespace DjMixOrganizer.Core.Models;

public class Mix
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Title { get; set; }

    public DateOnly RecordedDate { get; set; }

    // Backing field is a private List<T> — the mutable, real collection.
    private readonly List<MixTrackEntry> _tracks;

    public Mix()
    {
        _tracks = [];
    }

    // EF Core uses this constructor (instead of the parameterless one above)
    // when materializing a Mix from a database query, since it's the only
    // way to hand back a Mix with its tracks already populated — Tracks has
    // no public setter. App code should keep using `new Mix { ... }`, which
    // always goes through the parameterless constructor.
    private Mix(List<MixTrackEntry> tracks)
    {
        _tracks = tracks;
    }

    // Public surface is the IReadOnlyList<T> interface, satisfied directly
    // by the backing List<T> — List<T> already implements IReadOnlyList<T>,
    // so no wrapping/allocation is needed. Callers can enumerate and index
    // into it, but can't call .Add()/.Remove(); they have to go through the
    // methods below. This is the "encapsulated collection" pattern: cheap to
    // write, and it closes off an entire category of bugs where some
    // unrelated code silently mutates your aggregate's internal state.
    //
    // Deliberately IReadOnlyList<T>, not the concrete ReadOnlyCollection<T>
    // class: EF Core's change-tracking snapshot machinery needs to treat a
    // value of the *field's* type (List<MixTrackEntry>) as a value of the
    // *property's* declared type, and List<T> converts to the IReadOnlyList<T>
    // interface for free, but not to the unrelated ReadOnlyCollection<T> class.
    public IReadOnlyList<MixTrackEntry> Tracks => _tracks;

    public TimeSpan TotalDuration => TimeSpan.FromTicks(_tracks.Sum(t => t.Track.Duration.Ticks));

    public void AddTrack(Track track, TimeSpan startTime)
    {
        // A real invariant check: a mix can't have two tracks starting at
        // the exact same timestamp. This is the kind of guard that belongs
        // INSIDE the aggregate, not scattered across every ViewModel that
        // happens to call AddTrack.
        if (_tracks.Any(t => t.StartTime == startTime))
        {
            throw new InvalidOperationException(
                $"A track already starts at {startTime} in this mix.");
        }

        _tracks.Add(new MixTrackEntry(track, startTime));
        _tracks.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
    }

    public void RemoveTrack(Guid trackId)
    {
        _tracks.RemoveAll(t => t.Track.Id == trackId);
    }
}

// A `record` is the right call here: a MixTrackEntry is just "this track,
// at this point in time within this mix" — a value, not something with its
// own independent identity or mutable lifecycle. Two entries with the same
// Track and StartTime ARE the same entry. Compare this reasoning to why
// Track itself is a class (see Track.cs) — same question, different answer,
// because the underlying thing being modeled is different.
public record MixTrackEntry(Track Track, TimeSpan StartTime)
{
    // EF Core materializes a MixTrackEntry through this constructor instead
    // of the primary one above — it will bind StartTime (a plain value) but
    // refuses to bind Track through a constructor, since the related Track
    // row may not be loaded yet at that point. EF sets Track via reflection
    // right afterward, once it is loaded.
    private MixTrackEntry(TimeSpan startTime) : this(null!, startTime) { }
}
