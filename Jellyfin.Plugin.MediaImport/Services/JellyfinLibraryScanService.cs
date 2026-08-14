using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Queues scans through Jellyfin's library manager.
/// </summary>
public sealed class JellyfinLibraryScanService : ILibraryScanService
{
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinLibraryScanService"/> class.
    /// </summary>
    /// <param name="libraryManager">Jellyfin's library manager.</param>
    public JellyfinLibraryScanService(ILibraryManager libraryManager)
    {
        _libraryManager = libraryManager;
    }

    /// <inheritdoc />
    public void QueueScan()
    {
        _libraryManager.QueueLibraryScan();
    }
}
