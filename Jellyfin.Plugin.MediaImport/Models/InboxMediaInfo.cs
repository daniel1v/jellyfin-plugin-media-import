namespace Jellyfin.Plugin.MediaImport.Models;

/// <summary>
/// Lightweight technical details for a video waiting in the inbox.
/// </summary>
public sealed record InboxMediaInfo(
    string FileName,
    long? RunTimeTicks,
    int? Width,
    int? Height);
