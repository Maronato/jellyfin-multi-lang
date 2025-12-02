using Jellyfin.Plugin.Polyglot.EventConsumers;
using Jellyfin.Plugin.Polyglot.Helpers;
using Jellyfin.Plugin.Polyglot.Services;
using Jellyfin.Plugin.Polyglot.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Polyglot;

/// <summary>
/// Registers plugin services with the dependency injection container.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <summary>
    /// Full type name for UserCreatedEventArgs (stable across Jellyfin versions).
    /// </summary>
    private const string UserCreatedEventArgsTypeName = "Jellyfin.Data.Events.Users.UserCreatedEventArgs";

    /// <summary>
    /// Full type name for UserDeletedEventArgs (stable across Jellyfin versions).
    /// </summary>
    private const string UserDeletedEventArgsTypeName = "Jellyfin.Data.Events.Users.UserDeletedEventArgs";

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Configuration service - must be registered first as other services depend on it
        serviceCollection.AddSingleton<IConfigurationService, ConfigurationService>();

        // Core services
        serviceCollection.AddSingleton<IMirrorService, MirrorService>();
        serviceCollection.AddSingleton<IUserLanguageService, UserLanguageService>();
        serviceCollection.AddSingleton<ILibraryAccessService, LibraryAccessService>();
        serviceCollection.AddSingleton<IDebugReportService, DebugReportService>();

        // User event handlers service (used by dynamic event consumers)
        serviceCollection.AddSingleton<IUserEventHandlers, UserEventHandlers>();

        // Event consumers - registered dynamically to avoid compile-time dependencies
        // on event args types that reference User (which moved in Jellyfin 10.11)
        DynamicEventConsumer.TryRegister(
            serviceCollection,
            UserCreatedEventArgsTypeName,
            typeof(IUserEventHandlers),
            nameof(IUserEventHandlers.HandleUserCreatedAsync));

        DynamicEventConsumer.TryRegister(
            serviceCollection,
            UserDeletedEventArgsTypeName,
            typeof(IUserEventHandlers),
            nameof(IUserEventHandlers.HandleUserDeletedAsync));

        // Hosted service for library change monitoring
        serviceCollection.AddHostedService<LibraryChangedConsumer>();

        // Scheduled tasks
        serviceCollection.AddSingleton<IScheduledTask, MirrorSyncTask>();
        serviceCollection.AddSingleton<IScheduledTask, UserLanguageSyncTask>();

        // Post-scan task - triggers mirror sync after library scans
        serviceCollection.AddSingleton<ILibraryPostScanTask, MirrorPostScanTask>();
    }
}
