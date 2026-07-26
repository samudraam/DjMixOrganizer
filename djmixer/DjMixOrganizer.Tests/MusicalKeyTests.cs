using DjMixOrganizer.Core.Models;

namespace DjMixOrganizer.Tests;

/// <summary>Unit tests for classical letter-key parsing and rejection of Camelot/junk.</summary>
public class MusicalKeyTests
{
    [Theory]
    [InlineData("am", "Am")]
    [InlineData("A minor", "Am")]
    [InlineData("Am", "Am")]
    [InlineData("C", "C")]
    [InlineData("c major", "C")]
    [InlineData("Cmaj", "C")]
    [InlineData("F#m", "F#m")]
    [InlineData("Db", "Db")]
    [InlineData("D♭", "Db")]
    [InlineData("C#", "Db")]
    [InlineData("C#m", "Dbm")]
    [InlineData("bbm", "Bbm")]
    [InlineData("  Ebm  ", "Ebm")]
    public void TryParse_AcceptsLetterVariants_AsCanonical(string input, string expected)
    {
        var ok = MusicalKey.TryParse(input, out var key);

        Assert.True(ok);
        Assert.Equal(expected, key.Value);
    }

    [Theory]
    [InlineData("8A")]
    [InlineData("3b")]
    [InlineData("12B")]
    [InlineData("6m")]
    [InlineData("1d")]
    [InlineData("a c b")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("H")]
    [InlineData(null)]
    public void TryParse_RejectsCamelotOpenKeyAndJunk(string? input)
    {
        var ok = MusicalKey.TryParse(input, out _);

        Assert.False(ok);
    }

    [Fact]
    public void All_ContainsTwentyFourCanonicalKeys()
    {
        Assert.Equal(24, MusicalKey.All.Count);
        Assert.Contains("C", MusicalKey.All);
        Assert.Contains("Db", MusicalKey.All);
        Assert.Contains("Dbm", MusicalKey.All);
        Assert.Contains("Am", MusicalKey.All);
        Assert.Contains("F#m", MusicalKey.All);
        Assert.DoesNotContain("C#", MusicalKey.All);
        Assert.DoesNotContain("8A", MusicalKey.All);
    }
}
