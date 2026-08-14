namespace Jellyfin.Plugin.MediaImport.Models;

/// <summary>
/// Conservative metadata inferred from an inbox file name.
/// </summary>
public sealed record ParsedFileName(
    string? SuggestedTitle,
    int? Year,
    int? SeasonNumber,
    int? EpisodeNumber,
    bool IsGeneric);
