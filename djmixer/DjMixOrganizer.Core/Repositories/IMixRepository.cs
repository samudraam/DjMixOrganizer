using DjMixOrganizer.Core.Models;

namespace DjMixOrganizer.Core.Repositories;

public interface IMixRepository
{
    Task<IReadOnlyList<Mix>> GetAllAsync();
}
