using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SharpBridge.Infrastructure.Utilities;
using SharpBridge.Interfaces.Configuration;
using SharpBridge.Interfaces.Configuration.Factories;
using SharpBridge.Interfaces.Configuration.Managers;
using SharpBridge.Interfaces.Infrastructure.Services;
using SharpBridge.Models.Configuration;

namespace SharpBridge.Configuration.Managers
{
    /// <summary>
    /// Manages configuration file loading and saving.
    /// </summary>
    public class ConfigManager : IConfigManager
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _configDirectory;
        private readonly string _applicationConfigFilename = "ApplicationConfig.json";
        private readonly string _userPreferencesFilename = "UserPreferences.json";
        private readonly IAppLogger? _logger;
        private readonly IConfigSectionFieldExtractorsFactory _fieldExtractorsFactory;

        /// <summary>
        /// Initializes a new instance of the ConfigManager class.
        /// </summary>
        /// <param name="configDirectory">The directory where config files are stored</param>
        /// <param name="fieldExtractorsFactory">Factory for creating field extractors</param>
        /// <param name="logger">Optional logger for configuration operations</param>
        public ConfigManager(string configDirectory, IConfigSectionFieldExtractorsFactory fieldExtractorsFactory, IAppLogger? logger = null)
        {
            _configDirectory = configDirectory ?? throw new ArgumentNullException(nameof(configDirectory));
            _fieldExtractorsFactory = fieldExtractorsFactory ?? throw new ArgumentNullException(nameof(fieldExtractorsFactory));
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(), new InterpolationConverter(), new BezierInterpolationConverter() }
            };

            EnsureConfigDirectoryExists();
        }

        /// <summary>
        /// Gets the path to the consolidated Application configuration file.
        /// </summary>
        public string ApplicationConfigPath => Path.Combine(_configDirectory, _applicationConfigFilename);

        /// <summary>
        /// Gets the path to the User Preferences file.
        /// </summary>
        public string UserPreferencesPath => Path.Combine(_configDirectory, _userPreferencesFilename);

        /// <summary>
        /// Loads the consolidated application configuration from file.
        /// </summary>
        /// <returns>The consolidated application configuration.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the configuration file doesn't exist or is invalid.</exception>
        public async Task<ApplicationConfig> LoadApplicationConfigAsync()
        {
            _logger?.Debug("Loading ApplicationConfig from: {0}", ApplicationConfigPath);

            try
            {
                if (File.Exists(ApplicationConfigPath))
                {
                    var json = await File.ReadAllTextAsync(ApplicationConfigPath);
                    var config = JsonSerializer.Deserialize<ApplicationConfig>(json, _jsonOptions);
                    if (config != null)
                    {
                        _logger?.Debug("ApplicationConfig loaded successfully from: {0}", ApplicationConfigPath);
                        return config;
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger?.Warning("Failed to parse ApplicationConfig from {0}: {1}", ApplicationConfigPath, ex.Message);
            }
            catch (IOException ex)
            {
                _logger?.Warning("Failed to read ApplicationConfig from {0}: {1}", ApplicationConfigPath, ex.Message);
            }

            // Configuration file doesn't exist or is invalid - throw exception
            var errorMessage = File.Exists(ApplicationConfigPath)
                ? $"ApplicationConfig file exists but is invalid or corrupted: {ApplicationConfigPath}"
                : $"ApplicationConfig file not found: {ApplicationConfigPath}";

            _logger?.Error(errorMessage);
            throw new FileNotFoundException(errorMessage, ApplicationConfigPath);
        }

        /// <summary>
        /// Saves the consolidated application configuration to file
        /// </summary>
        /// <param name="config">The application configuration to save</param>
        /// <returns>A task representing the asynchronous save operation</returns>
        public async Task SaveApplicationConfigAsync(ApplicationConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            await SaveConfigAsync(ApplicationConfigPath, config);
        }

        /// <summary>
        /// Loads user preferences from file or creates default ones if the file doesn't exist.
        /// </summary>
        /// <returns>The user preferences.</returns>
        public async Task<UserPreferences> LoadUserPreferencesAsync()
        {
            _logger?.Debug("Loading UserPreferences from: {0}", UserPreferencesPath);

            try
            {
                if (File.Exists(UserPreferencesPath))
                {
                    var json = await File.ReadAllTextAsync(UserPreferencesPath);
                    var preferences = JsonSerializer.Deserialize<UserPreferences>(json, _jsonOptions);
                    if (preferences != null)
                    {
                        _logger?.Debug("UserPreferences loaded successfully from: {0}", UserPreferencesPath);
                        return preferences;
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger?.Warning("Failed to parse UserPreferences from {0}: {1}", UserPreferencesPath, ex.Message);
            }
            catch (IOException ex)
            {
                _logger?.Warning("Failed to read UserPreferences from {0}: {1}", UserPreferencesPath, ex.Message);
            }

            // Create default preferences if file doesn't exist or is invalid
            _logger?.Info("Creating default UserPreferences");
            var defaultPreferences = new UserPreferences();
            await SaveUserPreferencesAsync(defaultPreferences);
            return defaultPreferences;
        }

        /// <summary>
        /// Saves user preferences to file.
        /// </summary>
        /// <param name="preferences">The preferences to save.</param>
        /// <returns>A task representing the asynchronous save operation.</returns>
        public async Task SaveUserPreferencesAsync(UserPreferences preferences)
        {
            await SaveConfigAsync(UserPreferencesPath, preferences);
        }

        /// <summary>
        /// Resets user preferences to defaults by deleting the file and recreating it.
        /// </summary>
        /// <returns>A task representing the asynchronous reset operation.</returns>
        public async Task ResetUserPreferencesAsync()
        {
            if (File.Exists(UserPreferencesPath))
            {
                File.Delete(UserPreferencesPath);
            }

            var defaultPreferences = new UserPreferences();
            await SaveUserPreferencesAsync(defaultPreferences);
        }

        private async Task SaveConfigAsync<T>(string path, T config) where T : class
        {
            try
            {
                _logger?.Debug("Saving {0} configuration to: {1}", typeof(T).Name, path);

                // Create a backup if the file already exists
                var backupPath = path + ".backup";
                if (File.Exists(path))
                {
                    File.Copy(path, backupPath, overwrite: true);
                    _logger?.Debug("Created backup of existing configuration: {0}", backupPath);
                }

                using var fileStream = File.Create(path);
                await JsonSerializer.SerializeAsync(fileStream, config, _jsonOptions);

                _logger?.Info("Successfully saved {0} configuration to: {1}", typeof(T).Name, path);

                // Remove backup after successful save
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                    _logger?.Debug("Removed backup file after successful save: {0}", backupPath);
                }
            }
            catch (JsonException jsonEx)
            {
                _logger?.ErrorWithException("JSON serialization error saving configuration to: {0}", jsonEx, path);
                throw new InvalidOperationException($"JSON serialization error saving configuration to {path}: {jsonEx.Message}", jsonEx);
            }
            catch (IOException ioEx)
            {
                _logger?.ErrorWithException("IO error saving configuration to: {0}", ioEx, path);
                throw new InvalidOperationException($"IO error saving configuration to {path}: {ioEx.Message}", ioEx);
            }
            catch (UnauthorizedAccessException accessEx)
            {
                _logger?.ErrorWithException("Access denied saving configuration to: {0}", accessEx, path);
                throw new InvalidOperationException($"Access denied saving configuration to {path}: {accessEx.Message}", accessEx);
            }
            catch (Exception ex)
            {
                _logger?.ErrorWithException("Unexpected error saving configuration to: {0}", ex, path);
                throw new InvalidOperationException($"Error saving configuration to {path}: {ex.Message}", ex);
            }
        }

        private void EnsureConfigDirectoryExists()
        {
            if (!Directory.Exists(_configDirectory))
            {
                _logger?.Info("Creating configuration directory: {0}", _configDirectory);
                Directory.CreateDirectory(_configDirectory);
            }
        }

        /// <summary>
        /// Loads a configuration section using the enum type identifier
        /// </summary>
        /// <param name="sectionType">The type of configuration section to load</param>
        /// <returns>The loaded configuration section</returns>
        public async Task<IConfigSection> LoadSectionAsync(ConfigSectionTypes sectionType)
        {
            var appConfig = await LoadApplicationConfigAsync();
            return sectionType switch
            {
                ConfigSectionTypes.VTubeStudioPCConfig => appConfig.PCClient,
                ConfigSectionTypes.VTubeStudioPhoneClientConfig => appConfig.PhoneClient,
                ConfigSectionTypes.GeneralSettingsConfig => appConfig.GeneralSettings,
                ConfigSectionTypes.TransformationEngineConfig => appConfig.TransformationEngine,
                _ => throw new ArgumentException($"Unknown section type: {sectionType}", nameof(sectionType))
            };
        }

        /// <summary>
        /// Loads a configuration section using the type identifier
        /// </summary>
        /// <typeparam name="T">The type of configuration section to load</typeparam>
        /// <returns>The loaded configuration section</returns>
        public async Task<T> LoadSectionAsync<T>() where T : IConfigSection
        {
            var appConfig = await LoadApplicationConfigAsync();

            if (typeof(T) == typeof(VTubeStudioPCConfig))
                return (T)(object)appConfig.PCClient;
            else if (typeof(T) == typeof(VTubeStudioPhoneClientConfig))
                return (T)(object)appConfig.PhoneClient;
            else if (typeof(T) == typeof(GeneralSettingsConfig))
                return (T)(object)appConfig.GeneralSettings;
            else if (typeof(T) == typeof(TransformationEngineConfig))
                return (T)(object)appConfig.TransformationEngine;
            else
                throw new ArgumentException($"Unknown section type: {typeof(T)}");
        }

        /// <summary>
        /// Saves a configuration section using the enum type identifier
        /// Uses JSON path-based updates to preserve all other sections exactly as-is
        /// </summary>
        /// <param name="sectionType">The type of configuration section to save</param>
        /// <param name="config">The configuration section to save</param>
        /// <returns>A task representing the asynchronous save operation</returns>
        public async Task SaveSectionAsync<T>(ConfigSectionTypes sectionType, T config) where T : IConfigSection
        {
            try
            {
                _logger?.Debug("Saving {0} section using JSON path-based update", sectionType);

                // Get the JSON path for this section
                var sectionPath = GetSectionJsonPath(sectionType);

                // If config file doesn't exist, create an empty JSON container
                if (!File.Exists(ApplicationConfigPath))
                {
                    var newJson = "{}";
                    await File.WriteAllTextAsync(ApplicationConfigPath, newJson);
                }

                // Read the current JSON file
                var json = await File.ReadAllTextAsync(ApplicationConfigPath);

                // Update only the specific section using JSON path
                var updatedJson = UpdateJsonSection(json, sectionPath, config);

                // Create backup before writing
                var backupPath = ApplicationConfigPath + ".backup";
                if (File.Exists(ApplicationConfigPath))
                {
                    File.Copy(ApplicationConfigPath, backupPath, overwrite: true);
                }

                // Write the updated JSON
                await File.WriteAllTextAsync(ApplicationConfigPath, updatedJson);

                _logger?.Debug("Successfully saved {0} section", sectionType);
            }
            catch (Exception ex)
            {
                _logger?.Error("Failed to save {0} section using JSON path method: {1}", sectionType, ex.Message);
            }
        }

        /// <summary>
        /// Gets the JSON path for a configuration section
        /// </summary>
        /// <param name="sectionType">The type of configuration section</param>
        /// <returns>The JSON path string for the section</returns>
        private static string GetSectionJsonPath(ConfigSectionTypes sectionType)
        {
            return sectionType switch
            {
                ConfigSectionTypes.VTubeStudioPCConfig => "PCClient",
                ConfigSectionTypes.VTubeStudioPhoneClientConfig => "PhoneClient",
                ConfigSectionTypes.GeneralSettingsConfig => "GeneralSettings",
                ConfigSectionTypes.TransformationEngineConfig => "TransformationEngine",
                _ => throw new ArgumentException($"Unknown section type: {sectionType}", nameof(sectionType))
            };
        }

        /// <summary>
        /// Updates a specific section in JSON using path-based replacement
        /// </summary>
        /// <param name="json">The original JSON string</param>
        /// <param name="sectionPath">The JSON path to the section (e.g., "PhoneClient")</param>
        /// <param name="config">The configuration object to serialize</param>
        /// <returns>The updated JSON string</returns>
        private string UpdateJsonSection(string json, string sectionPath, IConfigSection config)
        {
            try
            {
                // Parse the original JSON
                using var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;

                // Create a new JSON object with the updated section
                var updatedJson = new Dictionary<string, object>();

                // Copy all existing properties
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Name == sectionPath)
                    {
                        // Replace the target section with the new config
                        updatedJson[property.Name] = config;
                    }
                    else
                    {
                        // Preserve all other sections exactly as-is
                        updatedJson[property.Name] = JsonSerializer.Deserialize<object>(property.Value.GetRawText())!;
                    }
                }

                // If the section doesn't exist, add it
                if (!root.TryGetProperty(sectionPath, out _))
                {
                    updatedJson[sectionPath] = config;
                }

                // Serialize back to JSON with proper formatting
                return JsonSerializer.Serialize(updatedJson, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = null, // Don't change property names from original
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Less aggressive escaping
                });
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse or update JSON for section '{sectionPath}'", ex);
            }
        }

        /// <summary>
        /// Gets the raw field states for a configuration section using the enum type identifier
        /// </summary>
        /// <param name="sectionType">The type of configuration section</param>
        /// <returns>List of field states for validation purposes</returns>
        public async Task<List<ConfigFieldState>> GetSectionFieldsAsync(ConfigSectionTypes sectionType)
        {
            try
            {
                var extractor = _fieldExtractorsFactory.GetExtractor(sectionType);
                return await extractor.ExtractFieldStatesAsync(ApplicationConfigPath);
            }
            catch (Exception ex)
            {
                _logger?.ErrorWithException("Failed to extract field states for section {0}", ex, sectionType);
                throw new InvalidOperationException($"Failed to extract field states for section {sectionType}: {ex.Message}", ex);
            }
        }
    }
}