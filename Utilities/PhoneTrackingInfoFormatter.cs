using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using SharpBridge.Interfaces;
using SharpBridge.Models;

[assembly: InternalsVisibleTo("Tests")]

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Formatter for PhoneTrackingInfo objects with service statistics
    /// </summary>
    public class PhoneTrackingInfoFormatter : IFormatter
    {
        private const int TARGET_COLUMN_COUNT = 4;
        private const int TARGET_ROWS_NORMAL = 13;
        
        // Define expected maximum widths for consistent formatting
        private const int FAILED_COUNT_WIDTH = 4;   // Up to 9,999 failed
        private const int FPS_WIDTH = 3;            // Up to 999 FPS
        private const int TIME_WIDTH = 6;           // "999.9s" format
        private const int FRAMES_WIDTH = 6;         // Up to 999999 frames
        
        private readonly IConsole _console;
        private readonly ITableFormatter _tableFormatter;
        
        /// <summary>
        /// Gets or sets the current time for testing purposes. If null, DateTime.UtcNow is used.
        /// </summary>
        internal DateTime? CurrentTime { get; set; }
        
        /// <summary>
        /// Gets the current time, using the test time if set, otherwise DateTime.UtcNow
        /// </summary>
        private DateTime GetCurrentTime() => CurrentTime ?? DateTime.UtcNow;
        
        /// <summary>
        /// Initializes a new instance of the PhoneTrackingInfoFormatter
        /// </summary>
        /// <param name="console">Console abstraction for getting window dimensions</param>
        /// <param name="tableFormatter">Table formatter for generating tables</param>
        public PhoneTrackingInfoFormatter(IConsole console, ITableFormatter tableFormatter)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
            _tableFormatter = tableFormatter ?? throw new ArgumentNullException(nameof(tableFormatter));
        }
        
        /// <summary>
        /// Current verbosity level for this formatter
        /// </summary>
        public VerbosityLevel CurrentVerbosity { get; private set; } = VerbosityLevel.Normal;
        
        /// <summary>
        /// Cycles to the next verbosity level
        /// </summary>
        public void CycleVerbosity()
        {
            CurrentVerbosity = CurrentVerbosity switch
            {
                VerbosityLevel.Basic => VerbosityLevel.Normal,
                VerbosityLevel.Normal => VerbosityLevel.Detailed,
                VerbosityLevel.Detailed => VerbosityLevel.Basic,
                _ => VerbosityLevel.Normal
            };
        }
        
        /// <summary>
        /// Formats a PhoneTrackingInfo object with service statistics into a display string
        /// </summary>
        public string Format(IServiceStats serviceStats)
        {
            if (serviceStats == null) 
                return "No service data available";
            
            var builder = new StringBuilder();
            
            // Header with service status
            builder.AppendLine(FormatServiceHeader("iPhone Tracking Data", serviceStats.Status, "Alt+O"));
            builder.AppendLine($"Verbosity: {CurrentVerbosity}");
            builder.AppendLine();
            
            // Health status
            builder.AppendLine(FormatHealthStatus(
                serviceStats.IsHealthy, 
                serviceStats.LastSuccessfulOperation, 
                serviceStats.LastError));
            
            // Metrics with proper padding
            if (serviceStats.Counters.ContainsKey("Total Frames") && 
                serviceStats.Counters.ContainsKey("Failed Frames"))
            {
                var totalFrames = serviceStats.Counters["Total Frames"];
                var failedFrames = serviceStats.Counters["Failed Frames"];
                var fps = serviceStats.Counters.ContainsKey("FPS") ? serviceStats.Counters["FPS"] : 0;
                
                builder.AppendLine(FormatMetrics(totalFrames, failedFrames, fps));
            }
            
            // Tracking data details
            if (serviceStats.CurrentEntity is PhoneTrackingInfo phoneTrackingInfo)
            {
                builder.AppendLine();
                AppendTrackingDetails(builder, phoneTrackingInfo);
            }
            else if (serviceStats.CurrentEntity != null)
            {
                throw new ArgumentException("CurrentEntity must be of type PhoneTrackingInfo or null");
            }
            else
            {
                builder.AppendLine();
                builder.AppendLine("No current tracking data available");
            }
            
            return builder.ToString();
        }
        

        
        /// <summary>
        /// Appends tracking data details to the string builder
        /// </summary>
        private void AppendTrackingDetails(StringBuilder builder, PhoneTrackingInfo phoneTrackingInfo)
        {
            // Face detection status with ASCII icon
            var faceIcon = phoneTrackingInfo.FaceFound ? "√" : "X";
            var faceColor = phoneTrackingInfo.FaceFound ? ConsoleColors.Success : ConsoleColors.Warning;
            builder.AppendLine($"Face Status: {ConsoleColors.Colorize($"{faceIcon} {(phoneTrackingInfo.FaceFound ? "Detected" : "Not Found")}", faceColor)}");
            
            // Only show head rotation and position if face is detected
            if (phoneTrackingInfo.FaceFound && CurrentVerbosity >= VerbosityLevel.Normal)
            {
                if (phoneTrackingInfo.Rotation != null)
                {
                    builder.AppendLine($"Head Rotation (X,Y,Z): " +
                        $"{phoneTrackingInfo.Rotation.X:F1}°, " +
                        $"{phoneTrackingInfo.Rotation.Y:F1}°, " +
                        $"{phoneTrackingInfo.Rotation.Z:F1}°");
                }
            
                if (phoneTrackingInfo.Position != null)
                {
                    builder.AppendLine($"Head Position (X,Y,Z): " +
                        $"{phoneTrackingInfo.Position.X:F1}, " +
                        $"{phoneTrackingInfo.Position.Y:F1}, " +
                        $"{phoneTrackingInfo.Position.Z:F1}");
                }
            }
            
            // Show blend shapes data in detailed mode
            if (CurrentVerbosity >= VerbosityLevel.Normal)
            {
                builder.AppendLine();
                
                if (phoneTrackingInfo.BlendShapes == null || phoneTrackingInfo.BlendShapes.Count == 0)
                {
                    builder.AppendLine("No blend shapes");
                }
                else
                {
                    // Sort blend shapes by name - let TableFormatter handle display limits
                    var sortedShapes = phoneTrackingInfo.BlendShapes
                        .Where(s => s != null)
                        .OrderBy(s => s.Key)
                        .ToList();

                    // Define columns for the generic table
                    var columns = new List<ITableColumn<BlendShape>>
                    {
                        new TextColumn<BlendShape>("Expression", shape => shape.Key, minWidth: 10, maxWidth: 20),
                        new ProgressBarColumn<BlendShape>("", shape => shape.Value, minWidth: 6, maxWidth: 15, _tableFormatter),
                        new NumericColumn<BlendShape>("Value", shape => shape.Value, "F2", minWidth: 6, padLeft: true)
                    };

                    // Use the new generic table formatter - let it handle display limits
                    var singleColumnLimit = CurrentVerbosity == VerbosityLevel.Detailed ? (int?)null : TARGET_ROWS_NORMAL;
                    _tableFormatter.AppendTable(builder, "BlendShapes:", sortedShapes, columns, TARGET_COLUMN_COUNT, _console.WindowWidth, 20, singleColumnLimit);

                    builder.AppendLine();
                    builder.AppendLine($"Total Blend Shapes: {phoneTrackingInfo.BlendShapes.Count}");
                }
            }
        }
        
        /// <summary>
        /// Formats service metrics with consistent padding
        /// </summary>
        /// <param name="totalFrames">Total frames received</param>
        /// <param name="failedFrames">Failed frame count</param>
        /// <param name="fps">Frames per second</param>
        /// <returns>Formatted metrics string</returns>
        private string FormatMetrics(long totalFrames, long failedFrames, long fps)
        {
            var failedStr = failedFrames.ToString().PadLeft(FAILED_COUNT_WIDTH);
            var fpsStr = fps.ToString().PadLeft(FPS_WIDTH);
            
            var framesContent = $"{totalFrames:N0} frames";
            
            return $"Metrics: {framesContent.PadLeft(FRAMES_WIDTH)} | {failedStr} failed | {fpsStr} FPS";
        }
        
        /// <summary>
        /// Formats health status with consistent padding
        /// </summary>
        /// <param name="isHealthy">Whether the service is healthy</param>
        /// <param name="lastSuccess">Last successful operation timestamp</param>
        /// <param name="lastError">Last error message (optional)</param>
        /// <returns>Formatted health status string</returns>
        private string FormatHealthStatus(bool isHealthy, DateTime lastSuccess, string lastError = null)
        {
            var healthIcon = isHealthy ? "√" : "X";
            var healthText = (isHealthy ? "Healthy" : "Unhealthy");
            var healthColor = ConsoleColors.GetHealthColor(isHealthy);
            
            var timeAgo = lastSuccess != DateTime.MinValue
                ? FormatTimeAgo(GetCurrentTime() - lastSuccess)
                : "Never".PadLeft(TIME_WIDTH);

            var healthContent = $"{healthIcon} {healthText}";
            var colorizedHealth = ConsoleColors.Colorize(healthContent, healthColor);

            // Note: We need to account for the invisible color codes when padding
            var result = $"Health: {healthContent}";
            result += Environment.NewLine;
            result += $"Last Success: {timeAgo}";
            
            // Apply color to the health content after padding calculation
            result = result.Replace(healthContent, colorizedHealth);
            
            // Add error information if unhealthy and error is provided
            if (!isHealthy && !string.IsNullOrEmpty(lastError))
            {
                var errorText = lastError.Length > 50 ? lastError.Substring(0, 47) + "..." : lastError;
                result += Environment.NewLine;
                result += $"Error: {ConsoleColors.Colorize(errorText, ConsoleColors.Error)}";
            }
            
            return result;
        }
        
        /// <summary>
        /// Formats a time span into a human-readable string with consistent width
        /// </summary>
        /// <param name="timeSpan">Time span to format</param>
        /// <returns>Formatted time string</returns>
        private string FormatTimeAgo(TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 0)
                return "0s".PadLeft(TIME_WIDTH);
            else if (timeSpan.TotalSeconds < 60)
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
        private string FormatServiceHeader(string serviceName, string status, string shortcut)
        {
            var statusColor = ConsoleColors.GetStatusColor(status);
            var colorizedStatus = ConsoleColors.Colorize(status, statusColor);
            return $"=== {serviceName} ({colorizedStatus}) === [{shortcut}]";
        }
    }
} 