using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MediaImport.Models;

namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Builds reviewable plans and executes confirmed single-file imports.
/// </summary>
public interface IImportService
{
    /// <summary>
    /// Resolves metadata and builds a server-controlled target path without changing files.
    /// </summary>
    /// <param name="request">The administrator's source and metadata selection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The import plan.</returns>
    Task<ImportPlan> PreviewAsync(ImportRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Rebuilds and validates the plan, then moves the file without overwriting.
    /// </summary>
    /// <param name="request">The administrator-confirmed source and metadata selection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The import result.</returns>
    Task<ImportResult> ImportAsync(ImportRequest request, CancellationToken cancellationToken);
}
