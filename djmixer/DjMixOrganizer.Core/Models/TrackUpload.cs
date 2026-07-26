using DjMixOrganizer.Core.Enums;

namespace DjMixOrganizer.Core.Models;

/// <summary>
/// Input payload for creating a track row — raw key text is normalized before the entity is built.
/// </summary>
/// <param name="Title">Required track title.</param>
/// <param name="Artist">Optional artist name.</param>
/// <param name="Bpm">Optional tempo in beats per minute.</param>
/// <param name="Key">Raw key text (e.g. <c>am</c>); normalized to a letter key before save.</param>
/// <param name="Duration">Track length.</param>
/// <param name="FilePath">Path to the audio file on disk.</param>
/// <param name="Format">Audio container format; defaults to MP3.</param>
public sealed record TrackUpload(
    string Title,
    string? Artist,
    double? Bpm,
    string? Key,
    TimeSpan Duration,
    string FilePath,
    AudioFormat Format = AudioFormat.Mp3)
{
    /// <summary>
    /// Validates required fields, normalizes the key, and builds a new <see cref="Track"/>.
    /// </summary>
    /// <returns>A track ready to insert into a repository.</returns>
    /// <exception cref="ArgumentException">Thrown when title or file path is missing.</exception>
    /// <exception cref="InvalidMusicalKeyException">Thrown when <see cref="Key"/> is non-empty but invalid.</exception>
    public Track ToTrack()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new ArgumentException("Track title is required.", nameof(Title));
        }

        if (string.IsNullOrWhiteSpace(FilePath))
        {
            throw new ArgumentException("Track file path is required.", nameof(FilePath));
        }

        return new Track
        {
            Title = Title.Trim(),
            Artist = string.IsNullOrWhiteSpace(Artist) ? null : Artist.Trim(),
            Bpm = Bpm,
            MusicalKey = ParseOptionalKey(Key),
            Duration = Duration,
            FilePath = FilePath.Trim(),
            Format = Format,
        };
    }

    /// <summary>
    /// Empty/whitespace key becomes null; non-empty must parse as a letter key or throw.
    /// </summary>
    private static MusicalKey? ParseOptionalKey(string? rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return null;
        }

        if (!MusicalKey.TryParse(rawKey, out var key))
        {
            throw new InvalidMusicalKeyException(rawKey);
        }

        return key;
    }
}
