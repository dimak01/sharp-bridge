using System;
using System.Collections.Generic;
using System.Linq;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.UI.Components;
using SharpBridge.Models;
using SharpBridge.Models.Domain;

namespace SharpBridge.UI.Components
{
    /// <summary>
    /// Implementation of IShortcutParser for parsing keyboard shortcut strings
    /// </summary>
    public class ShortcutParser : IShortcutParser
    {
        /// <summary>
        /// Parses a shortcut string into a Shortcut object
        /// </summary>
        /// <param name="shortcutString">Shortcut string to parse (e.g., "Alt+T", "Ctrl+Alt+E", "F1")</param>
        /// <returns>Shortcut object, or null if parsing failed</returns>
        public Shortcut? ParseShortcut(string shortcutString)
        {
            // Handle special cases FIRST before any other checks
            if (shortcutString == "+")
                return new Shortcut(ConsoleKey.OemPlus, ConsoleModifiers.None);
            if (shortcutString == " ")
                return new Shortcut(ConsoleKey.Spacebar, ConsoleModifiers.None);

            if (string.IsNullOrWhiteSpace(shortcutString))
                return null;

            // Handle explicit disable keywords
            if (string.Equals(shortcutString, "None", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(shortcutString, "Disabled", StringComparison.OrdinalIgnoreCase))
                return null;

            var parts = shortcutString.Split('+').Select(p => p.Trim()).ToArray();

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

            return new Shortcut(consoleKey, modifiers);
        }

        /// <summary>
        /// Formats a Shortcut object into a readable string
        /// </summary>
        /// <param name="shortcut">The shortcut to format</param>
        /// <returns>Formatted shortcut string (e.g., "Alt+T")</returns>
        public string FormatShortcut(Shortcut shortcut)
        {
            if (shortcut == null)
                throw new ArgumentNullException(nameof(shortcut));

            var parts = new List<string>();

            // Add modifiers in consistent order
            if ((shortcut.Modifiers & ConsoleModifiers.Control) != 0)
                parts.Add("Ctrl");
            if ((shortcut.Modifiers & ConsoleModifiers.Alt) != 0)
                parts.Add("Alt");
            if ((shortcut.Modifiers & ConsoleModifiers.Shift) != 0)
                parts.Add("Shift");

            // Add the key
            parts.Add(FormatConsoleKey(shortcut.Key));

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
            // Handle invalid modifier keys
            if (IsModifierKey(keyString))
            {
                consoleKey = default;
                return false;
            }

            // Handle user-friendly number keys (0-9 instead of D0-D9)
            if (TryParseDigitKey(keyString, out consoleKey))
                return true;

            // Try direct enum parsing for standard keys
            if (Enum.TryParse<ConsoleKey>(keyString, true, out consoleKey))
                return true;

            // Handle common symbols
            consoleKey = GetSymbolKey(keyString);
            return consoleKey != default;
        }

        /// <summary>
        /// Checks if the key string represents a modifier key
        /// </summary>
        private static bool IsModifierKey(string keyString)
        {
            var upperKey = keyString.ToUpper();
            return upperKey is "CTRL" or "CONTROL" or "ALT" or "SHIFT";
        }

        /// <summary>
        /// Attempts to parse a single digit character as a ConsoleKey
        /// </summary>
        private static bool TryParseDigitKey(string keyString, out ConsoleKey consoleKey)
        {
            if (keyString.Length == 1 && char.IsDigit(keyString[0]))
            {
                var digit = keyString[0] - '0';
                consoleKey = ConsoleKey.D0 + digit;
                return true;
            }

            consoleKey = default;
            return false;
        }

        /// <summary>
        /// Maps symbol strings to ConsoleKey values
        /// </summary>
        private static ConsoleKey GetSymbolKey(string symbol)
        {
            return _symbolKeyMap.TryGetValue(symbol, out var key) ? key : default;
        }

        /// <summary>
        /// Static dictionary mapping symbol strings to ConsoleKey values
        /// </summary>
        private static readonly Dictionary<string, ConsoleKey> _symbolKeyMap = new()
        {
            // Shifted symbols
            ["!"] = ConsoleKey.D1,      // Shift+1
            ["@"] = ConsoleKey.D2,      // Shift+2
            ["#"] = ConsoleKey.D3,      // Shift+3
            ["$"] = ConsoleKey.D4,      // Shift+4
            ["%"] = ConsoleKey.D5,      // Shift+5
            ["^"] = ConsoleKey.D6,      // Shift+6
            ["&"] = ConsoleKey.D7,      // Shift+7
            ["*"] = ConsoleKey.D8,      // Shift+8
            ["("] = ConsoleKey.D9,      // Shift+9
            [")"] = ConsoleKey.D0,      // Shift+0
            ["_"] = ConsoleKey.OemMinus,   // Shift+Minus
            ["+"] = ConsoleKey.OemPlus,    // Shift+Plus
            ["{"] = ConsoleKey.Oem4,       // Shift+[ (US layout)
            ["}"] = ConsoleKey.Oem6,       // Shift+] (US layout)
            ["|"] = ConsoleKey.Oem5,       // Shift+\ (US layout)
            [":"] = ConsoleKey.Oem1,       // Shift+; (US layout)
            ["\""] = ConsoleKey.Oem7,      // Shift+' (US layout)
            ["<"] = ConsoleKey.OemComma,   // Shift+,
            [">"] = ConsoleKey.OemPeriod,  // Shift+.
            ["?"] = ConsoleKey.Oem2,       // Shift+/ (US layout)
            ["~"] = ConsoleKey.Oem3,       // Shift+` (US layout)
            // Unshifted symbols
            ["-"] = ConsoleKey.OemMinus,
            ["="] = ConsoleKey.OemPlus,
            ["["] = ConsoleKey.Oem4,
            ["]"] = ConsoleKey.Oem6,
            ["\\"] = ConsoleKey.Oem5,
            [";"] = ConsoleKey.Oem1,
            ["'"] = ConsoleKey.Oem7,
            [","] = ConsoleKey.OemComma,
            ["."] = ConsoleKey.OemPeriod,
            ["/"] = ConsoleKey.Oem2,
            ["`"] = ConsoleKey.Oem3,
            [" "] = ConsoleKey.Spacebar,
        };

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
            return _consoleKeyFormatMap.TryGetValue(key, out var formatted) ? formatted : key.ToString();
        }

