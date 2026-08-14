using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Plugin.MediaImport.Models;

/// <summary>
/// Identifies an inbox file and the administrator's metadata selection.
/// </summary>
public sealed class ImportRequest
{
    /// <summary>
    /// Gets or sets the filename relative to the configured inbox.
    /// </summary>
    [Required]
    public string SourceFileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the selected media type.
    /// </summary>
    public ImportMediaType MediaType { get; set; }

    /// <summary>
    /// Gets or sets the selected movie or series TMDb provider ID.
    /// </summary>
    [Required]
    [RegularExpression("^[1-9][0-9]*$")]
    public string TmdbId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the season number for a series import.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int? SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode number for a series import.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? EpisodeNumber { get; set; }
}
