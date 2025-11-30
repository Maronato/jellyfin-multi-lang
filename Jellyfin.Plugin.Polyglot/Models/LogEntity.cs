using System;

namespace Jellyfin.Plugin.Polyglot.Models;

/// <summary>
/// Base interface for entities that can be referenced in logs.
/// Entities store snapshots of data at log time for privacy-aware rendering.
/// </summary>
public interface ILogEntity
{
    /// <summary>
    /// Gets the entity type for categorization.
    /// </summary>
    LogEntityType EntityType { get; }

    /// <summary>
    /// Renders the entity as a string for stdout (full details, no privacy).
    /// </summary>
    /// <returns>Full detailed string representation.</returns>
    string RenderFull();

    /// <summary>
    /// Renders the entity as a privacy-aware string for debug reports.
    /// </summary>
    /// <param name="index">The index to use for anonymization (e.g., User_1, Library_2).</param>
    /// <returns>Privacy-aware string representation.</returns>
    string RenderPrivate(int index);
}

/// <summary>
/// Types of loggable entities for tracking and consistent privacy handling.
/// </summary>
public enum LogEntityType
{
    /// <summary>
    /// A Jellyfin user.
    /// </summary>
    User,

    /// <summary>
    /// A Jellyfin library (source or mirror).
    /// </summary>
    Library,

    /// <summary>
    /// A language alternative configuration.
    /// </summary>
    Alternative,

    /// <summary>
    /// A library mirror configuration.
    /// </summary>
    Mirror,

    /// <summary>
    /// A filesystem path.
    /// </summary>
    Path,

    /// <summary>
    /// A simple value wrapper (no privacy redaction).
    /// </summary>
    Value
}

/// <summary>
/// Represents a simple value wrapper for logging that doesn't require privacy redaction.
/// Used to maintain argument order and count when mixing entities and non-entities.
/// </summary>
public sealed class LogValue : ILogEntity
{
    private readonly string _value;

    /// <inheritdoc />
    public LogEntityType EntityType => LogEntityType.Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogValue"/> class.
    /// </summary>
    /// <param name="value">The value to log.</param>
    public LogValue(object? value)
    {
        _value = value?.ToString() ?? string.Empty;
    }

    /// <inheritdoc />
    public string RenderFull() => _value;

    /// <inheritdoc />
    public string RenderPrivate(int index) => _value;
}

/// <summary>
/// Represents a user entity reference for logging.
/// Stores a snapshot of user data at log time.
/// </summary>
public sealed class LogUser : ILogEntity
{
    /// <summary>
    /// Gets the user ID.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the username at the time of logging.
    /// </summary>
    public string Username { get; }

    /// <inheritdoc />
    public LogEntityType EntityType => LogEntityType.User;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogUser"/> class.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="username">The username.</param>
    public LogUser(Guid id, string username)
    {
        Id = id;
        Username = username ?? string.Empty;
    }

    /// <inheritdoc />
    public string RenderFull() => $"{Username} ({Id})";

    /// <inheritdoc />
    public string RenderPrivate(int index) => $"User_{index}";
}

/// <summary>
/// Represents a library entity reference for logging.
/// Stores a snapshot of library data at log time.
/// </summary>
public sealed class LogLibrary : ILogEntity
{
    /// <summary>
    /// Gets the library ID.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the library name at the time of logging.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets whether this is a mirror library.
    /// </summary>
    public bool IsMirror { get; }

    /// <inheritdoc />
    public LogEntityType EntityType => LogEntityType.Library;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogLibrary"/> class.
    /// </summary>
    /// <param name="id">The library ID.</param>
    /// <param name="name">The library name.</param>
    /// <param name="isMirror">Whether this is a mirror library.</param>
    public LogLibrary(Guid id, string name, bool isMirror = false)
    {
        Id = id;
        Name = name ?? string.Empty;
        IsMirror = isMirror;
    }

    /// <inheritdoc />
    public string RenderFull() => $"{Name} ({Id})";

    /// <inheritdoc />
    public string RenderPrivate(int index) => IsMirror ? $"Mirror_{index}" : $"Library_{index}";
}

/// <summary>
/// Represents a language alternative entity reference for logging.
/// Stores a snapshot of alternative data at log time.
/// </summary>
public sealed class LogAlternative : ILogEntity
{
    /// <summary>
    /// Gets the alternative ID.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the alternative name at the time of logging.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the language code.
    /// </summary>
    public string LanguageCode { get; }

    /// <inheritdoc />
    public LogEntityType EntityType => LogEntityType.Alternative;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogAlternative"/> class.
    /// </summary>
    /// <param name="id">The alternative ID.</param>
    /// <param name="name">The alternative name.</param>
    /// <param name="languageCode">The language code.</param>
    public LogAlternative(Guid id, string name, string languageCode)
    {
        Id = id;
        Name = name ?? string.Empty;
        LanguageCode = languageCode ?? string.Empty;
    }

    /// <inheritdoc />
    public string RenderFull() => $"{Name} ({LanguageCode}, {Id})";

    /// <inheritdoc />
    public string RenderPrivate(int index) => $"Alt_{index} ({LanguageCode})";
}

/// <summary>
/// Represents a mirror configuration entity reference for logging.
/// Stores a snapshot of mirror data at log time.
/// </summary>
public sealed class LogMirror : ILogEntity
{
    /// <summary>
    /// Gets the mirror ID.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the source library name at the time of logging.
    /// </summary>
    public string SourceLibraryName { get; }

    /// <summary>
    /// Gets the target library name at the time of logging.
    /// </summary>
    public string TargetLibraryName { get; }

    /// <inheritdoc />
    public LogEntityType EntityType => LogEntityType.Mirror;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogMirror"/> class.
    /// </summary>
    /// <param name="id">The mirror ID.</param>
    /// <param name="sourceLibraryName">The source library name.</param>
    /// <param name="targetLibraryName">The target library name.</param>
    public LogMirror(Guid id, string sourceLibraryName, string targetLibraryName)
    {
        Id = id;
        SourceLibraryName = sourceLibraryName ?? string.Empty;
        TargetLibraryName = targetLibraryName ?? string.Empty;
    }

    /// <inheritdoc />
    public string RenderFull() => $"{SourceLibraryName} -> {TargetLibraryName} ({Id})";

    /// <inheritdoc />
    public string RenderPrivate(int index) => $"Mirror_{index}";
}

/// <summary>
/// Represents a filesystem path entity reference for logging.
/// Stores a snapshot of path data at log time.
/// </summary>
public sealed class LogPath : ILogEntity
{
    /// <summary>
    /// Gets the full path.
    /// </summary>
    public string FullPath { get; }

    /// <summary>
    /// Gets the path type for context in anonymized output.
    /// </summary>
    public string PathType { get; }

    /// <inheritdoc />
    public LogEntityType EntityType => LogEntityType.Path;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogPath"/> class.
    /// </summary>
    /// <param name="fullPath">The full filesystem path.</param>
    /// <param name="pathType">The type of path for context (e.g., "source", "target", "file").</param>
    public LogPath(string fullPath, string pathType = "path")
    {
        FullPath = fullPath ?? string.Empty;
        PathType = pathType ?? "path";
    }

    /// <inheritdoc />
    public string RenderFull() => FullPath;

    /// <inheritdoc />
    public string RenderPrivate(int index) => $"[{PathType}_{index}]";
}

