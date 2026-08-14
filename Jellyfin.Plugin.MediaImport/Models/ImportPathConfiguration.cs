namespace Jellyfin.Plugin.MediaImport.Models;

/// <summary>
/// The three configurable roots used by the import workflow.
/// </summary>
public sealed record ImportPathConfiguration(
    string InboxPath,
    string MoviesLibraryPath,
    string SeriesLibraryPath);
