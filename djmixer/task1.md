# Task 1: From Fake Data to a Real Repository

**Closes out:** Phase 2 (MVVM + data-binding) · **Unblocks:** Phase 3 (ID3 parsing) and Phase 4 (concurrency)

## The situation

Right now, `MixListViewModel.LoadMixesAsync()` does this:

```csharp
[RelayCommand]
private async Task LoadMixesAsync()
{
    IsLoading = true;
    try
    {
        await Task.Delay(300); // simulates I/O latency for now
    }
    finally
    {
        IsLoading = false;
    }
}
```

It flips a loading spinner on and off around... nothing. No mixes ever get
added to the `Mixes` collection. Tapping "Load Mixes" in the simulator does
literally nothing visible.

Your job this task: make that button actually load something, by putting a
real (if simple) repository behind it. No SQLite yet, no file scanning yet —
just an in-memory list, so you can focus entirely on **the wiring**: how a
View, a ViewModel, a Core interface, and a Data implementation are supposed
to talk to each other. Get this pattern solid now, and Phases 3–4 become
"swap the in-memory list for a real database call" instead of "figure out
MVVM and databases at the same time."

## Warm-up: MVVM, mapped from what you already know

You've done SwiftUI, and SwiftUI's own state-management story is basically
the same shape as MVVM, just with different keywords. Here's the direct
translation:

| SwiftUI | MAUI (this app) | What it's doing |
|---|---|---|
| `class MixListModel: ObservableObject` | `partial class MixListViewModel : ObservableObject` | The "brain" behind a screen. Holds state, exposes actions. |
| `@Published var mixes: [Mix]` | `[ObservableProperty] private ObservableCollection<Mix> _mixes;` | A property that yells "I changed!" so the UI redraws. |
| A `View` reading `@ObservedObject var model` | XAML `{Binding Mixes}` | The UI reads state from the brain; it doesn't hold its own copy. |
| `Button("Load") { model.load() }` | `Button Command="{Binding LoadMixesCommand}"` | User action → a method on the brain, not UI code. |
| `protocol MixRepository` | `interface IMixRepository` | A contract: "something that can fetch mixes," with no promise of *how*. |
| `struct InMemoryMixRepository: MixRepository` | `class InMemoryMixRepository : IMixRepository` | A concrete thing that fulfills the contract. |
| Passing a repository into a model's initializer | Constructor injection (a DI container hands it to you) | The View/ViewModel never does `MixRepository()` itself — something else decides which concrete type to hand over. |

The one genuinely new idea if you're coming from SwiftUI: MAUI's binding
system doesn't re-render a whole tree on every state change the way
SwiftUI's diffing does. It's closer to old-school UIKit + Combine —
individual properties fire `PropertyChanged`, and only the specific UI
elements bound to that property update. You don't have to manage this
yourself; `[ObservableProperty]` generates it for you. Just know that's
what's happening under the hood when you see `_mixes` (lowercase, private)
turn into `Mixes` (uppercase, public) that XAML can bind to — **you never
call it `_mixes` from outside the class; the generator makes the real
public property for you.**

## C# syntax cheat-sheet for this task

You'll touch all of these. Quick reference so nothing here is a surprise:

