using DjMixOrganizer.Core.Models;
using DjMixOrganizer.Core.Repositories;

namespace DjMixOrganizer.Data.Repositories;

public class InMemoryTrackRepository : ITrackRepository
{
    private readonly List<Track> _seedData =
    [
        new Track
        {
            Title = "Bandit (with Youngboy Never Broke Again)",
            Artist = "Juice WRLD",
            Bpm = 115,
            CamelotKey = "C",
            Duration = TimeSpan.FromMinutes(3.5),
            FilePath = "/seed/bandit.mp3",
        },
        new Track
        {
            Title = "In My Mind",
            Bpm = 115,
            CamelotKey = "C",
            Duration = TimeSpan.FromMinutes(3.25),
            FilePath = "/seed/in-my-mind.mp3",
        },
        new Track
        {
            Title = "Sunset Drift",
            Artist = "Nocturne",
            Bpm = 124,
            CamelotKey = "8A",
            Duration = TimeSpan.FromMinutes(4.1),
            FilePath = "/seed/sunset-drift.mp3",
        },
    ];

    public Task<IReadOnlyList<Track>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<Track>>(_seedData);
    }
}
