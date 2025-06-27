using System;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for parsing keyboard shortcut strings into Shortcut objects
    /// </summary>
    public interface IShortcutParser
    {
        /// <summary>
        /// Parses a shortcut string into a Shortcut object
        /// </summary>
        /// <param name="shortcutString">Shortcut string to parse (e.g., "Alt+T", "Ctrl+Alt+E", "F1")</param>
        /// <returns>Shortcut object, or null if parsing failed</returns>
        Shortcut? ParseShortcut(string shortcutString);

        /// <summary>
        /// Formats a Shortcut object into a readable string
        /// </summary>
        /// <param name="shortcut">The shortcut to format</param>
        /// <returns>Formatted shortcut string (e.g., "Alt+T")</returns>
        string FormatShortcut(Shortcut shortcut);

        /// <summary>
        /// Validates whether a shortcut string can be parsed successfully
        /// </summary>
        /// <param name="shortcutString">Shortcut string to validate</param>
        /// <returns>True if the shortcut string is valid and can be parsed</returns>
        bool IsValidShortcut(string shortcutString);
    }
}