namespace SharpBridge.Utilities
{
    /// <summary>
    /// Provides ANSI color codes and helper methods for console output
    /// </summary>
    public static class ConsoleColors
    {
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
        public const string Error = "\u001b[31m";        // Red
        
        /// <summary>
        /// Cyan color for informational status indicators
        /// </summary>
        public const string Info = "\u001b[36m";         // Cyan
        
        /// <summary>
        /// Bright green color for success status indicators
        /// </summary>
        public const string Success = "\u001b[92m";      // Bright green
        
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
        /// Wraps text with the specified color and resets afterward
        /// </summary>
        /// <param name="text">Text to colorize</param>
        /// <param name="color">ANSI color code</param>
        /// <returns>Colorized text with reset</returns>
        public static string Colorize(string text, string color)
        {
            return $"{color}{text}{Reset}";
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
    }
} 