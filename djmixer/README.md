# DJ Mix Organizer — Learning Roadmap

This project exists to do two things at once: give you a real tool you'll
actually use, and give you deliberate reps in the skills on your interview's
"preferred qualifications" list. Each phase below is a genuine milestone, not
a toy exercise.

## Why the architecture looks the way it does

We're using a **multi-project solution** instead of one flat MAUI app:

```
DjMixOrganizer.sln
├── DjMixOrganizer.Core/       <- domain models, interfaces, business logic
│                                 (no UI, no database — pure C#)
├── DjMixOrganizer.Data/       <- SQLite persistence, file I/O, ID3 parsing
│                                 (implements Core's interfaces)
├── DjMixOrganizer.App/        <- MAUI UI, ViewModels, XAML views
│                                 (depends on Core + Data)
└── DjMixOrganizer.Tests/      <- unit tests against Core + Data
```

This is **Clean Architecture / Onion Architecture** in miniature: dependencies
point *inward*. The UI depends on Core; Core depends on nothing. This matters
for the interview for two reasons:

1. It's exactly the kind of separation you'll be expected to *read and
   navigate* in an existing embedded/vehicle-control codebase — business
   logic that has to run correctly regardless of which UI or hardware layer
   sits on top of it.
2. It lets us swap the MAUI app for, say, a console tool or a background
   service later without touching business logic — the same reason real
   systems separate a "controller" layer from the physical I/O layer.

## MAUI ↔ Flutter, conceptually

Since Flutter/React is the named comparison point in the posting, here's the
direct mapping so you can speak to both fluently:

| Concept | Flutter | .NET MAUI |
|---|---|---|
| UI description | Widget tree (Dart) | XAML markup (declarative, like Widget tree) |
| Reactive state | `StatefulWidget` + `setState`, or Provider/Bloc | MVVM: `ObservableObject` + data-binding |
| Cross-platform target | Compiles to native ARM via Dart AOT | Compiles to native via .NET NativeAOT/Mono |
| Platform-specific code | `Platform.isAndroid` checks, platform channels | `#if ANDROID` partial classes, `DependencyService` |
| Hot reload | Yes | Yes |
| State management library | Provider, Riverpod, Bloc | CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`) |

The mental model is identical: **declarative UI + a reactive state layer +
platform-specific escape hatches when you need real hardware access**
(camera, BLE, kiosk mode). That last part is exactly what "Android
provisioning / kiosk / hardware integration" is asking about — MAUI gives you
that same escape hatch via `DependencyService` or partial classes per
platform.

## Phase plan

1. ✅ **Solution + domain models** — Track, Mix, Playlist, Tag exist in
   `DjMixOrganizer.Core` with real invariants (no duplicate start times,
   encapsulated collections). Get comfortable with C# records, nullable
   reference types, and why we keep this project dependency-free.
2. 🔶 **MVVM + data-binding** — `MixListViewModel`/`MixListPage` are wired
   through DI (`MauiProgram.cs`) and the binding round-trip works. But
   `LoadMixesCommand` is still a stub (`await Task.Delay(300)`) — there's no
   `IMixRepository` interface in Core yet for it to call.
3. ⬜ **ID3 tag parsing** — reading raw bytes from an mp3 file header by hand
   before reaching for a library, so you understand the binary format.
   `DjMixOrganizer.Data` currently has no code at all — just a `.csproj`
   referencing Core.
4. ⬜ **Concurrency** — `async`/`await`, `IProgress<T>`, `CancellationToken`
   for scanning a folder of mixes without freezing the UI. Blocked on Phase
   3 (needs something to scan) and an `IMixRepository` interface in Core.
5. ⬜ **FFmpeg interop** — `Process.Start` to shell out for mp3 clip export.
   Same shape as talking to an external tool/daemon over a process boundary.
6. ⬜ **Docker** — containerize a small companion service (e.g. a headless
   "library scanner" API) so you get real `Dockerfile` reps even though the
   MAUI client itself isn't containerized.
7. ⬜ **GitHub Actions** — CI pipeline: restore, build, test on every push.
   No workflow files exist in the repo yet.
8. 🔶 **Android target + kiosk discussion** — iOS builds and runs today (see
   "How to Run" above). Android is untested, and the kiosk/`LockTask`
   discussion hasn't started.

Two loose ends worth cleaning up independent of what's next: `MainPage.xaml`
is the unmodified `dotnet new maui` template screen — `AppShell` already
routes straight to `MixListPage` and never references `MainPage`, so it's
dead code. And `DjMixOrganizer.Tests` has exactly one empty test method.

## Getting this running on your machine

This sandbox can't run MAUI builds (no Android/iOS SDKs, no NuGet access),
so run these locally:

```bash
# Install the MAUI workload (one-time)
dotnet workload install maui

# From an empty folder:
dotnet new sln -n DjMixOrganizer

