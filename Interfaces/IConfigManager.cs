using System.Collections.Generic;
using System.Threading.Tasks;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for managing configuration file loading and saving
    /// </summary>
    public interface IConfigManager
    {
        /// <summary>
        /// Gets the path to the consolidated Application configuration file
        /// </summary>
        string ApplicationConfigPath { get; }

        /// <summary>
        /// Gets the path to the User Preferences file
        /// </summary>
        string UserPreferencesPath { get; }

        /// <summary>
        /// Loads the consolidated application configuration from file or creates a default one if it doesn't exist
        /// </summary>
        /// <returns>The consolidated application configuration</returns>
        Task<ApplicationConfig> LoadApplicationConfigAsync();

        /// <summary>
        /// Saves the consolidated application configuration to file
        /// </summary>
        /// <param name="config">The application configuration to save</param>
        /// <returns>A task representing the asynchronous save operation</returns>
        Task SaveApplicationConfigAsync(ApplicationConfig config);

        /// <summary>
        /// Loads the PC configuration from the consolidated config
        /// </summary>
        /// <returns>The PC configuration</returns>
        Task<VTubeStudioPCConfig> LoadPCConfigAsync();

        /// <summary>
        /// Loads the Phone configuration from the consolidated config
        /// </summary>
        /// <returns>The Phone configuration</returns>
        Task<VTubeStudioPhoneClientConfig> LoadPhoneConfigAsync();

        /// <summary>
        /// Loads the GeneralSettings configuration from the consolidated config
        /// </summary>
        /// <returns>The GeneralSettings configuration</returns>
        Task<GeneralSettingsConfig> LoadGeneralSettingsConfigAsync();

        /// <summary>
        /// Loads the TransformationEngine configuration from the consolidated config
        /// </summary>
        /// <returns>The TransformationEngine configuration</returns>
        Task<TransformationEngineConfig> LoadTransformationConfigAsync();

        /// <summary>
        /// Loads user preferences from file or creates default ones if the file doesn't exist
        /// </summary>
        /// <returns>The user preferences</returns>
        Task<UserPreferences> LoadUserPreferencesAsync();

        /// <summary>
        /// Saves user preferences to file
        /// </summary>
        /// <param name="preferences">The preferences to save</param>
        /// <returns>A task representing the asynchronous save operation</returns>
        Task SaveUserPreferencesAsync(UserPreferences preferences);

        /// <summary>
        /// Resets user preferences to defaults by deleting the file and recreating it
        /// </summary>
        /// <returns>A task representing the asynchronous reset operation</returns>
        Task ResetUserPreferencesAsync();

        /// <summary>
        /// Saves the PC configuration to file (for API symmetry - unused in production)
        /// </summary>
        /// <param name="config">The configuration to save</param>
        /// <returns>A task representing the asynchronous save operation</returns>
        Task SavePCConfigAsync(VTubeStudioPCConfig config);

        /// <summary>
        /// Saves the Phone configuration to file (for API symmetry - unused in production)
        /// </summary>
        /// <param name="config">The configuration to save</param>
        /// <returns>A task representing the asynchronous save operation</returns>
        Task SavePhoneConfigAsync(VTubeStudioPhoneClientConfig config);

        /// <summary>
        /// Saves the GeneralSettings configuration to file (for API symmetry - unused in production)
        /// </summary>
        /// <param name="config">The configuration to save</param>
        /// <returns>A task representing the asynchronous save operation</returns>
        Task SaveGeneralSettingsConfigAsync(GeneralSettingsConfig config);

        /// <summary>
        /// Saves the TransformationEngine configuration to file (for API symmetry - unused in production)
        /// </summary>
        /// <param name="config">The configuration to save</param>
        /// <returns>A task representing the asynchronous save operation</returns>
        Task SaveTransformationConfigAsync(TransformationEngineConfig config);



        /// <summary>
        /// Loads a configuration section using the enum type identifier
        /// </summary>
        /// <param name="sectionType">The type of configuration section to load</param>
        /// <returns>The loaded configuration section</returns>
        Task<IConfigSection> LoadSectionAsync(ConfigSectionTypes sectionType);

        /// <summary>
        /// Saves a configuration section using the enum type identifier
        /// </summary>
        /// <param name="sectionType">The type of configuration section to save</param>
        /// <param name="config">The configuration section to save</param>
        /// <returns>A task representing the asynchronous save operation</returns>
        Task SaveSectionAsync(ConfigSectionTypes sectionType, IConfigSection config);

        /// <summary>
        /// Gets the raw field states for a configuration section using the enum type identifier
        /// </summary>
        /// <param name="sectionType">The type of configuration section</param>
        /// <returns>List of field states for validation purposes</returns>
        Task<List<ConfigFieldState>> GetSectionFieldsAsync(ConfigSectionTypes sectionType);
    }
}