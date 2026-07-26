using DjMixOrganizer.Core.Models;
using DjMixOrganizer.Data.Repositories;

namespace DjMixOrganizer.Tests;

public class InMemoryMixRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsSeedData()
    {
        var repository = new InMemoryMixRepository();

        var mixes = await repository.GetAllAsync();

        Assert.NotEmpty(mixes);
    }

    [Fact]
    public async Task GetAllAsync_SeedMixesHaveDistinctTitles()
    {
        var repository = new InMemoryMixRepository();

        var mixes = await repository.GetAllAsync();

        Assert.Equal(mixes.Count, mixes.Select(m => m.Title).Distinct().Count());
    }

    /// <summary>
    /// Regression: Save must insert a brand-new mix so a later GetAllAsync sees it.
    /// (The UI bug was the list not reloading; this locks the repository contract.)
    /// </summary>
    [Fact]
    public async Task SaveAsync_NewMix_AppearsInGetAllAsync()
    {
        var repository = new InMemoryMixRepository();
        var beforeCount = (await repository.GetAllAsync()).Count;

        var mix = new Mix
        {
            Id = Guid.NewGuid(),
            Title = "Brand New Warehouse Set",
            RecordedDate = new DateOnly(2026, 7, 26),
        };

        await repository.SaveAsync(mix);

        var after = await repository.GetAllAsync();
        Assert.Equal(beforeCount + 1, after.Count);
        Assert.Contains(after, m => m.Id == mix.Id && m.Title == "Brand New Warehouse Set");
    }

    [Fact]
    public async Task SaveAsync_ExistingMix_UpdatesTitleWithoutDuplicating()
    {
        var repository = new InMemoryMixRepository();
        var original = (await repository.GetAllAsync()).First();
        var countBefore = (await repository.GetAllAsync()).Count;

        var updated = new Mix
        {
            Id = original.Id,
            Title = "Renamed Set",
            RecordedDate = original.RecordedDate,
        };
        await repository.SaveAsync(updated);

        var after = await repository.GetAllAsync();
        Assert.Equal(countBefore, after.Count);
        Assert.Contains(after, m => m.Id == original.Id && m.Title == "Renamed Set");
    }
}
