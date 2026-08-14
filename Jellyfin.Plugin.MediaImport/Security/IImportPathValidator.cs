namespace Jellyfin.Plugin.MediaImport.Security;

/// <summary>
/// Validates that the import roots have non-overlapping responsibilities.
/// </summary>
public interface IImportPathValidator
{
    /// <summary>
    /// Validates the configured import roots.
    /// </summary>
    /// <param name="inboxPath">The import queue root.</param>
    /// <param name="moviesLibraryPath">The movie library root.</param>
    /// <param name="seriesLibraryPath">The series library root.</param>
    void Validate(string inboxPath, string moviesLibraryPath, string seriesLibraryPath);
}