| Swift | C# | Notes |
|---|---|---|
| `protocol Foo { func bar() }` | `interface IFoo { void Bar(); }` | C# interfaces conventionally start with `I`. No default implementations here (that's a newer, less-used C# feature — don't reach for it). |
| `func bar() async throws -> [Mix]` | `Task<List<Mix>> BarAsync();` | No `throws` in the signature — C# doesn't check exceptions at compile time. `Async` suffix is a *naming convention*, not a keyword requirement, but everyone follows it. |
| `let id = UUID()` | `Guid Id = Guid.NewGuid();` | Same idea, different name. |
| `var name: String?` | `string? Name` | Same `?` meaning: "this can be null." C# calls this a *nullable reference type*. |
| `guard let x = y else { return }` | `if (y is null) return;` then use `y` | No direct `guard` equivalent; C#'s null-flow analysis is smart enough to know `y` is non-null after that early return. |
| `x?.foo()` | `x?.Foo()` | Identical — optional chaining works the same way. |
| `x ?? defaultValue` | `x ?? defaultValue` | Identical — nil-coalescing, same operator. |
| `init(name: String)` requiring a value | `public required string Name { get; set; }` | `required` (C# 11+) forces callers to set it, without writing a constructor by hand. |
| `class Foo` split across files via `// MARK:` | `partial class Foo` split across *actual separate files* | `partial` means "this class's definition continues elsewhere." `MixListViewModel` is one half you write; the `CommunityToolkit.Mvvm` source generator writes the other half (the real `Mixes` property, `LoadMixesCommand`, etc.) into a hidden generated file at compile time. This is why you'll define a *field* (`_mixes`) but bind to a *property* (`Mixes`) that doesn't visibly exist anywhere in your file. |
| Property wrapper, e.g. `@Published` | Attribute, e.g. `[ObservableProperty]` | Conceptually similar — both are annotations that make the compiler/tooling generate extra code around a property. |
| Dependency container / manual init passing | Constructor injection + `builder.Services.AddSingleton<T>()` | Whatever you register in `MauiProgram.cs` is what gets handed to a constructor that asks for that type. You never write `new SomeViewModel()` yourself. |

## Step by step

### 1. Define the contract in Core

Core has zero dependencies — it doesn't know SQLite exists, doesn't know
MAUI exists. It only gets to say "here's what a repository must be able to
do," not how.

Create `DjMixOrganizer.Core/Repositories/IMixRepository.cs`:

```csharp
using DjMixOrganizer.Core.Models;

namespace DjMixOrganizer.Core.Repositories;

public interface IMixRepository
{
    Task<IReadOnlyList<Mix>> GetAllAsync();
}
```

That's it for now — one method. Resist the urge to add `AddAsync`,
`DeleteAsync`, etc. yet; add methods when a real caller needs them, not
speculatively (see the stretch goals at the bottom if you want more).

### 2. Implement it in Data

This is the layer that's allowed to know *how* — even if "how" is just an
in-memory list for now.

Create `DjMixOrganizer.Data/Repositories/InMemoryMixRepository.cs`:

```csharp
using DjMixOrganizer.Core.Models;
using DjMixOrganizer.Core.Repositories;

namespace DjMixOrganizer.Data.Repositories;

public class InMemoryMixRepository : IMixRepository
{
    private readonly List<Mix> _seedData;

    public InMemoryMixRepository()
    {
        // TODO: build 2-3 fake Mix objects here to seed the list.
        // Reminder: Mix.Title is `required`, so you must set it in an
        // object initializer:
        //
        //   var mix = new Mix { Title = "Warehouse Set" };
        //
        // Try giving each one a different RecordedDate too — you'll want
        // that to confirm the UI is showing real, distinct data and not
        // one row repeated three times.
        _seedData = [ /* your mixes here */ ];
    }

    public Task<IReadOnlyList<Mix>> GetAllAsync()
    {
        // TODO: return _seedData, wrapped as IReadOnlyList<Mix>,
        // wrapped in a completed Task.
        //
        // Hint: Task.FromResult(...) is how you hand back an already-known
        // value from a method that's *shaped* like async work. There's no
        // real I/O here yet, so there's nothing to `await`.
        throw new NotImplementedException();
    }
}
```

Why does an in-memory list still return a `Task`? Because `IMixRepository`
promised `Task<IReadOnlyList<Mix>>`, and every implementation has to honor
that shape — including this fake one. When you swap this for SQLite in a
later phase, the ViewModel won't need to change at all. That's the entire
payoff of coding against the interface instead of the concrete class.

### 3. Register both with the DI container

Open `DjMixOrganizer.App/MauiProgram.cs`. You'll see this already:

```csharp
builder.Services.AddTransient<MixListViewModel>();
builder.Services.AddTransient<MixListPage>();
```

Add a line *above* those two, registering the interface against the
concrete type:

```csharp
builder.Services.AddSingleton<IMixRepository, InMemoryMixRepository>();
```

`AddSingleton` (one shared instance for the app's lifetime) makes sense
here — it's just a list in memory, no reason to recreate it. You'll need
`using DjMixOrganizer.Core.Repositories;` and
`using DjMixOrganizer.Data.Repositories;` at the top of the file.

> **Heads up on the architecture diagram in the README:** it draws
> `App → Core` only, no arrow to `Data`. That's describing "app *logic*
> shouldn't call Data's concrete classes" — but `MauiProgram.cs` is the one
> exception, on purpose. Something has to tell the DI container which real
> class to use for `IMixRepository`, and that wiring has to happen
> somewhere that can see both sides. This spot — called the **composition
> root** — is the one place in Clean Architecture where the outer layers
> are allowed to touch. Every other file in `App` should only ever reference
> `IMixRepository`, never `InMemoryMixRepository` directly.

### 4. Wire the ViewModel to ask for it

This is the constructor-injection step. Open
`DjMixOrganizer.App/ViewModels/MixListViewModel.cs` and change it from a
parameterless class to one that takes the repository as a constructor
argument:

```csharp
public partial class MixListViewModel : ObservableObject
{
    private readonly IMixRepository _mixRepository;

    [ObservableProperty]
    private ObservableCollection<Mix> _mixes = [];

    [ObservableProperty]
    private bool _isLoading;

    public MixListViewModel(IMixRepository mixRepository)
    {
        _mixRepository = mixRepository;
    }

    [RelayCommand]
    private async Task LoadMixesAsync()
    {
        IsLoading = true;
        try
        {
            // TODO: await _mixRepository.GetAllAsync(), then replace the
            // contents of Mixes with what came back.
            //
            // Careful: Mixes is an ObservableCollection, and GetAllAsync
            // returns an IReadOnlyList. You can't just assign one to the
            // other — either build a new ObservableCollection<Mix> from
            // the result, or clear Mixes and .Add() each item in a loop.
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

You never call `new MixListViewModel(...)` yourself anywhere — you
registered `MixListViewModel` in `MauiProgram.cs` back in the original
delivery, and now that it asks for an `IMixRepository` in its constructor,
the DI container will look up what you registered in Step 3 and hand it
over automatically. This is the whole reason constructor injection exists:
`MixListPage` asks for a `MixListViewModel`, which asks for an
`IMixRepository`, and the container resolves the entire chain without
anyone writing `new` by hand.

### 5. Run it

```bash
cd /Users/RamyaSamudrala/Developer/music/djmixer
open -a Simulator
dotnet build DjMixOrganizer.App/DjMixOrganizer.App.csproj -t:Run -f net10.0-ios -r iossimulator-arm64
```

Tap "Load Mixes." You should see your fake mixes appear as rows, each
showing title, recorded date, and total duration (which is computed
automatically from `Mix.TotalDuration` — you get that for free from the
teaching-notes code already in `Mix.cs`, as long as your seed mixes have
tracks with durations... which, note, they won't unless you add tracks too.
It's fine if `TotalDuration` shows `00:00:00` for now — that's expected
until Phase 3 gives you real tracks).

### 6. Write one test

Open `DjMixOrganizer.Tests/UnitTest1.cs`. Rename the file (and class) to
something meaningful — `InMemoryMixRepositoryTests.cs` — and write a real
test instead of the empty stub:

```csharp
using DjMixOrganizer.Data.Repositories;

namespace DjMixOrganizer.Tests;

public class InMemoryMixRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsSeedData()
    {
        // Arrange
        var repository = new InMemoryMixRepository();

        // Act
        var mixes = await repository.GetAllAsync();

        // Assert
        Assert.NotEmpty(mixes);
    }
}
```

This is the same Arrange/Act/Assert shape as `given/when/then` if you've
used Quick/Nimble, or just plain `XCTAssert...` calls if you've used
XCTest. Run it with:

```bash
dotnet test DjMixOrganizer.Tests/DjMixOrganizer.Tests.csproj
```

You'll need to add a project reference first — `Tests` currently only
knows about `Core`:

```bash
dotnet add DjMixOrganizer.Tests reference DjMixOrganizer.Data
```

## Definition of done

- [ ] `IMixRepository` exists in `Core/Repositories/`, with one method:
      `GetAllAsync()`.
- [ ] `InMemoryMixRepository` exists in `Data/Repositories/`, implements
      the interface, and returns 2–3 hand-built `Mix` objects.
- [ ] `MauiProgram.cs` registers `IMixRepository → InMemoryMixRepository`.
- [ ] `MixListViewModel` takes `IMixRepository` in its constructor and
      actually calls it from `LoadMixesAsync`.
- [ ] Running the app and tapping "Load Mixes" shows real rows, not an
      empty list.
- [ ] At least one passing xUnit test covers `InMemoryMixRepository`.
- [ ] `README.md`'s Phase 2 line gets flipped from 🔶 to ✅ (you did the
      thing the checklist was waiting on).

## If you get stuck

- **"CS9035: Required member 'Mix.Title' must be set"** — you built a
  `Mix` without an object initializer. Use `new Mix { Title = "..." }`, not
  `new Mix()`.
- **"Cannot convert from 'IReadOnlyList<Mix>' to 'ObservableCollection<Mix>'"**
  — this is the mismatch flagged in Step 4. Build a new
  `ObservableCollection<Mix>(result)` rather than assigning directly.
- **DI throws at startup ("Unable to resolve service for type
  IMixRepository")** — you added the constructor parameter in Step 4
  before registering the interface in Step 3. Double check the
  `AddSingleton` line is actually in `MauiProgram.cs` and spelled
  identically to the interface/class names.
- **The build succeeds but the button still does nothing** — check that
  `LoadMixesAsync` actually assigns to `Mixes` (the public generated
  property) and not to `_mixes` inside a scope where that doesn't do what
  you expect. Also confirm you're calling `.Add()`/reassigning `Mixes`, not
  just declaring a local variable and throwing it away.

## Stretch goals (optional, don't block Task 2 on these)

- Add a second interface method, `AddAsync(Mix mix)`, and a "New Mix"
  button in `MixListPage.xaml` that calls it through a new
  `[RelayCommand]`.
- Make `LoadMixesAsync` run automatically when the page first appears
  (override `OnAppearing` in `MixListPage.xaml.cs`, or look into MAUI's
  `INotifyPropertyChanged`-based page lifecycle events) instead of waiting
  for a tap — this is the more realistic UX and sets up the async-loading
  habits Phase 4 leans on harder.
- Add tracks (with real `TimeSpan` durations) to your seed mixes so
  `TotalDuration` actually shows something other than zero.
