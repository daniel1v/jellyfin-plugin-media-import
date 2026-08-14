namespace Jellyfin.Plugin.MediaImport.Models;

/// <summary>
/// The result of moving one file into its Jellyfin library.
/// </summary>
public sealed record ImportResult(
    ImportPlan Plan,
    bool LibraryScanQueued);
