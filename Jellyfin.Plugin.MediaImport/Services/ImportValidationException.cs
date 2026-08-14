using System;

namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Indicates invalid or unsafe import input or configuration.
/// </summary>
public sealed class ImportValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImportValidationException"/> class.
    /// </summary>
    /// <param name="message">The safe user-facing error message.</param>
    public ImportValidationException(string message)
        : base(message)
    {
    }
}
