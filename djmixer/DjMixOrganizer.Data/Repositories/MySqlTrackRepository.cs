using DjMixOrganizer.Core.Models;
using DjMixOrganizer.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DjMixOrganizer.Data.Repositories;

/// <summary>MySQL-backed track library with letter-key normalization on write.</summary>
public class MySqlTrackRepository(IDbContextFactory<DjMixDbContext> contextFactory) : ITrackRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Track>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Tracks.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Track> AddAsync(TrackUpload upload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);
        var track = upload.ToTrack();

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Tracks.Add(track);
        await context.SaveChangesAsync(cancellationToken);

        return track;
    }

    /// <inheritdoc />
    public async Task<Track> UpdateAsync(
        Guid trackId,
        TrackUpload upload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);
        var replacement = upload.ToTrack();

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var existing = await context.Tracks.FindAsync([trackId], cancellationToken)
            ?? throw new InvalidOperationException($"Track {trackId} not found.");

        existing.Title = replacement.Title;
        existing.Artist = replacement.Artist;
        existing.Bpm = replacement.Bpm;
        existing.MusicalKey = replacement.MusicalKey;
        existing.Duration = replacement.Duration;
        existing.FilePath = replacement.FilePath;
        existing.Format = replacement.Format;

        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }
}
