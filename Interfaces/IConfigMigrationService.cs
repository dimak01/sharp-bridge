using System;
using System.Threading.Tasks;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Service for handling configuration loading with version migration support
    /// </summary>
    public interface IConfigMigrationService
    {
        /// <summary>
        /// Loads configuration with migration support
        /// </summary>
        /// <typeparam name="T">The configuration type</typeparam>
        /// <param name="filePath">Path to the configuration file</param>
        /// <param name="defaultFactory">Factory function to create default configuration</param>
        /// <returns>Configuration load result with migration information</returns>
        Task<ConfigLoadResult<T>> LoadWithMigrationAsync<T>(string filePath, Func<T> defaultFactory) where T : class;

        /// <summary>
        /// Probes the version of a configuration file without fully loading it
        /// </summary>
        /// <param name="filePath">Path to the configuration file</param>
        /// <returns>Version number, or 0 if file doesn't exist or version cannot be determined</returns>
        int ProbeVersion(string filePath);
    }
}
