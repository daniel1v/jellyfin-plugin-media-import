namespace Jellyfin.Plugin.MediaImport.Models;

/// <summary>
/// A TMDb-backed metadata result supplied by Jellyfin's configured provider.
/// </summary>
public sealed record MetadataSearchResult(
    string Name,
    int? ProductionYear,
    string TmdbId);
