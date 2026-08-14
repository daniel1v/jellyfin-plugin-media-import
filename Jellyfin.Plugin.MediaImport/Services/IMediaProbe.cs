using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Reads technical details from one local video file.
/// </summary>
public interface IMediaProbe
{
    /// <summary>
    /// Probes a validated local video path.
    /// </summary>
    /// <param name="path">The absolute local path.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The available technical video details.</returns>
    Task<MediaProbeResult> ProbeAsync(string path, CancellationToken cancellationToken);
}
