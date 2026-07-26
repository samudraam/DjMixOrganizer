using DjMixOrganizer.Core.Models;

namespace DjMixOrganizer.Core.Repositories;

public interface ITrackRepository
{
    /// <summary>Returns every track currently in the library.</summary>
    Task<IReadOnlyList<Track>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and inserts a new track. Raw <see cref="TrackUpload.Key"/> is normalized to a letter key.
    /// </summary>
    /// <param name="upload">Song metadata to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted track with a generated <see cref="Track.Id"/>.</returns>
    Task<Track> AddAsync(TrackUpload upload, CancellationToken cancellationToken = default);

    /// <summary>Validates and updates an existing track row.</summary>
    /// <param name="trackId">Identity of the track to update.</param>
    /// <param name="upload">Replacement metadata, normalized using the same rules as add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated track.</returns>
    Task<Track> UpdateAsync(
        Guid trackId,
        TrackUpload upload,
        CancellationToken cancellationToken = default);
}
