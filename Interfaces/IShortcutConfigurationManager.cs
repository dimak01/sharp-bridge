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
        /// <returns>Dictionary mapping shortcut actions to their shortcuts (or null if disabled)</returns>
        Dictionary<ShortcutAction, Shortcut?> GetMappedShortcuts();

        /// <summary>
        /// Gets the original invalid shortcut strings for debugging purposes
        /// </summary>
        /// <returns>Dictionary mapping shortcut actions to their invalid configuration strings</returns>
        Dictionary<ShortcutAction, string> GetIncorrectShortcuts();

        /// <summary>
        /// Gets the display string for a shortcut action (e.g., "Alt+T", "None", "Ctrl+K (Invalid)")
        /// </summary>
        /// <param name="action">The shortcut action</param>
        /// <returns>Human-readable shortcut string for display</returns>
        string GetDisplayString(ShortcutAction action);

        /// <summary>
        /// Loads shortcut configuration from the provided application configuration
        /// </summary>
        /// <param name="config">Application configuration containing shortcut definitions</param>
        void LoadFromConfiguration(ApplicationConfig config);

        /// <summary>
        /// Gets the default shortcut mappings used when no configuration is provided
        /// </summary>
        /// <returns>Dictionary of default shortcut action to Shortcut object mappings</returns>
        Dictionary<ShortcutAction, Shortcut> GetDefaultShortcuts();

        /// <summary>
        /// Gets the status of a specific shortcut action
        /// </summary>
        /// <param name="action">The shortcut action to check</param>
        /// <returns>The current status of the shortcut</returns>
        ShortcutStatus GetShortcutStatus(ShortcutAction action);


    }
}