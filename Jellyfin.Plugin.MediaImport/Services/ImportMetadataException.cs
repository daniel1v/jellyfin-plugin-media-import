using System;

namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Indicates that the selected metadata could not be resolved again.
/// </summary>
public sealed class ImportMetadataException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImportMetadataException"/> class.
    /// </summary>
    /// <param name="message">The safe user-facing error message.</param>
    public ImportMetadataException(string message)
        : base(message)
    {
    }
}
