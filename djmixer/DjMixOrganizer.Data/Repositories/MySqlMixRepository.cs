using DjMixOrganizer.Core.Models;
using DjMixOrganizer.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DjMixOrganizer.Data.Repositories;

public class MySqlMixRepository(IDbContextFactory<DjMixDbContext> contextFactory) : IMixRepository
{
    public async Task<IReadOnlyList<Mix>> GetAllAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        return await context.Mixes
            .Include(mix => mix.Tracks)
            .ThenInclude(entry => entry.Track)
            .ToListAsync();
    }

    // `mix` is a disconnected graph — its Track references came from a
    // different DjMixDbContext (loaded earlier via ITrackRepository in the
    // ViewModel). Never trust that object identity directly: re-resolve
    // each Track by Id through *this* context (FindAsync checks the
    // identity map first, then falls back to a query) before attaching it,
    // or EF would treat every referenced Track as a brand-new row and fail
    // on a duplicate primary key.
    public async Task SaveAsync(Mix mix)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var existing = await context.Mixes
            .Include(m => m.Tracks)
            .ThenInclude(entry => entry.Track)
            .FirstOrDefaultAsync(m => m.Id == mix.Id);

        Mix target;
        if (existing is null)
        {
            target = new Mix { Id = mix.Id, Title = mix.Title, RecordedDate = mix.RecordedDate };
            context.Mixes.Add(target);
        }
        else
        {
            existing.Title = mix.Title;
            existing.RecordedDate = mix.RecordedDate;
            foreach (var trackId in existing.Tracks.Select(entry => entry.Track.Id).ToList())
            {
                existing.RemoveTrack(trackId);
            }

            target = existing;
        }

        foreach (var entry in mix.Tracks)
        {
            var track = await context.Tracks.FindAsync(entry.Track.Id)
                ?? throw new InvalidOperationException($"Track {entry.Track.Id} not found.");
            target.AddTrack(track, entry.StartTime);
        }

        await context.SaveChangesAsync();
    }
}
