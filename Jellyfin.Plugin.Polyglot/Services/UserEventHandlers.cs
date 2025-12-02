using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Polyglot.Helpers;
using Jellyfin.Plugin.Polyglot.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Polyglot.Services;

/// <summary>
/// Interface for handling user-related events.
/// </summary>
public interface IUserEventHandlers
{
    /// <summary>
    /// Handles user created events.
    /// </summary>
    /// <param name="eventArgs">The event args (as object to avoid compile-time type dependency).</param>
    /// <returns>A task representing the async operation.</returns>
    Task HandleUserCreatedAsync(object eventArgs);

    /// <summary>
    /// Handles user deleted events.
    /// </summary>
    /// <param name="eventArgs">The event args (as object to avoid compile-time type dependency).</param>
    /// <returns>A task representing the async operation.</returns>
    Task HandleUserDeletedAsync(object eventArgs);
}

/// <summary>
/// Handles user creation and deletion events.
/// Uses reflection to access event args properties to avoid compile-time dependencies
/// on types that moved between Jellyfin versions.
/// </summary>
public class UserEventHandlers : IUserEventHandlers
{
    private readonly IUserLanguageService _userLanguageService;
    private readonly IConfigurationService _configService;
    private readonly ILogger<UserEventHandlers> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserEventHandlers"/> class.
    /// </summary>
    public UserEventHandlers(
        IUserLanguageService userLanguageService,
        IConfigurationService configService,
        ILogger<UserEventHandlers> logger)
    {
        _userLanguageService = userLanguageService;
        _configService = configService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleUserCreatedAsync(object eventArgs)
    {
        // Extract User from eventArgs.Argument using reflection
        var userObj = GetArgumentFromEventArgs(eventArgs);
        if (userObj == null)
        {
            _logger.LogWarning("UserEventHandlers: Could not extract user from event args");
            return;
        }

        var user = new PolyglotUser(userObj);
        var userEntity = new LogUser(user.Id, user.Username);
        _logger.PolyglotInfo("UserCreatedHandler: User created: {0}", userEntity);

        // Get config values in one atomic read
        var (autoManageNewUsers, defaultLanguageAlternativeId) = _configService.Read(c =>
            (c.AutoManageNewUsers, c.DefaultLanguageAlternativeId));

        if (autoManageNewUsers)
        {
            try
            {
                await _userLanguageService.AssignLanguageAsync(
                    user.Id,
                    defaultLanguageAlternativeId,
                    "auto",
                    manuallySet: false,
                    isPluginManaged: true,
                    CancellationToken.None).ConfigureAwait(false);

                // Get language alternative from fresh config lookup
                if (defaultLanguageAlternativeId.HasValue)
                {
                    var alt = _configService.Read(c => c.LanguageAlternatives.FirstOrDefault(a => a.Id == defaultLanguageAlternativeId.Value));
                    if (alt != null)
                    {
                        _logger.PolyglotInfo(
                            "UserCreatedHandler: Auto-assigned {0} to new user {1}",
                            new LogAlternative(alt.Id, alt.Name, alt.LanguageCode),
                            userEntity);
                    }
                    else
                    {
                        _logger.PolyglotInfo(
                            "UserCreatedHandler: Auto-assigned default language to new user {0}",
                            userEntity);
                    }
                }
                else
                {
                    _logger.PolyglotInfo(
                        "UserCreatedHandler: Auto-assigned default libraries to new user {0}",
                        userEntity);
                }
            }
            catch (Exception ex)
            {
                _logger.PolyglotError(ex, "UserCreatedHandler: Failed to auto-assign for user {0}", userEntity);
            }
        }
    }

    /// <inheritdoc />
    public Task HandleUserDeletedAsync(object eventArgs)
    {
        // Extract User from eventArgs.Argument using reflection
        var userObj = GetArgumentFromEventArgs(eventArgs);
        if (userObj == null)
        {
            _logger.LogWarning("UserEventHandlers: Could not extract user from event args");
            return Task.CompletedTask;
        }

        var user = new PolyglotUser(userObj);
        var userEntity = new LogUser(user.Id, user.Username);
        _logger.PolyglotInfo("UserDeletedHandler: User deleted: {0}", userEntity);

        // Remove user language assignment
        _userLanguageService.RemoveUser(user.Id);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Extracts the Argument property from GenericEventArgs via reflection.
    /// </summary>
    private static object? GetArgumentFromEventArgs(object eventArgs)
    {
        if (eventArgs == null)
        {
            return null;
        }

        var argumentProperty = eventArgs.GetType().GetProperty("Argument");
        return argumentProperty?.GetValue(eventArgs);
    }
}

