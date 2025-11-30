using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.Polyglot.Models;
using Jellyfin.Plugin.Polyglot.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Polyglot.Helpers;

/// <summary>
/// Extension methods for ILogger that also log to the Polyglot debug buffer.
/// Supports privacy-aware logging with entity references that can be rendered
/// differently based on debug report settings.
/// </summary>
public static class PolyglotLoggerExtensions
{
    /// <summary>
    /// Logs a debug message and captures it in the debug buffer.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The message template with {0}, {1}, etc. placeholders.</param>
    /// <param name="args">Arguments for the message. ILogEntity instances will be handled with privacy awareness.</param>
    public static void PolyglotDebug(this ILogger logger, string message, params object?[] args)
    {
        var (renderedMessage, entities) = ProcessArgs(message, args);
        logger.LogDebug(message, args.Select(RenderArgForStdout).ToArray());
        LogToBuffer("Debug", message, renderedMessage, entities, null);
    }

    /// <summary>
    /// Logs an information message and captures it in the debug buffer.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The message template with {0}, {1}, etc. placeholders.</param>
    /// <param name="args">Arguments for the message. ILogEntity instances will be handled with privacy awareness.</param>
    public static void PolyglotInfo(this ILogger logger, string message, params object?[] args)
    {
        var (renderedMessage, entities) = ProcessArgs(message, args);
        logger.LogInformation(message, args.Select(RenderArgForStdout).ToArray());
        LogToBuffer("Information", message, renderedMessage, entities, null);
    }

    /// <summary>
    /// Logs a warning message and captures it in the debug buffer.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The message template with {0}, {1}, etc. placeholders.</param>
    /// <param name="args">Arguments for the message. ILogEntity instances will be handled with privacy awareness.</param>
    public static void PolyglotWarning(this ILogger logger, string message, params object?[] args)
    {
        var (renderedMessage, entities) = ProcessArgs(message, args);
        logger.LogWarning(message, args.Select(RenderArgForStdout).ToArray());
        LogToBuffer("Warning", message, renderedMessage, entities, null);
    }

    /// <summary>
    /// Logs a warning message with exception and captures it in the debug buffer.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message template with {0}, {1}, etc. placeholders.</param>
    /// <param name="args">Arguments for the message. ILogEntity instances will be handled with privacy awareness.</param>
    public static void PolyglotWarning(this ILogger logger, Exception? exception, string message, params object?[] args)
    {
        var (renderedMessage, entities) = ProcessArgs(message, args);
        logger.LogWarning(exception, message, args.Select(RenderArgForStdout).ToArray());
        LogToBuffer("Warning", message, renderedMessage, entities, exception?.Message);
    }

    /// <summary>
    /// Logs an error message and captures it in the debug buffer.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="message">The message template with {0}, {1}, etc. placeholders.</param>
    /// <param name="args">Arguments for the message. ILogEntity instances will be handled with privacy awareness.</param>
    public static void PolyglotError(this ILogger logger, string message, params object?[] args)
    {
        var (renderedMessage, entities) = ProcessArgs(message, args);
        logger.LogError(message, args.Select(RenderArgForStdout).ToArray());
        LogToBuffer("Error", message, renderedMessage, entities, null);
    }

    /// <summary>
    /// Logs an error message with exception and captures it in the debug buffer.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message template with {0}, {1}, etc. placeholders.</param>
    /// <param name="args">Arguments for the message. ILogEntity instances will be handled with privacy awareness.</param>
    public static void PolyglotError(this ILogger logger, Exception? exception, string message, params object?[] args)
    {
        var (renderedMessage, entities) = ProcessArgs(message, args);
        logger.LogError(exception, message, args.Select(RenderArgForStdout).ToArray());
        LogToBuffer("Error", message, renderedMessage, entities, exception?.Message);
    }

    /// <summary>
    /// Processes arguments, extracting entities and rendering a full message for stdout.
    /// </summary>
    private static (string RenderedMessage, List<ILogEntity> Entities) ProcessArgs(string message, object?[] args)
    {
        var entities = new List<ILogEntity>();
        var renderedArgs = new object?[args.Length];

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is ILogEntity entity)
            {
                entities.Add(entity);
                renderedArgs[i] = entity.RenderFull();
            }
            else
            {
                // Wrap non-entity args to maintain position and count
                var valueEntity = new LogValue(arg);
                entities.Add(valueEntity);
                renderedArgs[i] = valueEntity.RenderFull();
            }
        }

        string renderedMessage;
        try
        {
            renderedMessage = args.Length > 0 ? string.Format(message, renderedArgs) : message;
        }
        catch
        {
            // If format fails, just use the template
            renderedMessage = message;
        }

        return (renderedMessage, entities);
    }

    /// <summary>
    /// Renders an argument for stdout (full details).
    /// </summary>
    private static object? RenderArgForStdout(object? arg)
    {
        return arg is ILogEntity entity ? entity.RenderFull() : arg;
    }

    /// <summary>
    /// Logs to the debug buffer with structured entity information.
    /// </summary>
    private static void LogToBuffer(string level, string messageTemplate, string renderedMessage, List<ILogEntity> entities, string? exception)
    {
        try
        {
            DebugReportService.LogToBufferStatic(level, messageTemplate, renderedMessage, entities, exception);
        }
        catch
        {
            // Don't let logging failures affect the application
        }
    }
}
