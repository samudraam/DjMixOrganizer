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
}
