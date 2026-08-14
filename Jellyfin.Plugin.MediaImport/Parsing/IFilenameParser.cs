using Jellyfin.Plugin.MediaImport.Models;

namespace Jellyfin.Plugin.MediaImport.Parsing;

/// <summary>
/// Extracts conservative search hints from a media filename.
/// </summary>
public interface IFilenameParser
{
    /// <summary>
    /// Parses a filename without inventing metadata for generic rip names.
    /// </summary>
    /// <param name="fileName">The filename, with or without its extension.</param>
    /// <returns>The inferred search hints.</returns>
    ParsedFileName Parse(string fileName);
}
