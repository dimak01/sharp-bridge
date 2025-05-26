namespace SharpBridge.Utilities
{
    /// <summary>
    /// Provides ANSI color codes and helper methods for console output
    /// </summary>
    public static class ConsoleColors
    {
        // Reset and formatting
        public const string Reset = "\u001b[0m";
        public const string Bold = "\u001b[1m";
        public const string Underline = "\u001b[4m";
        
        // Status colors
        public const string Healthy = "\u001b[32m";      // Green
        public const string Warning = "\u001b[33m";      // Yellow  
        public const string Error = "\u001b[31m";        // Red
        public const string Info = "\u001b[36m";         // Cyan
        public const string Success = "\u001b[92m";      // Bright green
        
        // Service status colors
        public const string Connected = "\u001b[32m";    // Green
        public const string Connecting = "\u001b[33m";   // Yellow
        public const string Disconnected = "\u001b[31m"; // Red
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
                var s when s.Contains("connect") && !s.Contains("disconnect") => Connected,
                var s when s.Contains("initializ") => Initializing,
                var s when s.Contains("sending") || s.Contains("receiving") => Success,
                var s when s.Contains("error") || s.Contains("fail") => Error,
                var s when s.Contains("disconnect") => Disconnected,
                _ => Info
            };
        }
    }
} 