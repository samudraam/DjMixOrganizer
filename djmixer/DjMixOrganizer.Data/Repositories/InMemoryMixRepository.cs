using DjMixOrganizer.Core.Models;
using DjMixOrganizer.Core.Repositories;

namespace DjMixOrganizer.Data.Repositories;

public class InMemoryMixRepository : IMixRepository
{
    private readonly List<Mix> _seedData;

    public InMemoryMixRepository()
    {
        var bandit = new Track
        {
            Title = "Bandit (with Youngboy Never Broke Again)",
            Artist = "Juice WRLD",
            Bpm = 115,
            CamelotKey = "C",
            Duration = TimeSpan.FromMinutes(3.5),
            FilePath = "/seed/bandit.mp3",
        };
        var inMyMind = new Track
        {
            Title = "In My Mind",
            Bpm = 115,
            CamelotKey = "C",
            Duration = TimeSpan.FromMinutes(3.25),
            FilePath = "/seed/in-my-mind.mp3",
        };

        var warehouseSet = new Mix { Title = "Warehouse Set", RecordedDate = new DateOnly(2026, 1, 15) };
        warehouseSet.AddTrack(bandit, TimeSpan.Zero);
        warehouseSet.AddTrack(inMyMind, TimeSpan.FromMinutes(3.5));

        var sunsetSet = new Mix { Title = "Sunset Set", RecordedDate = new DateOnly(2026, 3, 2) };
        sunsetSet.AddTrack(inMyMind, TimeSpan.Zero);

        var closer = new Mix { Title = "Closer", RecordedDate = new DateOnly(2026, 5, 20) };

        _seedData = [warehouseSet, sunsetSet, closer];
    }

    public Task<IReadOnlyList<Mix>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<Mix>>(_seedData);
    }
}
