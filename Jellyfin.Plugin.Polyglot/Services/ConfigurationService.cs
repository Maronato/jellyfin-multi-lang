using System;
using System.Text.Json;
using Jellyfin.Plugin.Polyglot.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Polyglot.Services;

/// <summary>
/// Service for atomic configuration reads and updates.
/// Uses JSON serialization for deep cloning to ensure complete isolation between
/// the live configuration and any snapshots passed to callbacks.
/// </summary>
/// <remarks>
/// This service is registered as a singleton, so the instance lock is sufficient.
/// Using an instance lock instead of static lock allows proper isolation in test scenarios
/// and prevents issues if Jellyfin ever reloads plugins or creates multiple service contexts.
/// </remarks>
public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly object _configLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
    }

    private static PluginConfiguration? Clone(PluginConfiguration? config)
    {
        if (config == null) return null;
        var json = JsonSerializer.Serialize(config);
        return JsonSerializer.Deserialize<PluginConfiguration>(json);
    }

    /// <inheritdoc />
    public T Read<T>(Func<PluginConfiguration, T> selector)
    {
        lock (_configLock)
        {
            var snapshot = Clone(Plugin.Instance?.Configuration)
                ?? throw new InvalidOperationException("Plugin configuration is not available");
            return selector(snapshot);
        }
    }

    /// <inheritdoc />
    public void Update(Action<PluginConfiguration> mutation)
    {
        Update(snapshot =>
        {
            mutation(snapshot);
            return true;
        });
    }

    /// <inheritdoc />
    public bool Update(Func<PluginConfiguration, bool> mutation)
    {
        lock (_configLock)
        {
            var snapshot = Clone(Plugin.Instance?.Configuration);
            if (snapshot == null)
            {
                _logger.LogWarning("Update: Plugin configuration is null");
                return false;
            }

            if (!mutation(snapshot))
            {
                return false;
            }

            // Clone again to break any references from objects added during mutation
            var toSave = Clone(snapshot)!;
            Plugin.Instance?.UpdateConfiguration(toSave);
            return true;
        }
    }
}
