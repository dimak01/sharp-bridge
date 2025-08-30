using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using SharpBridge.Interfaces;

namespace SharpBridge.Utilities;

/// <summary>
/// Manages and executes a chain of configuration migrations.
/// </summary>
/// <typeparam name="T">The target configuration type</typeparam>
public class ConfigMigrationChain<T> : IConfigMigrationChain<T> where T : class
{
    private readonly List<IJsonConfigMigration> _migrations = new();
    private readonly IAppLogger? _logger;

    /// <summary>
    /// Initializes a new instance of the ConfigMigrationChain class.
    /// </summary>
    /// <param name="logger">Optional logger for migration operations</param>
    public ConfigMigrationChain(IAppLogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a JSON-based migration step in the chain.
    /// </summary>
    /// <param name="migration">The migration to register</param>
    /// <exception cref="ArgumentNullException">Thrown when migration is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when migration creates a gap or duplicate in the chain</exception>
    public void RegisterMigration(IJsonConfigMigration migration)
    {
        if (migration == null)
            throw new ArgumentNullException(nameof(migration));

        if (migration.FromVersion >= migration.ToVersion)
            throw new InvalidOperationException($"Migration from version {migration.FromVersion} to {migration.ToVersion} is invalid. ToVersion must be greater than FromVersion.");

        // Check for duplicate migration steps
        var existing = _migrations.FirstOrDefault(m => m.FromVersion == migration.FromVersion && m.ToVersion == migration.ToVersion);
        if (existing != null)
            throw new InvalidOperationException($"Migration from version {migration.FromVersion} to {migration.ToVersion} is already registered.");

        _migrations.Add(migration);
        _migrations.Sort((a, b) => a.FromVersion.CompareTo(b.FromVersion));

        _logger?.Debug("Registered migration: v{0} → v{1}", migration.FromVersion, migration.ToVersion);
    }

    /// <summary>
    /// Executes the migration chain to bring configuration from any version to the current version.
    /// </summary>
    /// <param name="sourceJson">The source JSON document</param>
    /// <param name="sourceVersion">The version of the source configuration</param>
    /// <param name="targetVersion">The target version to migrate to</param>
    /// <returns>The migrated JSON as a string</returns>
    /// <exception cref="ArgumentNullException">Thrown when sourceJson is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when migration path is not available</exception>
    public string ExecuteMigrationChain(JsonDocument sourceJson, int sourceVersion, int targetVersion)
    {
        if (sourceJson == null)
            throw new ArgumentNullException(nameof(sourceJson));

        if (sourceVersion == targetVersion)
        {
            _logger?.Debug("No migration needed: source and target versions are the same (v{0})", sourceVersion);
            return sourceJson.RootElement.GetRawText();
        }

        if (!CanMigrate(sourceVersion, targetVersion))
        {
            throw new InvalidOperationException($"No migration path available from version {sourceVersion} to {targetVersion}");
        }

        var currentJson = sourceJson.RootElement.GetRawText();
        var currentVersion = sourceVersion;

        _logger?.Info("Starting migration chain: v{0} → v{1}", sourceVersion, targetVersion);

        while (currentVersion < targetVersion)
        {
            var migration = _migrations.FirstOrDefault(m => m.FromVersion == currentVersion);
            if (migration == null)
            {
                throw new InvalidOperationException($"No migration available from version {currentVersion}");
            }

            try
            {
                _logger?.Debug("Applying migration: v{0} → v{1}", migration.FromVersion, migration.ToVersion);

                using var jsonDoc = JsonDocument.Parse(currentJson);
                currentJson = migration.Migrate(jsonDoc);
                currentVersion = migration.ToVersion;

                _logger?.Debug("Migration successful: v{0} → v{1}", migration.FromVersion, migration.ToVersion);
            }
            catch (Exception ex)
            {
                _logger?.ErrorWithException("Migration failed: v{0} → v{1}", ex, migration.FromVersion, migration.ToVersion);
                throw new InvalidOperationException($"Migration from version {migration.FromVersion} to {migration.ToVersion} failed: {ex.Message}", ex);
            }
        }

        _logger?.Info("Migration chain completed successfully: v{0} → v{1}", sourceVersion, targetVersion);
        return currentJson;
    }

    /// <summary>
    /// Checks if a migration path exists from source version to target version.
    /// </summary>
    /// <param name="sourceVersion">The source version</param>
    /// <param name="targetVersion">The target version</param>
    /// <returns>True if migration path exists, false otherwise</returns>
    public bool CanMigrate(int sourceVersion, int targetVersion)
    {
        if (sourceVersion == targetVersion)
            return true;

        if (sourceVersion > targetVersion)
            return false; // We only support forward migrations

        var currentVersion = sourceVersion;
        while (currentVersion < targetVersion)
        {
            var migration = _migrations.FirstOrDefault(m => m.FromVersion == currentVersion);
            if (migration == null)
                return false;

            currentVersion = migration.ToVersion;
        }

        return currentVersion == targetVersion;
    }

    /// <summary>
    /// Gets all available migration steps ordered by version.
    /// </summary>
    /// <returns>List of migration steps</returns>
    public IReadOnlyList<IJsonConfigMigration> GetMigrationSteps()
    {
        return _migrations.AsReadOnly();
    }
}
