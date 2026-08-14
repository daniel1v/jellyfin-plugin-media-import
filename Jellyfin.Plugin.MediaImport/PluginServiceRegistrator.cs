using Jellyfin.Plugin.MediaImport.Naming;
using Jellyfin.Plugin.MediaImport.Nfo;
using Jellyfin.Plugin.MediaImport.Parsing;
using Jellyfin.Plugin.MediaImport.Security;
using Jellyfin.Plugin.MediaImport.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.MediaImport;

/// <summary>
/// Registers Media Import services with Jellyfin's dependency injection container.
/// </summary>
public sealed class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<IFilenameParser, FilenameParser>();
        serviceCollection.AddSingleton<IInboxService, InboxService>();
        serviceCollection.AddSingleton<IMediaProbe, JellyfinMediaProbe>();
        serviceCollection.AddSingleton<IInboxMediaInfoService, InboxMediaInfoService>();
        serviceCollection.AddSingleton<IMetadataSearchService, MetadataSearchService>();
        serviceCollection.AddSingleton<IPluginConfigurationAccessor, PluginConfigurationAccessor>();
        serviceCollection.AddSingleton<INamingService, NamingService>();
        serviceCollection.AddSingleton<INfoService, NfoService>();
        serviceCollection.AddSingleton<IImportPathValidator, ImportPathValidator>();
        serviceCollection.AddSingleton<IPathGuard, PathGuard>();
        serviceCollection.AddSingleton<ILibraryScanService, JellyfinLibraryScanService>();
        serviceCollection.AddSingleton<IImportService, ImportService>();
    }
}
