using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MediaImport.Models;

namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Searches the remote TMDb metadata provider configured in Jellyfin.
/// </summary>
public interface IMetadataSearchService
{
    /// <summary>
    /// Searches films by title and, when available, release year.
    /// </summary>
    /// <param name="title">The title to search for.</param>
    /// <param name="year">The optional release year.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching TMDb results.</returns>
    Task<IReadOnlyList<MetadataSearchResult>> SearchMoviesAsync(
        string title,
        int? year,
        CancellationToken cancellationToken);

    /// <summary>
    /// Searches series by title and, when available, first-air year.
    /// </summary>
    /// <param name="title">The title to search for.</param>
    /// <param name="year">The optional first-air year.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching TMDb results.</returns>
    Task<IReadOnlyList<MetadataSearchResult>> SearchSeriesAsync(
        string title,
        int? year,
        CancellationToken cancellationToken);

    /// <summary>
    /// Resolves a selected movie TMDb provider ID using Jellyfin's provider.
    /// </summary>
    /// <param name="tmdbId">The selected movie TMDb provider ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The freshly resolved movie, or <c>null</c> when no result was obtained.</returns>
    Task<MetadataSearchResult?> ResolveMovieAsync(string tmdbId, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves a selected series TMDb provider ID using Jellyfin's provider.
    /// </summary>
    /// <param name="tmdbId">The selected series TMDb provider ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The freshly resolved series, or <c>null</c> when no result was obtained.</returns>
    Task<MetadataSearchResult?> ResolveSeriesAsync(string tmdbId, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves an episode number for a selected series.
    /// </summary>
    /// <param name="seriesTmdbId">The TMDb provider ID of the selected series.</param>
    /// <param name="seasonNumber">The season number, including zero for specials.</param>
    /// <param name="episodeNumber">The episode number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved episode, or <c>null</c> when Jellyfin returns no matching result.</returns>
    Task<EpisodeResolution?> ResolveEpisodeAsync(
        string seriesTmdbId,
        int seasonNumber,
        int episodeNumber,
        CancellationToken cancellationToken);
}
