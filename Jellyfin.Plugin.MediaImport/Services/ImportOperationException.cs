using System;

namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Indicates an unexpected failure while moving an otherwise valid import.
/// </summary>
public sealed class ImportOperationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImportOperationException"/> class.
    /// </summary>
    /// <param name="message">The safe user-facing error message.</param>
    /// <param name="innerException">The underlying filesystem exception.</param>
    public ImportOperationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
