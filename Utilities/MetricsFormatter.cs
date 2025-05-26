using System;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Provides consistent formatting for service metrics with proper padding
    /// </summary>
    public static class MetricsFormatter
    {
        // Define expected maximum widths for consistent formatting
        private const int FAILED_COUNT_WIDTH = 4;   // Up to 9,999 failed
        private const int FPS_WIDTH = 3;            // Up to 999 FPS
        private const int TIME_WIDTH = 6;           // "999.9s" format
        private const int CONTENT_WIDTH = 15;       // Width for content before the | character
        
        /// <summary>
        /// Formats service metrics with consistent padding
        /// </summary>
        /// <param name="totalFrames">Total frames received</param>
        /// <param name="failedFrames">Failed frame count</param>
        /// <param name="fps">Frames per second</param>
        /// <returns>Formatted metrics string</returns>
        public static string FormatMetrics(long totalFrames, long failedFrames, long fps)
        {
            var failedStr = failedFrames.ToString().PadLeft(FAILED_COUNT_WIDTH);
            var fpsStr = fps.ToString().PadLeft(FPS_WIDTH);
            
            var framesContent = $"{totalFrames:N0} frames";
            
            return $"Metrics: {framesContent.PadLeft(CONTENT_WIDTH)} | {failedStr} failed | {fpsStr} FPS";
        }
        
        /// <summary>
        /// Formats health status with consistent padding
        /// </summary>
        /// <param name="isHealthy">Whether the service is healthy</param>
        /// <param name="lastSuccess">Last successful operation timestamp</param>
        /// <param name="lastError">Last error message (optional)</param>
        /// <returns>Formatted health status string</returns>
        public static string FormatHealthStatus(bool isHealthy, DateTime? lastSuccess, string lastError = null)
        {
            var healthIcon = isHealthy ? "√" : "X";
            var healthText = (isHealthy ? "Healthy" : "Unhealthy");
            var healthColor = ConsoleColors.GetHealthColor(isHealthy);
            
            var timeAgo = lastSuccess.HasValue && !(lastSuccess.Value == DateTime.MinValue)
                ? FormatTimeAgo(DateTime.UtcNow - lastSuccess.Value)
                : "Never".PadLeft(TIME_WIDTH);

            
            var healthContent = $"{healthIcon} {healthText}";
            var colorizedHealth = ConsoleColors.Colorize(healthContent, healthColor);
            
            // Note: We need to account for the invisible color codes when padding
            var result = $"Health:  {healthContent.PadLeft(CONTENT_WIDTH)} | Last Success: {timeAgo}";
            
            // Apply color to the health content after padding calculation
            result = result.Replace(healthContent, colorizedHealth);
            
            // Add error information if unhealthy and error is provided
            if (!isHealthy && !string.IsNullOrEmpty(lastError))
            {
                var errorText = lastError.Length > 50 ? lastError.Substring(0, 47) + "..." : lastError;
                result += $"\nError: {ConsoleColors.Colorize(errorText, ConsoleColors.Error)}";
            }
            
            return result;
        }
        
        /// <summary>
        /// Formats a time span into a human-readable string with consistent width
        /// </summary>
        /// <param name="timeSpan">Time span to format</param>
        /// <returns>Formatted time string</returns>
        private static string FormatTimeAgo(TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 60)
                return $"{timeSpan.TotalSeconds:F0}s".PadLeft(TIME_WIDTH);
            else if (timeSpan.TotalMinutes < 60)
                return $"{timeSpan.TotalMinutes:F0}m".PadLeft(TIME_WIDTH);
            else if (timeSpan.TotalHours < 24)
                return $"{timeSpan.TotalHours:F0}h".PadLeft(TIME_WIDTH);
            else
                return $"{timeSpan.TotalDays:F0}d".PadLeft(TIME_WIDTH);
        }
        
        /// <summary>
        /// Formats a service header with status and color coding
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="status">Current status</param>
        /// <param name="shortcut">Keyboard shortcut (e.g., "Alt+O")</param>
        /// <returns>Formatted header string</returns>
        public static string FormatServiceHeader(string serviceName, string status, string shortcut)
        {
            var statusColor = ConsoleColors.GetStatusColor(status);
            var colorizedStatus = ConsoleColors.Colorize(status, statusColor);
            return $"=== {serviceName} ({colorizedStatus}) === [{shortcut}]";
        }
    }
} 