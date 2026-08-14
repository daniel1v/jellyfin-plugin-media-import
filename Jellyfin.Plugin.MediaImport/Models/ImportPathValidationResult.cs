namespace Jellyfin.Plugin.MediaImport.Models;

/// <summary>
/// The result of validating proposed import roots.
/// </summary>
public sealed record ImportPathValidationResult(
    bool IsValid,
    string? Message);