dotnet new classlib -n DjMixOrganizer.Core -o DjMixOrganizer.Core
dotnet new classlib -n DjMixOrganizer.Data -o DjMixOrganizer.Data
dotnet new maui -n DjMixOrganizer.App -o DjMixOrganizer.App
dotnet new xunit -n DjMixOrganizer.Tests -o DjMixOrganizer.Tests

dotnet sln add DjMixOrganizer.Core DjMixOrganizer.Data DjMixOrganizer.App DjMixOrganizer.Tests

# Wire up project references (Core has none; Data and App depend inward)
dotnet add DjMixOrganizer.Data reference DjMixOrganizer.Core
dotnet add DjMixOrganizer.App reference DjMixOrganizer.Core DjMixOrganizer.Data
dotnet add DjMixOrganizer.Tests reference DjMixOrganizer.Core DjMixOrganizer.Data

# Add the MVVM toolkit to the App project
dotnet add DjMixOrganizer.App package CommunityToolkit.Mvvm
```

Then copy the files from this delivery into the matching folders (paths
noted at the top of each file) and `dotnet build`.

## How to Run

```bash
cd /Users/RamyaSamudrala/Developer/music/djmixer
open -a Simulator
dotnet build DjMixOrganizer.App/DjMixOrganizer.App.csproj -t:Run -f net10.0-ios -r iossimulator-arm64
```

If `actool` fails with "No simulator runtime version ... available to use
with iphonesimulator SDK version", Xcode's bundled SDK is newer than any
installed simulator runtime — fix once with:
`xcodebuild -downloadPlatform iOS`.

## System diagrams (current state)

### Project dependencies

```mermaid
graph TD;
    Core["DjMixOrganizer.Core<br/>models + Repository Interfaces<br/>(Zero Dependencies)"]
    Data["DjMixOrganizer.Data<br/>SQLite + ID3 implementations"]
    App["DjMixOrganizer.App<br/>MAUI UI + ViewModels"]
    Tests["DjMixOrganizer.Tests<br/>Unit & Integration Tests"]

    Data --> Core
    App --> Core
    Tests --> Core
    Tests --> Data
    
    classDef highlight fill:#f9f,stroke:#333,stroke-width:2px;
    class Core highlight;
```

### Domain model

```mermaid
classDiagram
    class Track {
        +Guid Id
        +string Title
        +string? Artist
        +double? Bpm
        +string? CamelotKey
        +TimeSpan Duration
        +string FilePath
        +AudioFormat Format
        +DateTimeOffset ImportedAt
    }
    class MixTrackEntry {
        <<record>>
        +Track Track
        +TimeSpan StartTime
    }
    class Mix {
        +Guid Id
        +string Title
        +DateOnly RecordedDate
        +AddTrack(track, startTime)
        +RemoveTrack(trackId)
    }
    class Playlist {
        +Guid Id
        +string Name
        +List~Guid~ MixIds
        +List~Tag~ Tags
    }
    class Tag {
        <<record>>
        +string Name
    }
    class AudioFormat {
        <<enumeration>>
        Mp3
        Wav
        Flac
        Aiff
    }
    Mix "1" *-- "many" MixTrackEntry : Tracks
    MixTrackEntry --> Track
    Track --> AudioFormat
    Playlist ..> Mix : MixIds — by Guid, not object ref
    Playlist "1" *-- "many" Tag
```

`Playlist` holds `List<Guid>`, not `List<Mix>` — loading a playlist's name
for a list screen shouldn't force-load every mix and every track inside it.
`Track` is a class (mutable identity — you re-tag it over time);
`MixTrackEntry` and `Tag` are records (values — two identical ones are
interchangeable).

### Current MVVM flow (mix list screen)

```mermaid
sequenceDiagram
    actor You
    participant Page as MixListPage (View)
    participant VM as MixListViewModel
    participant Repo as IMixRepository

    You->>Page: tap "Load Mixes"
    Page->>VM: LoadMixesCommand
    VM->>VM: IsLoading = true
    rect rgba(200,120,50,0.12)
    Note over VM,Repo: today: await Task.Delay(300) — Repo doesn't exist yet
    end
    VM->>VM: IsLoading = false
    VM-->>Page: PropertyChanged
    Page-->>You: spinner hides, list stays empty
```

A rendered, styled version of these diagrams plus the notes below is also
available as a standalone page:
https://claude.ai/code/artifact/a2365250-ef4b-40e3-9d76-2b1f67d6908b

## Design notes: node-based mix editor (brainstorm, not a spec)

This is the biggest pivot from the phase plan above, so treat it as a
direction to react to, not a committed design.

**Two canvases, two node types.** A *Master View* where each node is a mix
idea (recorded or not), and a *Mix View* — opened from a Master View node —
where each node is a track placed inside that one mix, connected by edges
that represent the transition between them.

```mermaid
graph LR
    subgraph MasterView["Master View — every mix idea you have"]
        direction TB
        M1(("Warehouse Set"))
        M2(("Sunset Set"))
        M3(("Closer"))
    end
    subgraph MixView["Mix View — inside 'Warehouse Set'"]
        direction LR
        T1["Track A<br/>128 BPM · 8A"]
        T2["Track B<br/>126 BPM · 9A"]
        T3["Track C<br/>127 BPM · 9A"]
        T1 -->|"32-beat blend, +0st"| T2
        T2 -->|"cut, -2st"| T3
    end
    M1 -.->|open| MixView
