using System;
using System.IO;
using Jellyfin.Plugin.MediaImport.Services;

namespace Jellyfin.Plugin.MediaImport.Security;

/// <summary>
/// Prevents an inbox or library from containing another configured import root.
/// </summary>
public sealed class ImportPathValidator : IImportPathValidator
{
    /// <inheritdoc />
    public void Validate(string inboxPath, string moviesLibraryPath, string seriesLibraryPath)
    {
        var inbox = NormalizeOptionalPath(inboxPath, "inbox");
        var movies = NormalizeOptionalPath(moviesLibraryPath, "movie library");
        var series = NormalizeOptionalPath(seriesLibraryPath, "series library");

        RejectOverlap(inbox, "inbox", movies, "movie library");
        RejectOverlap(inbox, "inbox", series, "series library");
        RejectOverlap(movies, "movie library", series, "series library");
    }

    private static string? NormalizeOptionalPath(string path, string description)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new ImportValidationException($"The configured Media Import {description} path is invalid.");
        }
    }

    private static void RejectOverlap(
        string? firstPath,
        string firstDescription,
        string? secondPath,
        string secondDescription)
    {
        if (firstPath is null || secondPath is null)
        {
            return;
        }

        if (Contains(firstPath, secondPath) || Contains(secondPath, firstPath))
        {
            throw new ImportValidationException(
                $"The Media Import {firstDescription} and {secondDescription} paths must be separate and must not contain one another.");
        }
    }

    private static bool Contains(string parentPath, string candidatePath)
    {
        var relativePath = Path.GetRelativePath(parentPath, candidatePath);
        return relativePath.Equals(".", StringComparison.Ordinal)
            || (!Path.IsPathRooted(relativePath)
                && !relativePath.Equals("..", StringComparison.Ordinal)
                && !relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !relativePath.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal));
    }
}
