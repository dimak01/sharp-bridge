using System;
using System.Collections.Generic;
using System.Linq;
using SharpBridge.Interfaces;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Implementation of IShortcutParser for parsing keyboard shortcut strings
    /// </summary>
    public class ShortcutParser : IShortcutParser
    {
        /// <summary>
        /// Parses a shortcut string into console key and modifiers
        /// </summary>
        /// <param name="shortcutString">Shortcut string to parse (e.g., "Alt+T", "Ctrl+Alt+E", "F1")</param>
        /// <returns>Tuple containing the console key and modifiers, or null if parsing failed</returns>
        public (ConsoleKey Key, ConsoleModifiers Modifiers)? ParseShortcut(string shortcutString)
        {
            if (string.IsNullOrWhiteSpace(shortcutString))
                return null;

            // Handle explicit disable keywords
            if (string.Equals(shortcutString, "None", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(shortcutString, "Disabled", StringComparison.OrdinalIgnoreCase))
                return null;

            try
            {
                var parts = shortcutString.Split('+').Select(p => p.Trim()).ToArray();

                if (parts.Length == 0)
                    return null;

                // Last part is the key, everything before are modifiers
                var keyPart = parts.Last();
                var modifierParts = parts.Take(parts.Length - 1);

                // Parse the key
                if (!TryParseConsoleKey(keyPart, out var consoleKey))
                    return null;

                // Parse modifiers
                var modifiers = ConsoleModifiers.None;
                foreach (var modifierPart in modifierParts)
                {
                    if (!TryParseModifier(modifierPart, out var modifier))
                        return null;

                    modifiers |= modifier;
                }

                return (consoleKey, modifiers);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Formats a console key and modifiers combination into a readable string
        /// </summary>
        /// <param name="key">The console key</param>
        /// <param name="modifiers">The modifier keys</param>
        /// <returns>Formatted shortcut string (e.g., "Alt+T")</returns>
        public string FormatShortcut(ConsoleKey key, ConsoleModifiers modifiers)
        {
            var parts = new List<string>();

            // Add modifiers in consistent order
            if ((modifiers & ConsoleModifiers.Control) != 0)
                parts.Add("Ctrl");
            if ((modifiers & ConsoleModifiers.Alt) != 0)
                parts.Add("Alt");
            if ((modifiers & ConsoleModifiers.Shift) != 0)
                parts.Add("Shift");

            // Add the key
            parts.Add(FormatConsoleKey(key));

            return string.Join("+", parts);
        }

        /// <summary>
        /// Validates whether a shortcut string can be parsed successfully
        /// </summary>
        /// <param name="shortcutString">Shortcut string to validate</param>
        /// <returns>True if the shortcut string is valid and can be parsed</returns>
        public bool IsValidShortcut(string shortcutString)
        {
            return ParseShortcut(shortcutString) != null;
        }

        /// <summary>
        /// Attempts to parse a string into a ConsoleKey
        /// </summary>
        private static bool TryParseConsoleKey(string keyString, out ConsoleKey consoleKey)
        {
            // Handle special key name mappings
            switch (keyString.ToUpper())
            {
                case "CTRL":
                case "CONTROL":
                    consoleKey = ConsoleKey.LeftWindows; // Invalid - modifiers shouldn't be keys
                    return false;
                case "ALT":
                    consoleKey = ConsoleKey.LeftWindows; // Invalid - modifiers shouldn't be keys  
                    return false;
                case "SHIFT":
                    consoleKey = ConsoleKey.LeftWindows; // Invalid - modifiers shouldn't be keys
                    return false;
                default:
                    return Enum.TryParse<ConsoleKey>(keyString, true, out consoleKey);
            }
        }

        /// <summary>
        /// Attempts to parse a string into a ConsoleModifiers value
        /// </summary>
        private static bool TryParseModifier(string modifierString, out ConsoleModifiers modifier)
        {
            switch (modifierString.ToUpper())
            {
                case "CTRL":
                case "CONTROL":
                    modifier = ConsoleModifiers.Control;
                    return true;
                case "ALT":
                    modifier = ConsoleModifiers.Alt;
                    return true;
                case "SHIFT":
                    modifier = ConsoleModifiers.Shift;
                    return true;
                default:
                    modifier = ConsoleModifiers.None;
                    return false;
            }
        }

        /// <summary>
        /// Formats a ConsoleKey into a readable string
        /// </summary>
        private static string FormatConsoleKey(ConsoleKey key)
        {
            // Handle special formatting for common keys
            return key switch
            {
                ConsoleKey.Spacebar => "Space",
                ConsoleKey.Enter => "Enter",
                ConsoleKey.Escape => "Esc",
                ConsoleKey.Tab => "Tab",
                ConsoleKey.Backspace => "Backspace",
                ConsoleKey.Delete => "Delete",
                ConsoleKey.Insert => "Insert",
                ConsoleKey.Home => "Home",
                ConsoleKey.End => "End",
                ConsoleKey.PageUp => "PageUp",
                ConsoleKey.PageDown => "PageDown",
                ConsoleKey.UpArrow => "Up",
                ConsoleKey.DownArrow => "Down",
                ConsoleKey.LeftArrow => "Left",
                ConsoleKey.RightArrow => "Right",
                _ => key.ToString()
            };
        }
    }
}