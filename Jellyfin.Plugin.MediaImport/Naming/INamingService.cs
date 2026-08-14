namespace Jellyfin.Plugin.MediaImport.Naming;

/// <summary>
/// Creates Jellyfin-compatible relative media paths.
/// </summary>
public interface INamingService
{
    /// <summary>
    /// Creates the relative folder and filename for a movie.
    /// </summary>
    /// <param name="title">The resolved movie title.</param>
    /// <param name="year">The optional release year.</param>
    /// <param name="extension">The source file extension.</param>
    /// <returns>A relative path rooted below the configured movie library.</returns>
    string GetMovieRelativePath(string title, int? year, string extension);

    /// <summary>
    /// Creates the relative folders and filename for a series episode.
    /// </summary>
    /// <param name="seriesTitle">The resolved series title.</param>
    /// <param name="seriesYear">The optional first-air year.</param>
    /// <param name="seasonNumber">The resolved season number.</param>
    /// <param name="episodeNumber">The resolved episode number.</param>
    /// <param name="episodeTitle">The resolved episode title.</param>
    /// <param name="extension">The source file extension.</param>
    /// <returns>A relative path rooted below the configured series library.</returns>
    string GetEpisodeRelativePath(
        string seriesTitle,
        int? seriesYear,
        int seasonNumber,
        int episodeNumber,
        string episodeTitle,
        string extension);
}