```

**The modeling question this raises:** BPM, key, and cue points are
properties of the *track itself* — true no matter which mix it appears in.
Pitch shift, tempo adjustment, and "where in this mix does it start" are
properties of *this track's use in this specific mix* — the same song can be
pitched +0 in one set and -3 semitones in another. `MixTrackEntry` already
gets this right for start-time; a node editor just extends that same
pattern instead of bolting pitch/tempo onto `Track` directly.

```mermaid
classDiagram
    class Track {
        +Guid Id
        +double? Bpm
        +string? CamelotKey
        +List~CuePoint~ CuePoints
    }
    class CuePoint {
        <<record>>
        +string Label
        +TimeSpan Position
    }
    class TrackSource {
        <<abstract>>
    }
    class LocalFileTrackSource {
        +string FilePath
    }
    class SpotifyTrackReference {
        +string SpotifyUri
        +string? PreviewUrl
    }
    class TrackNode {
        +Guid Id
        +Track Track
        +TimeSpan CueInAt
        +TimeSpan CueOutAt
        +double PitchShiftSemitones
        +double TempoAdjustPercent
        +CanvasPosition Position
    }
    class MixTransition {
        +string Style
        +TimeSpan Duration
    }
    class MixCanvas {
        +Guid MixId
        +List~TrackNode~ Nodes
        +List~MixTransition~ Transitions
    }
    class CanvasPosition {
        <<record>>
        +double X
        +double Y
    }

    TrackSource <|-- LocalFileTrackSource
    TrackSource <|-- SpotifyTrackReference
    Track --> TrackSource
    Track "1" --> "*" CuePoint
    TrackNode --> Track
    TrackNode --> CanvasPosition
    MixCanvas "1" --> "*" TrackNode
    MixCanvas "1" --> "*" MixTransition
    MixTransition --> TrackNode
```

`MixCanvas` is deliberately separate from `Mix` — canvas layout (node
positions) is presentation state, not domain logic, same reasoning as why
`Playlist` holds Guids instead of objects.

Worth naming honestly: MAUI has no built-in node-graph control. This means
hand-rolling a canvas with `GraphicsView` (draw nodes/edges yourself, frame
by frame) plus `PanGestureRecognizer`/`DragGestureRecognizer` for dragging
nodes around — a real, multi-week UI engineering effort on top of the
existing phase plan, not a control you drop in.

### Spotify integration: the actual constraint

**Spotify's Web API no longer gives new apps audio analysis.** Since
November 2024, the Audio Features and Audio Analysis endpoints (tempo, key,
energy) are restricted to apps that already had Extended Quota approval
before the cutoff — a new app registered today cannot pull BPM or key from
Spotify's catalog data at all.

Separately, and unrelated to that cutoff: Spotify's terms have never
allowed downloading or capturing raw audio from a stream. Playback goes
through their SDK as a black box; there's no PCM data to run your own
BPM/key/pitch detection against, even if you wanted to compute it yourself.

Net effect: if Spotify gets integrated, treat it as a way to **browse your
library / pull track and artist metadata for building a mix's tracklist
idea** — not as a source of BPM, key, or cue data. That analysis only works
for audio you actually hold the bytes for, which in this app means Phase 3
(ID3/local files) — already on the roadmap.

### Pairing metadata with pro DJ software

"DJ Pro" software generally (Serato, rekordbox, Engine DJ, djay Pro)
converges on the same handful of fields, but stores them differently:

| Field | Where it usually lives | Notes |
|---|---|---|
| BPM | ID3v2 `TBPM` frame | Universally read by every app — the safest field to write. |
| Key | ID3v2 `TKEY` frame | Camelot ("8A") vs. Open Key ("6m") is a per-app display setting, not a different tag — pick one and be consistent. |
| Cue points / hot cues | App-proprietary binary tags | Serato writes custom `GEOB` frames ("Serato Markers2") — reverse-engineered, undocumented, and fragile to write to directly. |
| Beatgrid | App-proprietary | Engine DJ keeps its own SQLite database for USB export; not a tag on the file at all. |
| Playlists/crates | rekordbox XML | The one format with the broadest cross-app import support if the goal is moving organized sets between tools. |

Recommendation, given the interop is genuinely fragile: write standard
`TBPM`/`TKEY` ID3v2 tags for baseline compatibility everywhere, and if you
want organized sets to show up as playlists in whatever software you
actually mix in, target a rekordbox-XML export rather than reverse-engineering
one vendor's binary cue-point format.

And the modeling point above still applies: BPM/key/cue points belong on
`Track` (intrinsic to the audio); pitch shift and tempo adjustment belong on
the per-mix node (`TrackNode`) — because that's genuinely how DJ software
itself treats them.