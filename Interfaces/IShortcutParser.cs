using System;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for parsing keyboard shortcut strings into console key combinations
    /// </summary>
    public interface IShortcutParser
    {
        /// <summary>
        /// Parses a shortcut string into console key and modifiers
        /// </summary>
        /// <param name="shortcutString">Shortcut string to parse (e.g., "Alt+T", "Ctrl+Alt+E", "F1")</param>
        /// <returns>Tuple containing the console key and modifiers, or null if parsing failed</returns>
        (ConsoleKey Key, ConsoleModifiers Modifiers)? ParseShortcut(string shortcutString);

        /// <summary>
        /// Formats a console key and modifiers combination into a readable string
        /// </summary>
        /// <param name="key">The console key</param>
        /// <param name="modifiers">The modifier keys</param>
        /// <returns>Formatted shortcut string (e.g., "Alt+T")</returns>
        string FormatShortcut(ConsoleKey key, ConsoleModifiers modifiers);

        /// <summary>
        /// Validates whether a shortcut string can be parsed successfully
        /// </summary>
        /// <param name="shortcutString">Shortcut string to validate</param>
        /// <returns>True if the shortcut string is valid and can be parsed</returns>
        bool IsValidShortcut(string shortcutString);
    }
}