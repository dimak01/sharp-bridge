using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SharpBridge.Models;
using SharpBridge.Interfaces;

namespace SharpBridge.Utilities
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
        private readonly IConfigMigrationService _migrationService;

        /// <summary>
        /// Initializes a new instance of the ConfigManager class.
        /// </summary>
        /// <param name="configDirectory">The directory where config files are stored</param>
        /// <param name="migrationService">Service for handling configuration migration</param>
        public ConfigManager(string configDirectory, IConfigMigrationService migrationService)
        {
            _configDirectory = configDirectory ?? throw new ArgumentNullException(nameof(configDirectory));
            _migrationService = migrationService ?? throw new ArgumentNullException(nameof(migrationService));

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
        /// Loads the consolidated application configuration from file or creates a default one if it doesn't exist.
        /// </summary>
        /// <returns>The consolidated application configuration.</returns>
        public async Task<ApplicationConfig> LoadApplicationConfigAsync()
        {
            var result = await _migrationService.LoadWithMigrationAsync<ApplicationConfig>(
                ApplicationConfigPath,
                () => new ApplicationConfig());

            // For Phase 1, we'll save the config if it was created to maintain current behavior
            if (result.WasCreated)
            {
                await SaveConfigAsync(ApplicationConfigPath, result.Config);
            }

            return result.Config;
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
        /// Loads the PC configuration from the consolidated config.
        /// </summary>
        /// <returns>The PC configuration.</returns>
        public async Task<VTubeStudioPCConfig> LoadPCConfigAsync()
        {
            var appConfig = await LoadApplicationConfigAsync();
            return appConfig.PCClient;
        }

        /// <summary>
        /// Loads the Phone configuration from the consolidated config.
        /// </summary>
        /// <returns>The Phone configuration.</returns>
        public async Task<VTubeStudioPhoneClientConfig> LoadPhoneConfigAsync()
        {
            var appConfig = await LoadApplicationConfigAsync();
            return appConfig.PhoneClient;
        }

        /// <summary>
        /// Loads the GeneralSettings configuration from the consolidated config.
        /// </summary>
        /// <returns>The GeneralSettings configuration.</returns>
        public async Task<GeneralSettingsConfig> LoadGeneralSettingsConfigAsync()
        {
            var appConfig = await LoadApplicationConfigAsync();
            return appConfig.GeneralSettings;
        }

        /// <summary>
        /// Loads the TransformationEngine configuration from the consolidated config.
        /// </summary>
        /// <returns>The TransformationEngine configuration.</returns>
        public async Task<TransformationEngineConfig> LoadTransformationConfigAsync()
        {
            var appConfig = await LoadApplicationConfigAsync();
            return appConfig.TransformationEngine;
        }

        /// <summary>
        /// Loads user preferences from file or creates default ones if the file doesn't exist.
        /// </summary>
        /// <returns>The user preferences.</returns>
        public async Task<UserPreferences> LoadUserPreferencesAsync()
        {
            var result = await _migrationService.LoadWithMigrationAsync<UserPreferences>(
                UserPreferencesPath,
                () => new UserPreferences());

            // For Phase 1, we'll save the config if it was created to maintain current behavior
            if (result.WasCreated)
            {
                await SaveConfigAsync(UserPreferencesPath, result.Config);
            }

            return result.Config;
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

        /// <summary>
        /// Saves the PC configuration to file (for API symmetry - unused in production).
        /// </summary>
        /// <param name="config">The configuration to save.</param>
        /// <returns>A task representing the asynchronous save operation.</returns>
        public async Task SavePCConfigAsync(VTubeStudioPCConfig config)
        {
            // For API symmetry only - not used in production
            // In the future, this could update the consolidated config
            var appConfig = await LoadApplicationConfigAsync();
            appConfig.PCClient = config;
            await SaveConfigAsync(ApplicationConfigPath, appConfig);
        }

        /// <summary>
        /// Saves the Phone configuration to file (for API symmetry - unused in production).
        /// </summary>
        /// <param name="config">The configuration to save.</param>
        /// <returns>A task representing the asynchronous save operation.</returns>
        public async Task SavePhoneConfigAsync(VTubeStudioPhoneClientConfig config)
        {
            // For API symmetry only - not used in production
            // In the future, this could update the consolidated config
            var appConfig = await LoadApplicationConfigAsync();
            appConfig.PhoneClient = config;
            await SaveConfigAsync(ApplicationConfigPath, appConfig);
        }

        /// <summary>
        /// Saves the GeneralSettings configuration to file (for API symmetry - unused in production).
        /// </summary>
        /// <param name="config">The configuration to save.</param>
        /// <returns>A task representing the asynchronous save operation.</returns>
        public async Task SaveGeneralSettingsConfigAsync(GeneralSettingsConfig config)
        {
            // For API symmetry only - not used in production
            // In the future, this could update the consolidated config
            var appConfig = await LoadApplicationConfigAsync();
            appConfig.GeneralSettings = config;
            await SaveConfigAsync(ApplicationConfigPath, appConfig);
        }

        /// <summary>
        /// Saves the TransformationEngine configuration to file (for API symmetry - unused in production).
        /// </summary>
        /// <param name="config">The configuration to save.</param>
        /// <returns>A task representing the asynchronous save operation.</returns>
        public async Task SaveTransformationConfigAsync(TransformationEngineConfig config)
        {
            // For API symmetry only - not used in production
            // In the future, this could update the consolidated config
            var appConfig = await LoadApplicationConfigAsync();
            appConfig.TransformationEngine = config;
            await SaveConfigAsync(ApplicationConfigPath, appConfig);
        }



        private async Task SaveConfigAsync<T>(string path, T config) where T : class
        {
            try
            {
                using var fileStream = File.Create(path);
                await JsonSerializer.SerializeAsync(fileStream, config, _jsonOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error saving configuration to {path}: {ex.Message}", ex);
            }
        }

        private void EnsureConfigDirectoryExists()
        {
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }
        }
    }
}