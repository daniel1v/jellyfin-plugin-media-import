using System;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Creates remote-search requests that use Jellyfin's built-in TMDb provider.
/// </summary>
internal static class MetadataSearchRequestFactory
{
    internal const string TmdbProviderName = "TheMovieDb";
    internal const string TmdbProviderIdName = "Tmdb";

    /// <summary>
    /// Creates a movie search request.
    /// </summary>
    /// <param name="title">The movie title.</param>
    /// <param name="year">The optional release year.</param>
    /// <returns>A request for Jellyfin's enabled TMDb provider.</returns>
    internal static RemoteSearchQuery<MovieInfo> CreateMovieSearch(string title, int? year)
        => new()
        {
            SearchInfo = new MovieInfo
            {
                Name = title,
                Year = year
            },
            SearchProviderName = TmdbProviderName,
            IncludeDisabledProviders = false
        };

    /// <summary>
    /// Creates a series search request.
    /// </summary>
    /// <param name="title">The series title.</param>
    /// <param name="year">The optional first-air year.</param>
    /// <returns>A request for Jellyfin's enabled TMDb provider.</returns>
    internal static RemoteSearchQuery<SeriesInfo> CreateSeriesSearch(string title, int? year)
        => new()
        {
            SearchInfo = new SeriesInfo
            {
                Name = title,
                Year = year
            },
            SearchProviderName = TmdbProviderName,
            IncludeDisabledProviders = false
        };

    /// <summary>
    /// Creates a movie lookup request for a selected TMDb provider ID.
    /// </summary>
    /// <param name="tmdbId">The selected movie TMDb provider ID.</param>
    /// <returns>A request for Jellyfin's enabled TMDb provider.</returns>
    internal static RemoteSearchQuery<MovieInfo> CreateMovieLookup(string tmdbId)
        => new()
        {
            SearchInfo = new MovieInfo
            {
                ProviderIds =
                {
                    [TmdbProviderIdName] = ValidateTmdbId(tmdbId)
                }
            },
            SearchProviderName = TmdbProviderName,
            IncludeDisabledProviders = false
        };

    /// <summary>
    /// Creates a series lookup request for a selected TMDb provider ID.
    /// </summary>
    /// <param name="tmdbId">The selected series TMDb provider ID.</param>
    /// <returns>A request for Jellyfin's enabled TMDb provider.</returns>
    internal static RemoteSearchQuery<SeriesInfo> CreateSeriesLookup(string tmdbId)
        => new()
        {
            SearchInfo = new SeriesInfo
            {
                ProviderIds =
                {
                    [TmdbProviderIdName] = ValidateTmdbId(tmdbId)
                }
            },
            SearchProviderName = TmdbProviderName,
            IncludeDisabledProviders = false
        };

    /// <summary>
    /// Creates an episode resolution request for a selected series.
    /// </summary>
    /// <param name="seriesTmdbId">The selected series' TMDb provider ID.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <param name="episodeNumber">The episode number.</param>
    /// <returns>A request for Jellyfin's enabled TMDb provider.</returns>
    internal static RemoteSearchQuery<EpisodeInfo> CreateEpisodeSearch(
        string seriesTmdbId,
        int seasonNumber,
        int episodeNumber)
    {
        return new RemoteSearchQuery<EpisodeInfo>
        {
            SearchInfo = new EpisodeInfo
            {
                ParentIndexNumber = seasonNumber,
                IndexNumber = episodeNumber,
                SeriesProviderIds =
                {
                    [TmdbProviderIdName] = ValidateTmdbId(seriesTmdbId)
                }
            },
            SearchProviderName = TmdbProviderName,
            IncludeDisabledProviders = false
        };
    }

    private static string ValidateTmdbId(string tmdbId)
    {
        if (!int.TryParse(tmdbId, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var parsedId)
            || parsedId <= 0)
        {
            throw new ArgumentException("The TMDb provider ID is invalid.", nameof(tmdbId));
        }

        return parsedId.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
