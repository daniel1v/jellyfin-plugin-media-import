namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Technical video details returned by Jellyfin's media probe.
/// </summary>
public sealed record MediaProbeResult(long? RunTimeTicks, int? Width, int? Height);
