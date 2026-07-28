// File location: DjMixOrganizer.Core/Models/Track.cs
//
// NOTES
// ---------------
// A "Track" is a single song inside a mix — it's the atomic unit everything
// else (Mix, Playlist) is built from. Get this one right and the rest of
// the domain follows naturally.
//
// WHY A CLASS, NOT A RECORD?
// C# gives you two ways to define a data-holding type:
//   - `record` -> value-based equality, built for IMMUTABLE data
//   - `class`  -> reference-based equality, built for MUTABLE, identity-based data
//
// Tracks have an identity (the same physical mp3 file) but their metadata
// changes over time — you'll re-tag a track, correct a BPM guess, add a
// note. That's a *mutable entity with identity*, which is the textbook case
// for a class, not a record. (Contrast this with something like a
// `Duration` value — two 3:45 durations ARE equal regardless of which
// track they came from — that's a great record candidate. We'll use one
// below.)
//
// This distinction — "does this thing have an identity that persists across
// changes, or is it just a value?" — is a core domain-modeling skill and
// comes up constantly in interviews, not just for DJ apps but for anything
// with a database behind it.

using System;
using DjMixOrganizer.Core.Enums;

namespace DjMixOrganizer.Core.Models;

public class Track
{
    // `Guid` gives us a stable identity independent of the file path.
    // Why not just use the file path as the ID? Because files get moved,
    // renamed, or re-exported — the *track* should keep its identity even
    // if its location on disk changes. This is the same reasoning behind
    // using surrogate keys instead of natural keys in database design.
    public Guid Id { get; init; } = Guid.NewGuid();

    // `required` (C# 11+) forces callers to set this at construction time —
    // the compiler won't let you build a Track without a Title. This is a
    // lightweight way to express "this field must never be null/unset"
    // without writing a constructor by hand.
    public required string Title { get; set; }

    public string? Artist { get; set; }

    // BPM (beats per minute) — nullable because you might import a track
    // before it's been analyzed/tagged.
    public double? Bpm { get; set; }

    // Classical letter key (C, Am, F#m) — not Camelot. Parsed/normalized
    // at the upload boundary via MusicalKey.TryParse so the DB never stores
    // junk like "8A" or "a c b".
    public MusicalKey? MusicalKey { get; set; }

    public TimeSpan Duration { get; set; }

    // Where the actual audio file lives on disk. This is intentionally a
    // plain string path here in Core — Core has ZERO knowledge of the file
    // system APIs (File.Exists, streams, etc.). Actually *reading* the file
    // is Data's job. This is the dependency-inversion principle in action:
    // Core defines "a track has a file path," Data decides what to do with
    // that path.
    public required string FilePath { get; set; }

    public AudioFormat Format { get; set; } = AudioFormat.Mp3;

    // Timestamp for when this track was imported — useful later for
    // sorting/filtering and for cache-invalidation logic when we get to
    // background scanning (Phase 4).
    public DateTimeOffset ImportedAt { get; init; } = DateTimeOffset.UtcNow;

    // Simple derived/computed property — no backing field needed.
    // Demonstrates that not everything on a model needs to be stored;
    // some things are just cheap to compute from what's already there.
    public string DisplayName => Artist is null ? Title : $"{Artist} — {Title}";
}
