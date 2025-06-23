using System;
using System.Collections.Generic;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for managing keyboard shortcut configurations with graceful degradation
    /// </summary>
    public interface IShortcutConfigurationManager
    {
        /// <summary>
        /// Gets the currently mapped shortcuts. Null values indicate disabled shortcuts.
        /// </summary>
        /// <returns>Dictionary mapping shortcut actions to their key combinations (or null if disabled)</returns>
        Dictionary<ShortcutAction, (ConsoleKey Key, ConsoleModifiers Modifiers)?> GetMappedShortcuts();

        /// <summary>
        /// Gets a list of configuration issues encountered during loading
        /// </summary>
        /// <returns>List of human-readable error messages for display in help system</returns>
        List<string> GetConfigurationIssues();

        /// <summary>
        /// Loads shortcut configuration from the provided application configuration
        /// </summary>
        /// <param name="config">Application configuration containing shortcut definitions</param>
        void LoadFromConfiguration(ApplicationConfig config);

        /// <summary>
        /// Gets the default shortcut mappings used when no configuration is provided
        /// </summary>
        /// <returns>Dictionary of default shortcut action to string mappings</returns>
        Dictionary<ShortcutAction, string> GetDefaultShortcuts();
    }
}