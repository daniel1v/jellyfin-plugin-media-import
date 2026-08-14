using Jellyfin.Plugin.MediaImport.Configuration;

namespace Jellyfin.Plugin.MediaImport.Services;

/// <summary>
/// Reads the current configuration from the loaded plugin instance.
/// </summary>
public sealed class PluginConfigurationAccessor : IPluginConfigurationAccessor
{
    /// <inheritdoc />
    public PluginConfiguration GetCurrent()
        => Plugin.Instance?.Configuration
            ?? throw new ImportValidationException("The Media Import plugin configuration is unavailable.");
}