        /// <summary>
        /// Static dictionary mapping ConsoleKey values to user-friendly string representations
        /// </summary>
        private static readonly Dictionary<ConsoleKey, string> _consoleKeyFormatMap = new()
        {
            // Special keys with user-friendly names
            [ConsoleKey.Spacebar] = "Space",
            [ConsoleKey.Enter] = "Enter",
            [ConsoleKey.Escape] = "Esc",
            [ConsoleKey.Tab] = "Tab",
            [ConsoleKey.Backspace] = "Backspace",
            [ConsoleKey.Delete] = "Delete",
            [ConsoleKey.Insert] = "Insert",
            [ConsoleKey.Home] = "Home",
            [ConsoleKey.End] = "End",
            [ConsoleKey.PageUp] = "PageUp",
            [ConsoleKey.PageDown] = "PageDown",
            [ConsoleKey.UpArrow] = "Up",
            [ConsoleKey.DownArrow] = "Down",
            [ConsoleKey.LeftArrow] = "Left",
            [ConsoleKey.RightArrow] = "Right",
            // Format numbers without the 'D' prefix
            [ConsoleKey.D0] = "0",
            [ConsoleKey.D1] = "1",
            [ConsoleKey.D2] = "2",
            [ConsoleKey.D3] = "3",
            [ConsoleKey.D4] = "4",
            [ConsoleKey.D5] = "5",
            [ConsoleKey.D6] = "6",
            [ConsoleKey.D7] = "7",
            [ConsoleKey.D8] = "8",
            [ConsoleKey.D9] = "9",
            // Format common OEM keys in a user-friendly way
            [ConsoleKey.OemMinus] = "-",
            [ConsoleKey.OemPlus] = "=",
            [ConsoleKey.Oem4] = "[",
            [ConsoleKey.Oem6] = "]",
            [ConsoleKey.Oem5] = "\\",
            [ConsoleKey.Oem1] = ";",
            [ConsoleKey.Oem7] = "'",
            [ConsoleKey.OemComma] = ",",
            [ConsoleKey.OemPeriod] = ".",
            [ConsoleKey.Oem2] = "/",
            [ConsoleKey.Oem3] = "`",
        };
    }
}