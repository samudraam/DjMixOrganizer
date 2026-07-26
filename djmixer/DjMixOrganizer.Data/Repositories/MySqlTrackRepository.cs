using DjMixOrganizer.Core.Models;
using DjMixOrganizer.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DjMixOrganizer.Data.Repositories;

public class MySqlTrackRepository(IDbContextFactory<DjMixDbContext> contextFactory) : ITrackRepository
{
    public async Task<IReadOnlyList<Track>> GetAllAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        return await context.Tracks.ToListAsync();
    }
}
