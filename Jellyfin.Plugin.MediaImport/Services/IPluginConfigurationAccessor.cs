using Jellyfin.Plugin.MediaImport.Configuration;

namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Provides the current Media Import configuration.
/// </summary>
public interface IPluginConfigurationAccessor
{
    /// <summary>
    /// Gets the current configuration.
    /// </summary>
    /// <returns>The current configuration.</returns>
    PluginConfiguration GetCurrent();
}
