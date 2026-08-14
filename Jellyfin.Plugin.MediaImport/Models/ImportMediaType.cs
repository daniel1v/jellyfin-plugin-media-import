namespace Jellyfin.Plugin.MediaImport.Models;

/// <summary>
/// The type of media selected for an import.
/// </summary>
public enum ImportMediaType
{
    /// <summary>
    /// A feature film.
    /// </summary>
    Movie,

    /// <summary>
    /// A television episode.
    /// </summary>
    Series
}
