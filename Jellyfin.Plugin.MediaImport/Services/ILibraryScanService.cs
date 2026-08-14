namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Queues a Jellyfin library scan after a successful file move.
/// </summary>
public interface ILibraryScanService
{
    /// <summary>
    /// Queues a library scan.
    /// </summary>
    void QueueScan();
}
