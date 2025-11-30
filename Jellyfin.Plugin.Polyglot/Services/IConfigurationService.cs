using System;
using Jellyfin.Plugin.Polyglot.Configuration;

namespace Jellyfin.Plugin.Polyglot.Services;

/// <summary>
/// Service for atomic configuration reads and updates.
/// <para>
/// All reads return JSON-deserialized copies of the configuration data.
/// Callers can safely read/iterate the returned objects without holding locks.
/// </para>
/// <para>
/// All writes use a snapshot pattern: the callback receives a deserialized copy,
/// mutates it, and returns true to save or false to discard. This prevents
/// race conditions and ensures atomic updates.
/// </para>
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Atomically reads from configuration.
    /// The selector receives a JSON-deserialized copy of the configuration,
    /// completely disconnected from the live config.
    /// </summary>
    /// <typeparam name="T">The type to return from the selector.</typeparam>
    /// <param name="selector">Function that extracts data from the configuration snapshot.</param>
    /// <returns>The value returned by the selector.</returns>
    /// <exception cref="InvalidOperationException">Thrown if plugin configuration is not available.</exception>
    T Read<T>(Func<PluginConfiguration, T> selector);

    /// <summary>
    /// Atomically modifies configuration. Changes are always saved.
    /// The mutation receives a JSON-deserialized copy which replaces the live config after mutation.
    /// </summary>
    /// <param name="mutation">Action that modifies the configuration snapshot.</param>
    void Update(Action<PluginConfiguration> mutation);

    /// <summary>
    /// Atomically modifies configuration with validation/abort capability.
    /// The mutation receives a JSON-deserialized copy. Return true to save changes,
    /// false to discard the snapshot (live config remains unchanged).
    /// </summary>
    /// <param name="mutation">Function that modifies the configuration snapshot and returns whether to save.</param>
    /// <returns>True if changes were saved, false if the mutation returned false or config was unavailable.</returns>
    bool Update(Func<PluginConfiguration, bool> mutation);
}
