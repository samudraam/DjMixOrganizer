using DjMixOrganizer.Core.Models;
using DjMixOrganizer.Core.Repositories;

namespace DjMixOrganizer.Data.Repositories;

/// <summary>In-memory track library used by tests and offline development.</summary>
public class InMemoryTrackRepository : ITrackRepository
{
    private readonly List<Track> _tracks =
    [
        new Track
        {
            Title = "Bandit (with Youngboy Never Broke Again)",
            Artist = "Juice WRLD",
            Bpm = 115,
            MusicalKey = new MusicalKey("C"),
            Duration = TimeSpan.FromMinutes(3.5),
            FilePath = "/seed/bandit.mp3",
        },
        new Track
        {
            Title = "In My Mind",
            Bpm = 115,
            MusicalKey = new MusicalKey("C"),
            Duration = TimeSpan.FromMinutes(3.25),
            FilePath = "/seed/in-my-mind.mp3",
        },
        new Track
        {
            Title = "Sunset Drift",
            Artist = "Nocturne",
            Bpm = 124,
            MusicalKey = new MusicalKey("Am"),
            Duration = TimeSpan.FromMinutes(4.1),
            FilePath = "/seed/sunset-drift.mp3",
        },
    ];

    /// <inheritdoc />
    public Task<IReadOnlyList<Track>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<Track>>(_tracks.ToList());
    }

    /// <inheritdoc />
    public Task<Track> AddAsync(TrackUpload upload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);
        cancellationToken.ThrowIfCancellationRequested();

        var track = upload.ToTrack();
        _tracks.Add(track);
        return Task.FromResult(track);
    }

    /// <inheritdoc />
    public Task<Track> UpdateAsync(
        Guid trackId,
        TrackUpload upload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);
        cancellationToken.ThrowIfCancellationRequested();

        var index = _tracks.FindIndex(t => t.Id == trackId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Track {trackId} not found.");
        }

        var replacement = upload.ToTrack();
        var existing = _tracks[index];
        existing.Title = replacement.Title;
        existing.Artist = replacement.Artist;
        existing.Bpm = replacement.Bpm;
        existing.MusicalKey = replacement.MusicalKey;
        existing.Duration = replacement.Duration;
        existing.FilePath = replacement.FilePath;
        existing.Format = replacement.Format;

        return Task.FromResult(existing);
    }
}
