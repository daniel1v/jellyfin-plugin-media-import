using System;

namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Indicates that Jellyfin could not read technical details from a selected video file.
/// </summary>
public sealed class MediaProbeException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MediaProbeException"/> class.
    /// </summary>
    /// <param name="message">The safe user-facing error message.</param>
    /// <param name="innerException">The underlying Jellyfin media probe exception.</param>
    public MediaProbeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
