using Jellyfin.Plugin.MediaImport.Services;
using Xunit;

namespace Jellyfin.Plugin.MediaImport.Tests;

public class MetadataSearchRequestFactoryTests
{
    [Fact]
    public void Movie_search_targets_enabled_tmdb_provider()
    {
        var request = MetadataSearchRequestFactory.CreateMovieSearch("Dune Part Two", 2024);

        Assert.Equal("TheMovieDb", request.SearchProviderName);
        Assert.False(request.IncludeDisabledProviders);
        Assert.Equal("Dune Part Two", request.SearchInfo.Name);
        Assert.Equal(2024, request.SearchInfo.Year);
    }

    [Fact]
    public void Series_search_targets_enabled_tmdb_provider()
    {
        var request = MetadataSearchRequestFactory.CreateSeriesSearch("Bluey", 2018);

        Assert.Equal("TheMovieDb", request.SearchProviderName);
        Assert.False(request.IncludeDisabledProviders);
        Assert.Equal("Bluey", request.SearchInfo.Name);
        Assert.Equal(2018, request.SearchInfo.Year);
    }

    [Fact]
    public void Episode_search_contains_series_provider_id_and_episode_numbers()
    {
        var request = MetadataSearchRequestFactory.CreateEpisodeSearch("82728", 2, 14);

        Assert.Equal("TheMovieDb", request.SearchProviderName);
        Assert.False(request.IncludeDisabledProviders);
        Assert.Equal("82728", request.SearchInfo.SeriesProviderIds["Tmdb"]);
        Assert.Equal(2, request.SearchInfo.ParentIndexNumber);
        Assert.Equal(14, request.SearchInfo.IndexNumber);
    }

    [Fact]
    public void Movie_lookup_uses_only_selected_tmdb_provider_id()
    {
        var request = MetadataSearchRequestFactory.CreateMovieLookup("693134");

        Assert.Equal("TheMovieDb", request.SearchProviderName);
        Assert.False(request.IncludeDisabledProviders);
        Assert.Equal("693134", request.SearchInfo.ProviderIds["Tmdb"]);
    }

    [Fact]
    public void Series_lookup_uses_only_selected_tmdb_provider_id()
    {
        var request = MetadataSearchRequestFactory.CreateSeriesLookup("82728");

        Assert.Equal("TheMovieDb", request.SearchProviderName);
        Assert.False(request.IncludeDisabledProviders);
        Assert.Equal("82728", request.SearchInfo.ProviderIds["Tmdb"]);
    }
}
