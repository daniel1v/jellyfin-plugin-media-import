using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MediaImport.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the directory from which media files are proposed for import.
    /// </summary>
    public string InboxPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination directory for films.
    /// </summary>
    public string MoviesLibraryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination directory for series.
    /// </summary>
    public string SeriesLibraryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether a proposed import must remain a preview
    /// until the administrator explicitly confirms it.
    /// </summary>
    public bool RequireExplicitConfirmation { get; set; } = true;
}
