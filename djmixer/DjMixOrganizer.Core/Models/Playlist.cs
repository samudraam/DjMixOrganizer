// File location: DjMixOrganizer.Core/Models/Playlist.cs
//
// TEACHING NOTES
// ---------------
// A Playlist groups Mixes (not Tracks) — e.g. "Warehouse Sets 2026" might
// contain several full mixes. Notice it holds a list of Guid, not a list
// of Mix objects directly.
//
// WHY IDs INSTEAD OF OBJECT REFERENCES?
// If Playlist held `List<Mix>` directly, loading one Playlist from the
// database would force you to also load every Mix (and every Track inside
// every Mix) at the same time — even if all you wanted was the playlist's
// name for a list screen. Holding IDs instead lets the Data layer decide
// *when* to actually fetch the full Mix objects (this is the core idea
// behind "lazy loading" and why ORMs like EF Core give you the choice).
// It also avoids circular object graphs, which get messy fast once you
// serialize things to JSON or a database.

using System;
using System.Collections.Generic;

namespace DjMixOrganizer.Core.Models;

public class Playlist
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; set; }

    public List<Guid> MixIds { get; init; } = [];

    public List<Tag> Tags { get; init; } = [];
}

// Tag is a small, immutable value — a great fit for a record with
// positional syntax. "Deep House" the tag is interchangeable with any
// other "Deep House" tag; there's no meaningful identity to preserve.
public record Tag(string Name);
