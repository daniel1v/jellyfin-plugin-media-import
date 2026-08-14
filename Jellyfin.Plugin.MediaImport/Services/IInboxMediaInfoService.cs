using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MediaImport.Models;

namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Provides cached technical details for inbox files.
/// </summary>
public interface IInboxMediaInfoService
{
    /// <summary>
    /// Gets technical details for one validated inbox file.
    /// </summary>
    /// <param name="inboxPath">The configured inbox root.</param>
    /// <param name="sourceFileName">The filename relative to the inbox.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The available technical video details.</returns>
    Task<InboxMediaInfo> GetAsync(
        string inboxPath,
        string sourceFileName,
        CancellationToken cancellationToken);
}
