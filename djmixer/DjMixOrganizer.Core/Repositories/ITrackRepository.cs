using DjMixOrganizer.Core.Models;

namespace DjMixOrganizer.Core.Repositories;

public interface ITrackRepository
{
    Task<IReadOnlyList<Track>> GetAllAsync();
}
