using Jellyfin.Plugin.MediaImport.Parsing;
using Xunit;

namespace Jellyfin.Plugin.MediaImport.Tests;

public class FilenameParserTests
{
    private readonly FilenameParser _parser = new();

    [Fact]
    public void Generic_bluray_name_does_not_invent_metadata()
    {
        var result = _parser.Parse("title_t00.mkv");

        Assert.True(result.IsGeneric);
        Assert.Null(result.SuggestedTitle);
        Assert.Null(result.Year);
        Assert.Null(result.SeasonNumber);
        Assert.Null(result.EpisodeNumber);
    }

    [Fact]
    public void Parses_movie_title_and_year()
    {
        var result = _parser.Parse("Dune.Part.Two.2024.mkv");

        Assert.False(result.IsGeneric);
        Assert.Equal("Dune Part Two", result.SuggestedTitle);
        Assert.Equal(2024, result.Year);
        Assert.Null(result.SeasonNumber);
        Assert.Null(result.EpisodeNumber);
    }

    [Theory]
    [InlineData("Bluey.2018.S02E14.Mum.School.mkv")]
    [InlineData("Bluey.2018.2x14.Mum.School.mp4")]
    public void Parses_series_year_season_and_episode(string fileName)
    {
        var result = _parser.Parse(fileName);

        Assert.Equal("Bluey", result.SuggestedTitle);
        Assert.Equal(2018, result.Year);
        Assert.Equal(2, result.SeasonNumber);
        Assert.Equal(14, result.EpisodeNumber);
    }

    [Fact]
    public void Title_without_reliable_numbers_remains_only_a_search_hint()
    {
        var result = _parser.Parse("Arrival.m4v");

        Assert.Equal("Arrival", result.SuggestedTitle);
        Assert.Null(result.Year);
        Assert.Null(result.SeasonNumber);
        Assert.Null(result.EpisodeNumber);
    }
}
