using System.Collections.Generic;

namespace Jellyfin.Plugin.MediaImport.Models;

/// <summary>
/// Results returned by Jellyfin's enabled TMDb provider.
/// </summary>
/// <typeparam name="T">The result type.</typeparam>
public sealed record MetadataSearchResponse<T>(
    IReadOnlyList<T> Results,
    string? Message);
