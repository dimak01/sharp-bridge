using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Service for handling configuration loading with version migration support
    /// </summary>
    public class ConfigMigrationService : IConfigMigrationService
    {
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the ConfigMigrationService
        /// </summary>
        public ConfigMigrationService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(), new InterpolationConverter(), new BezierInterpolationConverter() }
            };
        }

        /// <summary>
        /// Loads configuration with migration support
        /// </summary>
        /// <typeparam name="T">The configuration type</typeparam>
        /// <param name="filePath">Path to the configuration file</param>
        /// <param name="defaultFactory">Factory function to create default configuration</param>
        /// <returns>Configuration load result with migration information</returns>
        public async Task<ConfigLoadResult<T>> LoadWithMigrationAsync<T>(string filePath, Func<T> defaultFactory) where T : class
        {
            if (!File.Exists(filePath))
            {
                // File doesn't exist - create default
                var defaultConfig = defaultFactory();
                return new ConfigLoadResult<T>(defaultConfig, WasCreated: true, WasMigrated: false, OriginalVersion: GetCurrentVersion<T>());
            }

            try
            {
                // Probe version first
                var version = ProbeVersion(filePath);

                if (version == 0)
                {
                    // Version missing or invalid - treat as broken, recreate from defaults
                    var defaultConfig = defaultFactory();
                    return new ConfigLoadResult<T>(defaultConfig, WasCreated: true, WasMigrated: false, OriginalVersion: 0);
                }

                var currentVersion = GetCurrentVersion<T>();

                if (version == currentVersion)
                {
                    // Current version - load directly
                    var config = await LoadConfigDirectlyAsync<T>(filePath);
                    return new ConfigLoadResult<T>(config, WasCreated: false, WasMigrated: false, OriginalVersion: version);
                }
                else if (version < currentVersion)
                {
                    // Old version - migration needed (for now, just load directly as current version)
                    // TODO: Implement actual migration in Phase 4
                    var config = await LoadConfigDirectlyAsync<T>(filePath);
                    return new ConfigLoadResult<T>(config, WasCreated: false, WasMigrated: true, OriginalVersion: version);
                }
                else
                {
                    // Future version - this shouldn't happen, but treat as current version
                    var config = await LoadConfigDirectlyAsync<T>(filePath);
                    return new ConfigLoadResult<T>(config, WasCreated: false, WasMigrated: false, OriginalVersion: version);
                }
            }
            catch (Exception)
            {
                // Error loading - recreate from defaults
                var defaultConfig = defaultFactory();
                return new ConfigLoadResult<T>(defaultConfig, WasCreated: true, WasMigrated: false, OriginalVersion: 0);
            }
        }

        /// <summary>
        /// Probes the version of a configuration file without fully loading it
        /// </summary>
        /// <param name="filePath">Path to the configuration file</param>
        /// <returns>Version number, or 0 if file doesn't exist or version cannot be determined</returns>
        public int ProbeVersion(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return 0;
            }

            try
            {
                var jsonText = File.ReadAllText(filePath);
                using var document = JsonDocument.Parse(jsonText);

                if (document.RootElement.TryGetProperty("Version", out var versionElement))
                {
                    if (versionElement.TryGetInt32(out var version))
                    {
                        return version;
                    }
                }

                // No version property found
                return 0;
            }
            catch
            {
                // Error reading or parsing file
                return 0;
            }
        }

        /// <summary>
        /// Loads configuration directly from file
        /// </summary>
        private async Task<T> LoadConfigDirectlyAsync<T>(string filePath) where T : class
        {
            using var fileStream = File.OpenRead(filePath);
            var config = await JsonSerializer.DeserializeAsync<T>(fileStream, _jsonOptions);

            if (config == null)
            {
                throw new InvalidOperationException($"Failed to deserialize configuration from {filePath}");
            }

            return config;
        }

        /// <summary>
        /// Gets the current version for a configuration type
        /// </summary>
        private static int GetCurrentVersion<T>() where T : class
        {
            var type = typeof(T);
            var field = type.GetField("CurrentVersion");

            if (field?.GetValue(null) is int version)
            {
                return version;
            }

            // Fallback to version 1 if CurrentVersion field not found
            return 1;
        }
    }
}
