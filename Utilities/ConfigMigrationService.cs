using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities.Migrations;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Service for handling configuration loading with version migration support
    /// </summary>
    public class ConfigMigrationService : IConfigMigrationService
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IAppLogger? _logger;
        private readonly IConfigMigrationChain<ApplicationConfig> _appConfigMigrationChain;
        private readonly IConfigMigrationChain<UserPreferences> _userPrefsMigrationChain;

        /// <summary>
        /// Initializes a new instance of the ConfigMigrationService
        /// </summary>
        /// <param name="logger">Optional logger for configuration operations</param>
        public ConfigMigrationService(IAppLogger? logger = null)
        {
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(), new InterpolationConverter(), new BezierInterpolationConverter() }
            };

            // Initialize migration chains
            _appConfigMigrationChain = new ConfigMigrationChain<ApplicationConfig>(_logger);
            _userPrefsMigrationChain = new ConfigMigrationChain<UserPreferences>(_logger);

            // Register available migrations
            RegisterMigrations();
        }

        /// <summary>
        /// Registers all available migrations for both configuration types
        /// </summary>
        private void RegisterMigrations()
        {
            // Register migrations for ApplicationConfig
            _appConfigMigrationChain.RegisterMigration(new NoVersionToV1Migration());

            // Register migrations for UserPreferences  
            _userPrefsMigrationChain.RegisterMigration(new NoVersionToV1Migration());

            _logger?.Info("Registered migration chains - ApplicationConfig: {0} steps, UserPreferences: {1} steps",
                _appConfigMigrationChain.GetMigrationSteps().Count,
                _userPrefsMigrationChain.GetMigrationSteps().Count);
        }

        /// <summary>
        /// Gets the appropriate migration chain for the specified configuration type
        /// </summary>
        /// <typeparam name="T">The configuration type</typeparam>
        /// <returns>The migration chain for the configuration type</returns>
        /// <exception cref="NotSupportedException">Thrown when configuration type is not supported</exception>
        private IConfigMigrationChain<T> GetMigrationChain<T>() where T : class
        {
            if (typeof(T) == typeof(ApplicationConfig))
                return (IConfigMigrationChain<T>)_appConfigMigrationChain;

            if (typeof(T) == typeof(UserPreferences))
                return (IConfigMigrationChain<T>)_userPrefsMigrationChain;

            throw new NotSupportedException($"Migration chain not available for configuration type: {typeof(T).Name}");
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
            var configTypeName = typeof(T).Name;

            if (!File.Exists(filePath))
            {
                // File doesn't exist - create default
                _logger?.Info("Configuration file does not exist, creating default: {0}", filePath);
                var defaultConfig = defaultFactory();
                return new ConfigLoadResult<T>(defaultConfig, WasCreated: true, WasMigrated: false, OriginalVersion: GetCurrentVersion<T>());
            }

            try
            {
                // Probe version first
                var version = ProbeVersion(filePath);

                if (version == 0)
                {
                    // Version missing - this is a legacy file that needs migration to current version
                    _logger?.Info("Configuration file has no version, applying migration from v0 to current version: {0}", filePath);

                    var migrationChain = GetMigrationChain<T>();
                    var targetVersionForLegacy = GetCurrentVersion<T>();

                    if (migrationChain.CanMigrate(0, targetVersionForLegacy))
                    {
                        try
                        {
                            // Load as JsonDocument for migration
                            var jsonText = await File.ReadAllTextAsync(filePath);
                            using var jsonDoc = JsonDocument.Parse(jsonText);

                            // Apply migration chain
                            var migratedJson = migrationChain.ExecuteMigrationChain(jsonDoc, 0, targetVersionForLegacy);

                            // Deserialize migrated JSON
                            var config = JsonSerializer.Deserialize<T>(migratedJson, _jsonOptions);
                            if (config == null)
                                throw new InvalidOperationException("Failed to deserialize migrated configuration");

                            _logger?.Info("Successfully migrated {0} configuration from v0 to v{1}: {2}", configTypeName, targetVersionForLegacy, filePath);
                            return new ConfigLoadResult<T>(config, WasCreated: false, WasMigrated: true, OriginalVersion: 0);
                        }
                        catch (Exception migrationEx)
                        {
                            _logger?.ErrorWithException("Migration failed for {0}, recreating from defaults: {1}", migrationEx, configTypeName, filePath);
                            var defaultConfig = defaultFactory();
                            return new ConfigLoadResult<T>(defaultConfig, WasCreated: true, WasMigrated: false, OriginalVersion: 0);
                        }
                    }
                    else
                    {
                        // No migration path available - recreate from defaults
                        _logger?.Warning("No migration path available for {0} from v0 to v{1}, recreating from defaults: {2}", configTypeName, targetVersionForLegacy, filePath);
                        var defaultConfig = defaultFactory();
                        return new ConfigLoadResult<T>(defaultConfig, WasCreated: true, WasMigrated: false, OriginalVersion: 0);
                    }
                }

                var targetVersion = GetCurrentVersion<T>();

                if (version == targetVersion)
                {
                    // Current version - load directly
                    _logger?.Debug("Loading {0} configuration from current version {1}: {2}", configTypeName, version, filePath);
                    var config = await LoadConfigDirectlyAsync<T>(filePath);
                    return new ConfigLoadResult<T>(config, WasCreated: false, WasMigrated: false, OriginalVersion: version);
                }
                else if (version < targetVersion)
                {
                    // Old version - migration needed
                    _logger?.Info("Loading {0} configuration from older version {1} (current: {2}), migration will be applied: {3}",
                        configTypeName, version, targetVersion, filePath);

                    var migrationChain = GetMigrationChain<T>();

                    if (migrationChain.CanMigrate(version, targetVersion))
                    {
                        try
                        {
                            // Load as JsonDocument for migration
                            var jsonText = await File.ReadAllTextAsync(filePath);
                            using var jsonDoc = JsonDocument.Parse(jsonText);

                            // Apply migration chain
                            var migratedJson = migrationChain.ExecuteMigrationChain(jsonDoc, version, targetVersion);

                            // Deserialize migrated JSON
                            var config = JsonSerializer.Deserialize<T>(migratedJson, _jsonOptions);
                            if (config == null)
                                throw new InvalidOperationException("Failed to deserialize migrated configuration");

                            _logger?.Info("Successfully migrated {0} configuration from v{1} to v{2}: {3}", configTypeName, version, targetVersion, filePath);
                            return new ConfigLoadResult<T>(config, WasCreated: false, WasMigrated: true, OriginalVersion: version);
                        }
                        catch (Exception migrationEx)
                        {
                            _logger?.ErrorWithException("Migration failed for {0} from v{1} to v{2}, recreating from defaults: {3}", migrationEx, configTypeName, version, targetVersion, filePath);
                            var defaultConfig = defaultFactory();
                            return new ConfigLoadResult<T>(defaultConfig, WasCreated: true, WasMigrated: false, OriginalVersion: version);
                        }
                    }
                    else
                    {
                        // No migration path available - recreate from defaults
                        _logger?.Warning("No migration path available for {0} from v{1} to v{2}, recreating from defaults: {3}", configTypeName, version, targetVersion, filePath);
                        var defaultConfig = defaultFactory();
                        return new ConfigLoadResult<T>(defaultConfig, WasCreated: true, WasMigrated: false, OriginalVersion: version);
                    }
                }
                else
                {
                    // Future version - this shouldn't happen, but treat as current version
                    _logger?.Warning("Configuration file has future version {0} (current: {1}), loading as current version: {2}",
                        version, targetVersion, filePath);
                    var config = await LoadConfigDirectlyAsync<T>(filePath);
                    return new ConfigLoadResult<T>(config, WasCreated: false, WasMigrated: false, OriginalVersion: version);
                }
            }
            catch (JsonException jsonEx)
            {
                // JSON parsing error - file is corrupted
                _logger?.Error("Configuration file '{0}' contains invalid JSON format and cannot be read. " +
                    "Creating a new configuration file with default values. " +
                    "Your previous settings have been lost. Error details: {1}", filePath, jsonEx.Message);
                var defaultConfig = defaultFactory();
                return new ConfigLoadResult<T>(defaultConfig, WasCreated: true, WasMigrated: false, OriginalVersion: 0);
            }
            catch (IOException ioEx)
            {
                // File system error
                _logger?.Error("Cannot read configuration file '{0}' due to an I/O error (file may be locked or corrupted). " +
                    "Creating a new configuration file with default values. " +
                    "Please check file permissions and disk space. Error details: {1}", filePath, ioEx.Message);
                var defaultConfig = defaultFactory();
                return new ConfigLoadResult<T>(defaultConfig, WasCreated: true, WasMigrated: false, OriginalVersion: 0);
            }
            catch (UnauthorizedAccessException accessEx)
            {
                // Permission error
                _logger?.Error("Access denied reading configuration file, recreating from defaults: {0}. Error: {1}", filePath, accessEx.Message);
                var defaultConfig = defaultFactory();
                return new ConfigLoadResult<T>(defaultConfig, WasCreated: true, WasMigrated: false, OriginalVersion: 0);
            }
            catch (Exception ex)
            {
                // Unexpected error - recreate from defaults
                _logger?.ErrorWithException("Unexpected error loading configuration file, recreating from defaults: {0}", ex, filePath);
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
                _logger?.Debug("Cannot probe version: file does not exist: {0}", filePath);
                return 0;
            }

            try
            {
                var jsonText = File.ReadAllText(filePath);

                if (string.IsNullOrWhiteSpace(jsonText))
                {
                    _logger?.Warning("Configuration file is empty: {0}", filePath);
                    return 0;
                }

                using var document = JsonDocument.Parse(jsonText);

                if (document.RootElement.TryGetProperty("Version", out var versionElement))
                {
                    if (versionElement.TryGetInt32(out var version))
                    {
                        _logger?.Debug("Probed version {0} from configuration file: {1}", version, filePath);
                        return version;
                    }
                    else
                    {
                        _logger?.Warning("Version property exists but is not a valid integer in configuration file: {0}", filePath);
                        return 0;
                    }
                }

                // No version property found
                _logger?.Debug("No Version property found in configuration file: {0}", filePath);
                return 0;
            }
            catch (JsonException jsonEx)
            {
                _logger?.Warning("Invalid JSON format while probing version from file: {0}. Error: {1}", filePath, jsonEx.Message);
                return 0;
            }
            catch (IOException ioEx)
            {
                _logger?.Warning("IO error while probing version from file: {0}. Error: {1}", filePath, ioEx.Message);
                return 0;
            }
            catch (Exception ex)
            {
                _logger?.Warning("Unexpected error while probing version from file: {0}. Error: {1}", filePath, ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// Loads configuration directly from file
        /// </summary>
        private async Task<T> LoadConfigDirectlyAsync<T>(string filePath) where T : class
        {
            try
            {
                using var fileStream = File.OpenRead(filePath);
                var config = await JsonSerializer.DeserializeAsync<T>(fileStream, _jsonOptions);

                if (config == null)
                {
                    throw new InvalidOperationException($"Deserialization returned null for configuration from {filePath}");
                }

                _logger?.Debug("Successfully loaded {0} configuration from: {1}", typeof(T).Name, filePath);
                return config;
            }
            catch (JsonException jsonEx)
            {
                _logger?.Error("JSON deserialization error loading configuration from: {0}. Error: {1}", filePath, jsonEx.Message);
                throw;
            }
            catch (IOException ioEx)
            {
                _logger?.Error("IO error loading configuration from: {0}. Error: {1}", filePath, ioEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger?.ErrorWithException("Unexpected error loading configuration from: {0}", ex, filePath);
                throw;
            }
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
