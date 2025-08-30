using System.Text.Json;

namespace SharpBridge.Interfaces;

/// <summary>
/// Represents a single configuration migration step from one version to the next.
/// </summary>
/// <typeparam name="TFrom">The source configuration type</typeparam>
/// <typeparam name="TTo">The target configuration type</typeparam>
public interface IConfigMigration<TFrom, TTo>
    where TFrom : class
    where TTo : class
{
    /// <summary>
    /// The version this migration migrates from
    /// </summary>
    int FromVersion { get; }

    /// <summary>
    /// The version this migration migrates to
    /// </summary>
    int ToVersion { get; }

    /// <summary>
    /// Migrates configuration from the source version to the target version.
    /// This method should be idempotent and have no side effects.
    /// </summary>
    /// <param name="source">The source configuration object</param>
    /// <returns>The migrated configuration object</returns>
    TTo Migrate(TFrom source);
}

/// <summary>
/// Represents a JSON-based configuration migration that works with JsonDocument.
/// Useful for migrations where we don't have strongly-typed legacy DTOs.
/// </summary>
public interface IJsonConfigMigration
{
    /// <summary>
    /// The version this migration migrates from
    /// </summary>
    int FromVersion { get; }

    /// <summary>
    /// The version this migration migrates to
    /// </summary>
    int ToVersion { get; }

    /// <summary>
    /// Migrates configuration JSON from the source version to the target version.
    /// This method should be idempotent and have no side effects.
    /// </summary>
    /// <param name="sourceJson">The source JSON document</param>
    /// <returns>The migrated JSON as a string</returns>
    string Migrate(JsonDocument sourceJson);
}


