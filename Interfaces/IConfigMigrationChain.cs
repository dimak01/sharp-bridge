using System.Collections.Generic;
using System.Text.Json;

namespace SharpBridge.Interfaces;

/// <summary>
/// Manages and executes a chain of configuration migrations.
/// </summary>
/// <typeparam name="T">The target configuration type</typeparam>
public interface IConfigMigrationChain<T> where T : class
{
    /// <summary>
    /// Registers a JSON-based migration step in the chain.
    /// </summary>
    /// <param name="migration">The migration to register</param>
    void RegisterMigration(IJsonConfigMigration migration);

    /// <summary>
    /// Executes the migration chain to bring configuration from any version to the current version.
    /// </summary>
    /// <param name="sourceJson">The source JSON document</param>
    /// <param name="sourceVersion">The version of the source configuration</param>
    /// <param name="targetVersion">The target version to migrate to</param>
    /// <returns>The migrated JSON as a string</returns>
    /// <exception cref="InvalidOperationException">Thrown when migration path is not available</exception>
    string ExecuteMigrationChain(JsonDocument sourceJson, int sourceVersion, int targetVersion);

    /// <summary>
    /// Checks if a migration path exists from source version to target version.
    /// </summary>
    /// <param name="sourceVersion">The source version</param>
    /// <param name="targetVersion">The target version</param>
    /// <returns>True if migration path exists, false otherwise</returns>
    bool CanMigrate(int sourceVersion, int targetVersion);

    /// <summary>
    /// Gets all available migration steps ordered by version.
    /// </summary>
    /// <returns>List of migration steps</returns>
    IReadOnlyList<IJsonConfigMigration> GetMigrationSteps();
}
