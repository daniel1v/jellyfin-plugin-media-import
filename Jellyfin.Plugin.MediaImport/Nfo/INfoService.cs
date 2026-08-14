using System.Collections.Generic;

namespace Jellyfin.Plugin.MediaImport.Nfo;

/// <summary>
/// Creates and validates the local NFO metadata used for deterministic Jellyfin identification.
/// </summary>
public interface INfoService
{
    /// <summary>
    /// Creates the NFO document for a movie.
    /// </summary>
    /// <param name="mediaRelativePath">The generated media path relative to the movie library.</param>
    /// <param name="title">The resolved movie title.</param>
    /// <param name="year">The resolved production year, when available.</param>
    /// <param name="tmdbId">The resolved TMDb movie provider ID.</param>
    /// <returns>The movie NFO document.</returns>
    IReadOnlyList<NfoDocument> CreateMovieDocuments(
        string mediaRelativePath,
        string title,
        int? year,
        string tmdbId);

    /// <summary>
    /// Creates the series and episode NFO documents for an episode.
    /// </summary>
    /// <param name="mediaRelativePath">The generated media path relative to the series library.</param>
    /// <param name="seriesTitle">The resolved series title.</param>
    /// <param name="seriesYear">The resolved series production year, when available.</param>
    /// <param name="seriesTmdbId">The resolved TMDb series provider ID.</param>
    /// <param name="seasonNumber">The selected season number.</param>
    /// <param name="episodeNumber">The selected episode number.</param>
    /// <param name="episodeTitle">The resolved episode title.</param>
    /// <param name="episodeTmdbId">The resolved TMDb episode provider ID.</param>
    /// <returns>The series and episode NFO documents.</returns>
    IReadOnlyList<NfoDocument> CreateEpisodeDocuments(
        string mediaRelativePath,
        string seriesTitle,
        int? seriesYear,
        string seriesTmdbId,
        int seasonNumber,
        int episodeNumber,
        string episodeTitle,
        string? episodeTmdbId);

    /// <summary>
    /// Verifies that an existing sidecar represents the expected document.
    /// </summary>
    /// <param name="path">The absolute sidecar path.</param>
    /// <param name="document">The expected NFO document.</param>
    void ValidateExisting(string path, NfoDocument document);

    /// <summary>
    /// Creates a new sidecar without overwriting an existing filesystem entry.
    /// </summary>
    /// <param name="path">The absolute sidecar path.</param>
    /// <param name="document">The NFO document to write.</param>
    void WriteNew(string path, NfoDocument document);
}
