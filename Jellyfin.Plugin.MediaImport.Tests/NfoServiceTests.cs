using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Jellyfin.Plugin.MediaImport.Nfo;
using Jellyfin.Plugin.MediaImport.Services;
using Xunit;

namespace Jellyfin.Plugin.MediaImport.Tests;

public sealed class NfoServiceTests : IDisposable
{
    private readonly string _root = Directory.CreateDirectory(
        Path.Combine(Path.GetTempPath(), $"media-import-nfo-tests-{Guid.NewGuid():N}")).FullName;
    private readonly NfoService _service = new();

    [Fact]
    public void Creates_movie_nfo_with_tmdb_identity()
    {
        var document = Assert.Single(
            _service.CreateMovieDocuments(
                Path.Combine("Dune Part Two (2024)", "Dune Part Two (2024).mkv"),
                "Dune: Part Two",
                2024,
                "693134"));

        Assert.Equal(Path.Combine("Dune Part Two (2024)", "movie.nfo"), document.RelativePath);
        Assert.Equal("movie", document.RootElementName);
        Assert.Equal("693134", document.TmdbId);
        var xml = XDocument.Parse(document.Content);
        Assert.Equal("Dune: Part Two", xml.Root?.Element("title")?.Value);
        Assert.Equal("2024", xml.Root?.Element("year")?.Value);
        Assert.Equal("693134", xml.Root?.Element("tmdbid")?.Value);
    }

    [Fact]
    public void Creates_series_and_episode_nfos()
    {
        var documents = _service.CreateEpisodeDocuments(
            Path.Combine("Bluey (2018)", "Season 02", "Bluey S02E14 Mum School.mkv"),
            "Bluey",
            2018,
            "82728",
            2,
            14,
            "Mum School",
            "123456");

        Assert.Collection(
            documents,
            series =>
            {
                Assert.Equal(Path.Combine("Bluey (2018)", "tvshow.nfo"), series.RelativePath);
                Assert.Contains("<tmdbid>82728</tmdbid>", series.Content, StringComparison.Ordinal);
            },
            episode =>
            {
                Assert.Equal(Path.Combine("Bluey (2018)", "Season 02", "Bluey S02E14 Mum School.nfo"), episode.RelativePath);
                var xml = XDocument.Parse(episode.Content);
                Assert.Equal("episodedetails", xml.Root?.Name.LocalName);
                Assert.Equal("2", xml.Root?.Element("season")?.Value);
                Assert.Equal("14", xml.Root?.Element("episode")?.Value);
                Assert.Equal("123456", xml.Root?.Element("tmdbid")?.Value);
            });
    }

    [Fact]
    public void Existing_matching_uniqueid_is_accepted()
    {
        var document = Assert.Single(
            _service.CreateMovieDocuments(Path.Combine("Movie", "Movie.mkv"), "Movie", 2024, "42"));
        var path = Path.Combine(_root, "movie.nfo");
        File.WriteAllText(path, "<movie><uniqueid type=\"tmdb\">42</uniqueid></movie>");

        _service.ValidateExisting(path, document);
    }

    [Fact]
    public void Existing_nfo_with_different_id_is_rejected()
    {
        var document = Assert.Single(
            _service.CreateMovieDocuments(Path.Combine("Movie", "Movie.mkv"), "Movie", 2024, "42"));
        var path = Path.Combine(_root, "movie.nfo");
        File.WriteAllText(path, "<movie><tmdbid>84</tmdbid></movie>");

        Assert.Throws<ImportConflictException>(() => _service.ValidateExisting(path, document));
    }

    [Fact]
    public void Write_new_never_overwrites_an_existing_file()
    {
        var document = Assert.Single(
            _service.CreateMovieDocuments(Path.Combine("Movie", "Movie.mkv"), "Movie", 2024, "42"));
        var path = Path.Combine(_root, "movie.nfo");
        File.WriteAllText(path, "keep");

        Assert.Throws<IOException>(() => _service.WriteNew(path, document));
        Assert.Equal("keep", File.ReadAllText(path));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(_root, true);
    }
}
