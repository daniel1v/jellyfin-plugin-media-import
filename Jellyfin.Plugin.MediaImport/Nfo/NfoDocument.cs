namespace Jellyfin.Plugin.MediaImport.Nfo;

/// <summary>
/// A generated NFO sidecar and the metadata identity it must represent.
/// </summary>
public sealed record NfoDocument(
    string RelativePath,
    string RootElementName,
    string? TmdbId,
    string Content,
    int? SeasonNumber,
    int? EpisodeNumber);
