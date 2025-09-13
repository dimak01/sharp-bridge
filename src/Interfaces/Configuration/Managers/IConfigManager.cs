// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Threading.Tasks;
using SharpBridge.Models.Configuration;

namespace SharpBridge.Interfaces.Configuration.Managers
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
        /// Loads a configuration section using the enum type identifier
        /// </summary>
        /// <typeparam name="T">The type of configuration section to load</typeparam>
        /// <returns>The loaded configuration section</returns>
        Task<T> LoadSectionAsync<T>() where T : IConfigSection;

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
        Task SaveSectionAsync<T>(ConfigSectionTypes sectionType, T config) where T : IConfigSection;

        /// <summary>
        /// Gets the raw field states for a configuration section using the enum type identifier
        /// </summary>
        /// <param name="sectionType">The type of configuration section</param>
        /// <returns>List of field states for validation purposes</returns>
        Task<List<ConfigFieldState>> GetSectionFieldsAsync(ConfigSectionTypes sectionType);
    }
}