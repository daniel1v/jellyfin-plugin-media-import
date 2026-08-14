namespace Jellyfin.Plugin.MediaImport.Models;

/// <summary>
/// A server-generated NFO sidecar that accompanies an imported media file.
/// </summary>
public sealed record NfoSidecarPlan(
    string RelativePath,
    string Path,
    bool AlreadyExists);
