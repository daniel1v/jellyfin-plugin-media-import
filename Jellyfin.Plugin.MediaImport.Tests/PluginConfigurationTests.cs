using Jellyfin.Plugin.MediaImport.Configuration;
using Xunit;

namespace Jellyfin.Plugin.MediaImport.Tests;

public class PluginConfigurationTests
{
    [Fact]
    public void Defaults_require_explicit_confirmation()
    {
        var configuration = new PluginConfiguration();

        Assert.True(configuration.RequireExplicitConfirmation);
        Assert.Empty(configuration.InboxPath);
        Assert.Empty(configuration.MoviesLibraryPath);
        Assert.Empty(configuration.SeriesLibraryPath);
    }
}
