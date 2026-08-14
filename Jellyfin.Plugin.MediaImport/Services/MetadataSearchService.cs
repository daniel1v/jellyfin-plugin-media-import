using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MediaImport.Models;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Wraps Jellyfin's remote metadata provider API for Media Import.
/// </summary>
public sealed class MetadataSearchService : IMetadataSearchService
{
    private readonly IProviderManager _providerManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataSearchService"/> class.
    /// </summary>
    /// <param name="providerManager">Jellyfin's metadata provider manager.</param>
    public MetadataSearchService(IProviderManager providerManager)
    {
        _providerManager = providerManager;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MetadataSearchResult>> SearchMoviesAsync(
        string title,
        int? year,
        CancellationToken cancellationToken)
    {
        ValidateTitle(title);

        var results = await _providerManager
            .GetRemoteSearchResults<Movie, MovieInfo>(
                MetadataSearchRequestFactory.CreateMovieSearch(title, year),
                cancellationToken)
            .ConfigureAwait(false);

        return ToMetadataResults(results);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MetadataSearchResult>> SearchSeriesAsync(
        string title,
        int? year,
        CancellationToken cancellationToken)
    {
        ValidateTitle(title);

        var results = await _providerManager
            .GetRemoteSearchResults<Series, SeriesInfo>(
                MetadataSearchRequestFactory.CreateSeriesSearch(title, year),
                cancellationToken)
            .ConfigureAwait(false);

        return ToMetadataResults(results);
    }

    /// <inheritdoc />
    public async Task<MetadataSearchResult?> ResolveMovieAsync(
        string tmdbId,
        CancellationToken cancellationToken)
    {
        var results = await _providerManager
            .GetRemoteSearchResults<Movie, MovieInfo>(
                MetadataSearchRequestFactory.CreateMovieLookup(tmdbId),
                cancellationToken)
            .ConfigureAwait(false);

        return ToMetadataResults(results).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<MetadataSearchResult?> ResolveSeriesAsync(
        string tmdbId,
        CancellationToken cancellationToken)
    {
        var results = await _providerManager
            .GetRemoteSearchResults<Series, SeriesInfo>(
                MetadataSearchRequestFactory.CreateSeriesLookup(tmdbId),
                cancellationToken)
            .ConfigureAwait(false);

        return ToMetadataResults(results).FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<EpisodeResolution?> ResolveEpisodeAsync(
        string seriesTmdbId,
        int seasonNumber,
        int episodeNumber,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seriesTmdbId);
        ArgumentOutOfRangeException.ThrowIfNegative(seasonNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(episodeNumber);

        var results = await _providerManager
            .GetRemoteSearchResults<Episode, EpisodeInfo>(
                MetadataSearchRequestFactory.CreateEpisodeSearch(seriesTmdbId, seasonNumber, episodeNumber),
                cancellationToken)
            .ConfigureAwait(false);

        var episode = results.FirstOrDefault(result =>
            result.ParentIndexNumber == seasonNumber && result.IndexNumber == episodeNumber);

        return episode is null ? null : ToEpisodeResolution(episode);
    }

    private static MetadataSearchResult[] ToMetadataResults(IEnumerable<RemoteSearchResult> results)
        => results
            .Select(ToMetadataSearchResult)
            .OfType<MetadataSearchResult>()
            .ToArray();

    private static MetadataSearchResult? ToMetadataSearchResult(RemoteSearchResult result)
        => !string.IsNullOrWhiteSpace(result.Name)
            && result.ProviderIds.TryGetValue(MetadataSearchRequestFactory.TmdbProviderIdName, out var tmdbId)
            && !string.IsNullOrWhiteSpace(tmdbId)
            ? new MetadataSearchResult(result.Name, result.ProductionYear ?? result.PremiereDate?.Year, tmdbId)
            : null;

    private static EpisodeResolution? ToEpisodeResolution(RemoteSearchResult result)
    {
        if (string.IsNullOrWhiteSpace(result.Name))
        {
            return null;
        }

        result.ProviderIds.TryGetValue(MetadataSearchRequestFactory.TmdbProviderIdName, out var tmdbId);
        return new EpisodeResolution(
            result.Name,
            result.ParentIndexNumber!.Value,
            result.IndexNumber!.Value,
            tmdbId);
    }

    private static void ValidateTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
    }
}
