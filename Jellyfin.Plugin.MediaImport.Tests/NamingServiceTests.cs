using System.IO;
using Jellyfin.Plugin.MediaImport.Naming;
using Xunit;

namespace Jellyfin.Plugin.MediaImport.Tests;

public class NamingServiceTests
{
    private readonly NamingService _service = new();

    [Fact]
    public void Creates_jellyfin_movie_path_and_removes_invalid_characters()
    {
        var path = _service.GetMovieRelativePath("Dune: Part Two", 2024, ".mkv");

        Assert.Equal(
            Path.Combine(
                "Dune Part Two (2024)",
                "Dune Part Two (2024).mkv"),
            path);
    }

    [Fact]
    public void Creates_jellyfin_episode_path()
    {
        var path = _service.GetEpisodeRelativePath("Bluey", 2018, 2, 14, "Mum School", ".mkv");

        Assert.Equal(
            Path.Combine(
                "Bluey (2018)",
                "Season 02",
                "Bluey S02E14 Mum School.mkv"),
            path);
    }

    [Fact]
    public void Uses_season_zero_for_specials()
    {
        var path = _service.GetEpisodeRelativePath("Bluey", 2018, 0, 1, "Special", ".mp4");

        Assert.Contains(Path.Combine("Season 00", "Bluey S00E01 Special.mp4"), path);
    }
}
