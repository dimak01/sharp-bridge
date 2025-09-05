using System.Text.RegularExpressions;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Provides ANSI color codes and helper methods for console output
    /// </summary>
    public static class ConsoleColors
    {
        // Cache the compiled regex for ANSI escape sequence removal
        private static readonly Regex AnsiEscapeRegex = new Regex(@"\u001b\[[0-9;]*m", RegexOptions.Compiled, System.TimeSpan.FromSeconds(0.1));
        // Reset and formatting
        /// <summary>
        /// ANSI escape code to reset all formatting and colors
        /// </summary>
        public const string Reset = "\u001b[0m";

        /// <summary>
        /// ANSI escape code for bold text formatting
        /// </summary>
        public const string Bold = "\u001b[1m";

        /// <summary>
        /// ANSI escape code for underlined text formatting
        /// </summary>
        public const string Underline = "\u001b[4m";

        // Status colors
        /// <summary>
        /// Green color for healthy status indicators
        /// </summary>
        public const string Healthy = "\u001b[32m";      // Green

        /// <summary>
        /// Yellow color for warning status indicators
        /// </summary>
        public const string Warning = "\u001b[33m";      // Yellow  

        /// <summary>
        /// Red color for error status indicators
        /// </summary>
        public const string Error = "\u001b[91m";        // Red

        /// <summary>
        /// Cyan color for informational status indicators
        /// </summary>
        public const string Info = "\u001b[36m";         // Cyan

        /// <summary>
        /// Bright green color for success status indicators
        /// </summary>
        public const string Success = "\u001b[92m";      // Bright green

        /// <summary>
        /// Gray color for disabled status indicators
        /// </summary>
        public const string Disabled = "\u001b[90m";     // Dark gray

        // Service status colors
        /// <summary>
        /// Green color for connected service status
        /// </summary>
        public const string Connected = "\u001b[32m";    // Green

        /// <summary>
        /// Yellow color for connecting service status
        /// </summary>
        public const string Connecting = "\u001b[33m";   // Yellow

        /// <summary>
        /// Red color for disconnected service status
        /// </summary>
        public const string Disconnected = "\u001b[31m"; // Red

        /// <summary>
        /// Cyan color for initializing service status
        /// </summary>
        public const string Initializing = "\u001b[36m"; // Cyan

        /// <summary>
        /// Light cyan color for blend shape names (iPhone source data)
        /// Color-blind friendly and visually distinct
        /// </summary>
        public const string BlendShapeColor = "\u001b[96m";

        /// <summary>
        /// Very bright magenta color for head rotation/position parameters (iPhone source data)
        /// Color-blind friendly and visually distinct from blend shapes
        /// </summary>
        public const string HeadParameterColor = "\u001b[38;5;213m"; // Very bright magenta (256-color)

        /// <summary>
        /// Light yellow color for calculated parameter names (PC derived parameters)
        /// Color-blind friendly and visually distinct from blend shapes
        /// </summary>
        public const string CalculatedParameterColor = "\u001b[93m";

        /// <summary>
        /// Light red color for rule error names (used in error tables)
        /// </summary>
        public const string RuleErrorNameColor = "\u001b[91m"; // Bright red

        /// <summary>
        /// Light Cyan color for config path display
        /// </summary>
        public const string ConfigPathColor = "\u001b[96m"; // Light Cyan

        /// <summary>
        /// Bright magenta color for shortcut action names
        /// </summary>
        public const string ShortcutActionColor = "\u001b[93m"; // Light yellow

        /// <summary>
        /// Light yellow color for shortcut key combinations
        /// </summary>
        public const string ShortcutKeyColor = "\u001b[96m"; // Light Cyan

        /// <summary>
        /// Light yellow color for shortcut key combinations
        /// </summary>
        public const string ConfigPropertyName = "\u001b[93m"; // Light yellow

        /// <summary>
        /// Light cyan color for string values
        /// </summary>
        public const string StringValueColor = "\u001b[96m"; // Light cyan

        /// <summary>
        /// Light orange color for numeric values
        /// </summary>
        public const string NumericValueColor = "\u001b[38;5;215m"; // Light orange (256-color)

        /// <summary>
        /// Light magenta color for boolean values
        /// </summary>
        public const string BooleanValueColor = "\u001b[96m"; // Light cyan

        /// <summary>
        /// Wraps text with the specified color and resets afterward
        /// </summary>
        /// <param name="text">Text to colorize</param>
        /// <param name="color">ANSI color code</param>
        /// <returns>Colorized text with reset</returns>
        public static string Colorize(string text, string color)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return $"{color}{text}{Reset}";
        }

        /// <summary>
        /// Convenience method to colorize blend shape names
        /// </summary>
        /// <param name="blendShapeName">Blend shape name to colorize</param>
        /// <returns>Blend shape name in light cyan with reset</returns>
        public static string ColorizeBlendShape(string blendShapeName)
        {
            return Colorize(blendShapeName, BlendShapeColor);
        }

        /// <summary>
        /// Convenience method to colorize head rotation/position parameters
        /// </summary>
        /// <param name="parameterName">Head parameter name to colorize</param>
        /// <returns>Parameter name in very bright magenta with reset</returns>
        public static string ColorizeHeadParameter(string parameterName)
        {
            return Colorize(parameterName, HeadParameterColor);
        }

        /// <summary>
        /// Convenience method to colorize calculated parameter names
        /// </summary>
        /// <param name="parameterName">Parameter name to colorize</param>
        /// <returns>Parameter name in light yellow with reset</returns>
        public static string ColorizeCalculatedParameter(string parameterName)
        {
            return Colorize(parameterName, CalculatedParameterColor);
        }

        /// <summary>
        /// Convenience method to colorize rule error names
        /// </summary>
        /// <param name="ruleName">Rule name to colorize</param>
        /// <returns>Rule name in light red with reset</returns>
        public static string ColorizeRuleErrorName(string ruleName)
        {
            return Colorize(ruleName, RuleErrorNameColor);
        }

        /// <summary>
        /// Colorizes a value based on its basic type
        /// </summary>
        /// <param name="value">Value to colorize</param>
        /// <returns>Colorized value based on type</returns>
        public static string ColorizeBasicType(object? value)
        {
            return value switch
            {
                null => "Not set",
                string str when string.IsNullOrWhiteSpace(str) => "Not set",
                string str => Colorize(str, StringValueColor), // Light cyan for strings
                int or long or short or byte => Colorize(value.ToString()!, NumericValueColor), // Light orange for integers
                double or float or decimal => Colorize(value.ToString()!, NumericValueColor), // Light orange for decimals
                bool b => Colorize(b ? "true" : "false", BooleanValueColor), // Light magenta for booleans
                _ => value.ToString() ?? "Not set" // Default for other types
            };
        }

        /// <summary>
        /// Gets the appropriate color for a health status
        /// </summary>
        /// <param name="isHealthy">Whether the service is healthy</param>
        /// <returns>ANSI color code</returns>
        public static string GetHealthColor(bool isHealthy)
        {
            return isHealthy ? Healthy : Error;
        }

        /// <summary>
        /// Gets the appropriate color for a service status
        /// </summary>
        /// <param name="status">Service status string</param>
        /// <returns>ANSI color code</returns>
        public static string GetStatusColor(string status)
        {
            return status.ToLowerInvariant() switch
            {
                // Check for failure states first (more specific patterns)
                var s when s.Contains("error") || s.Contains("fail") => Error,
                var s when s.Contains("disconnect") => Disconnected,
                // Then check for positive states
                var s when s.Contains("connect") && !s.Contains("disconnect") => Connected,
                var s when s.Contains("sending") || s.Contains("receiving") => Success,
                var s when s.Contains("initializ") => Initializing,
                _ => Info
            };
        }

        /// <summary>
        /// Calculates the visual length of a string, excluding ANSI escape sequences.
        /// This is essential for proper padding and alignment when using colored text.
        /// </summary>
        /// <param name="text">Text that may contain ANSI escape sequences</param>
        /// <returns>The number of visible characters (excluding ANSI sequences)</returns>
        public static int GetVisualLength(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            // Strip all ANSI escape sequences and return visible character count
            return AnsiEscapeRegex.Replace(text, "").Length;
        }

        /// <summary>
        /// Removes ANSI escape codes from a string (for test assertions and plain output)
        /// </summary>
        /// <param name="text">Text that may contain ANSI escape sequences</param>
        /// <returns>String with all ANSI escape codes removed</returns>
        public static string RemoveAnsiEscapeCodes(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            return AnsiEscapeRegex.Replace(text, "");
        }
    }
}