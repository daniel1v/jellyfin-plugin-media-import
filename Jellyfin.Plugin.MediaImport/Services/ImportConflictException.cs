using System;

namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Indicates that the source queue or target changed after review.
/// </summary>
public sealed class ImportConflictException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImportConflictException"/> class.
    /// </summary>
    /// <param name="message">The safe user-facing error message.</param>
    public ImportConflictException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportConflictException"/> class.
    /// </summary>
    /// <param name="message">The safe user-facing error message.</param>
    /// <param name="innerException">The underlying validation exception.</param>
    public ImportConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
