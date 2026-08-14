using System.Collections.Generic;

namespace Jellyfin.Plugin.MediaImport.Models;

/// <summary>
/// A server-generated, reviewable plan for importing one file.
/// </summary>
public sealed record ImportPlan(
    string SourceFileName,
    ImportMediaType MediaType,
    string TmdbId,
    string Title,
    int? Year,
    int? SeasonNumber,
    int? EpisodeNumber,
    string? EpisodeTitle,
    string? EpisodeTmdbId,
    string DestinationRoot,
    string DestinationRelativePath,
    string DestinationPath,
    IReadOnlyList<NfoSidecarPlan> NfoSidecars,
    bool CanImport,
    string? ConflictMessage);
