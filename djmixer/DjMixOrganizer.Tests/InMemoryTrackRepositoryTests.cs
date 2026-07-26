using DjMixOrganizer.Core.Enums;
using DjMixOrganizer.Core.Models;
using DjMixOrganizer.Data.Repositories;

namespace DjMixOrganizer.Tests;

/// <summary>Tests track upload normalization through the in-memory repository.</summary>
public class InMemoryTrackRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsCanonicalLetterKey()
    {
        var repository = new InMemoryTrackRepository();
        var upload = new TrackUpload(
            Title: "Test Groove",
            Artist: "Unit Test",
            Bpm: 120,
            Key: "a minor",
            Duration: TimeSpan.FromMinutes(3),
            FilePath: "/tmp/test-groove.mp3");

        var track = await repository.AddAsync(upload);

        Assert.Equal(new MusicalKey("Am"), track.MusicalKey);

        var all = await repository.GetAllAsync();
        Assert.Contains(all, t => t.Id == track.Id && t.MusicalKey == new MusicalKey("Am"));
    }

    [Fact]
    public async Task AddAsync_WithCamelotKey_ThrowsInvalidMusicalKeyException()
    {
        var repository = new InMemoryTrackRepository();
        var upload = new TrackUpload(
            Title: "Bad Key Track",
            Artist: null,
            Bpm: 128,
            Key: "8A",
            Duration: TimeSpan.FromMinutes(4),
            FilePath: "/tmp/bad-key.mp3",
            Format: AudioFormat.Mp3);

        await Assert.ThrowsAsync<InvalidMusicalKeyException>(() => repository.AddAsync(upload));
    }

    [Fact]
    public async Task AddAsync_WithEmptyKey_StoresNullMusicalKey()
    {
        var repository = new InMemoryTrackRepository();
        var upload = new TrackUpload(
            Title: "No Key Yet",
            Artist: null,
            Bpm: null,
            Key: null,
            Duration: TimeSpan.FromMinutes(2),
            FilePath: "/tmp/no-key.mp3");

        var track = await repository.AddAsync(upload);

        Assert.Null(track.MusicalKey);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesExistingTrackMetadata()
    {
        var repository = new InMemoryTrackRepository();
        var created = await repository.AddAsync(new TrackUpload(
            Title: "Original",
            Artist: "A",
            Bpm: 100,
            Key: "C",
            Duration: TimeSpan.FromMinutes(1),
            FilePath: "/tmp/original.mp3"));

        var originalImportedAt = created.ImportedAt;
        var updatedResult = await repository.UpdateAsync(
            created.Id,
            new TrackUpload(
                Title: "Updated",
                Artist: "B",
                Bpm: 124,
                Key: "C#",
                Duration: TimeSpan.FromMinutes(2),
                FilePath: "/tmp/replacement.flac",
                Format: AudioFormat.Flac));

        var all = await repository.GetAllAsync();
        var updated = Assert.Single(all, t => t.Id == created.Id);
        Assert.Equal("Updated", updated.Title);
        Assert.Equal(new MusicalKey("Db"), updated.MusicalKey);
        Assert.Equal("/tmp/replacement.flac", updated.FilePath);
        Assert.Equal(AudioFormat.Flac, updated.Format);
        Assert.Equal(originalImportedAt, updated.ImportedAt);
        Assert.Same(updatedResult, updated);
    }
}
