namespace Jellyfin.Plugin.MediaImport.Models;

/// <summary>
/// The episode resolved by Jellyfin's configured TMDb provider.
/// </summary>
public sealed record EpisodeResolution(
    string Name,
    int SeasonNumber,
    int EpisodeNumber,
    string? TmdbId);
