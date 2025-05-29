using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Formatter for PCTrackingInfo objects
    /// </summary>
    public class PCTrackingInfoFormatter : IFormatter
    {
        private const int PARAM_DISPLAY_COUNT_NORMAL = 10;
        
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
        /// Formats a PCTrackingInfo object with service statistics into a display string
        /// </summary>
        public string Format(IServiceStats serviceStats)
        {
            if (serviceStats == null) 
                return "No service data available";
            
            var builder = new StringBuilder();
            
            // Header with service status
            AppendServiceHeader(builder, serviceStats);
            
            // Tracking data details
            if (serviceStats.CurrentEntity is PCTrackingInfo pcTrackingInfo)
            {
            var parameters = pcTrackingInfo.Parameters?.ToList() ?? new List<TrackingParam>();
            
            if (CurrentVerbosity >= VerbosityLevel.Normal && parameters.Any())
            {
                AppendParameters(builder, pcTrackingInfo);
                }
            }
            else if (serviceStats.CurrentEntity != null)
            {
                throw new ArgumentException("CurrentEntity must be of type PCTrackingInfo or null");
            }
            else
            {
                builder.AppendLine();
                builder.AppendLine("No current tracking data available");
            }
            
            return builder.ToString();
        }
        

        
        /// <summary>
        /// Appends the service header information to the string builder
        /// </summary>
        private void AppendServiceHeader(StringBuilder builder, IServiceStats serviceStats)
        {
            // Header with service status
            builder.AppendLine(FormatServiceHeader("PC Client", serviceStats.Status, "Alt+P"));
            builder.AppendLine($"Verbosity: {CurrentVerbosity}");
            builder.AppendLine();
            
            // Health status
            builder.AppendLine(FormatHealthStatus(
                serviceStats.IsHealthy, 
                serviceStats.LastSuccessfulOperation, 
                serviceStats.LastError));
            
            // Connection metrics if available
            if (serviceStats.Counters.ContainsKey("MessagesSent"))
            {
                var messagesSent = serviceStats.Counters["MessagesSent"];
                var connectionAttempts = serviceStats.Counters.ContainsKey("ConnectionAttempts") ? serviceStats.Counters["ConnectionAttempts"] : 0;
                var uptimeSeconds = serviceStats.Counters.ContainsKey("UptimeSeconds") ? serviceStats.Counters["UptimeSeconds"] : 0;
                
                builder.AppendLine(FormatConnectionMetrics(messagesSent, connectionAttempts, uptimeSeconds));
            }
            
            // Show tracking data info if available
            if (serviceStats.CurrentEntity is PCTrackingInfo pcTrackingInfo)
            {
                var faceIcon = pcTrackingInfo.FaceFound ? "√" : "X";
                var faceColor = pcTrackingInfo.FaceFound ? ConsoleColors.Success : ConsoleColors.Warning;
                builder.AppendLine($"Face Status: {ConsoleColors.Colorize($"{faceIcon} {(pcTrackingInfo.FaceFound ? "Detected" : "Not Found")}", faceColor)}");
            
                var parameterCount = pcTrackingInfo.Parameters?.Count() ?? 0;
            builder.AppendLine($"Parameter Count: {parameterCount}");
            }
            
            builder.AppendLine();
        }
        
        /// <summary>
        /// Appends the parameter information to the string builder
        /// </summary>
        private void AppendParameters(StringBuilder builder, PCTrackingInfo trackingInfo)
        {
            builder.AppendLine();
            builder.AppendLine("Top Parameters:");
            var parameters = trackingInfo.Parameters.ToList();
            int displayCount = CurrentVerbosity == VerbosityLevel.Detailed ? parameters.Count : PARAM_DISPLAY_COUNT_NORMAL;
            
            // Calculate the length of the longest parameter ID for proper alignmentDispose_WhenCalled_ClosesWebSocketDispose_WhenCalled_ClosesWebSocket
            int maxIdLength = CalculateMaxIdLength(parameters, displayCount);
            
            // Display parameters
            for (int i = 0; i < Math.Min(displayCount, parameters.Count); i++)
            {
                var param = parameters[i];
                trackingInfo.ParameterDefinitions.TryGetValue(param.Id, out var definition);
                AppendParameterInfo(builder, param, maxIdLength, definition);
            }
            
            // Show count of additional parameters if not all are displayed
            if (parameters.Count > displayCount)
            {
                builder.AppendLine($"  ... and {parameters.Count - displayCount} more");
            }
        }
        
        /// <summary>
        /// Calculates the maximum ID length for proper alignment
        /// </summary>
        private int CalculateMaxIdLength(List<TrackingParam> parameters, int displayCount)
        {
            int maxIdLength = parameters.Take(displayCount)
                .Select(p => p.Id?.Length ?? 0)
                .DefaultIfEmpty(0)
                .Max();
            
            // Add 1 for extra spacing
            return maxIdLength + 1;
        }
        
        /// <summary>
        /// Appends a single parameter's information to the string builder
        /// </summary>
        private void AppendParameterInfo(StringBuilder builder, TrackingParam param, int maxIdLength, VTSParameter definition)
        {
            string id = param.Id ?? string.Empty;
            string formattedValue = FormatNumericValue(param.Value);
            
            string progressBar = definition != null 
                ? CreateProgressBar(param.Value, definition.Min, definition.Max)
                : CreateProgressBar(param.Value, -1, 1); // Fallback to default range
                
            string weightPart = FormatWeightPart(param);
            string rangeInfo = FormatRangeInfo(definition);
            
            builder.AppendLine($"  {id.PadRight(maxIdLength)}: {progressBar} {formattedValue} ({(string.IsNullOrEmpty(weightPart) ? "" : $"{weightPart}, ")}{rangeInfo})");
        }
        
        /// <summary>
        /// Creates a progress bar visualization for a parameter value
        /// </summary>
        private string CreateProgressBar(double value, double min, double max)
        {
            const int barLength = 20;
            const char fillChar = '█';
            const char emptyChar = '░';
            
            // Calculate the normalized position (0.0 to 1.0)
            double range = max - min;
            double normalizedValue = range != 0 ? (value - min) / range : 0.5;
            
            // Clamp to valid range
            normalizedValue = Math.Max(0, Math.Min(1, normalizedValue));
            
            // Calculate the number of filled positions
            int fillCount = (int)Math.Round(normalizedValue * barLength);
            
            // Build the progress bar
            var barBuilder = new StringBuilder(barLength);
            for (int i = 0; i < barLength; i++)
            {
                barBuilder.Append(i < fillCount ? fillChar : emptyChar);
            }
            
            return barBuilder.ToString();
        }
        
        /// <summary>
        /// Formats a numeric value with sign-aware padding
        /// </summary>
        private string FormatNumericValue(double value)
        {
            // Add a space before positive numbers to align with negative numbers
            return value >= 0 ? $" {value:F2}" : $"{value:F2}";
        }
        
        /// <summary>
        /// Formats the weight part of a parameter
        /// </summary>
        private string FormatWeightPart(TrackingParam param)
        {
            if (!param.Weight.HasValue) return "";
            return $"weight: {FormatNumericValue(param.Weight.Value)}";
        }
        
        /// <summary>
        /// Formats the range information of a parameter
        /// </summary>
        private string FormatRangeInfo(VTSParameter definition)
        {
            if (definition == null) return "no definition";
            
            string minStr = $"min: {FormatNumericValue(definition.Min)}";
            string maxStr = $"max: {FormatNumericValue(definition.Max)}";
            string defaultStr = $"default: {FormatNumericValue(definition.DefaultValue)}";
            
            return $"{minStr}, {maxStr}, {defaultStr}";
        }
        
        /// <summary>
        /// Formats connection metrics for PC client
        /// </summary>
        /// <param name="messagesSent">Number of messages sent</param>
        /// <param name="connectionAttempts">Number of connection attempts</param>
        /// <param name="uptimeSeconds">Uptime in seconds</param>
        /// <returns>Formatted connection metrics string</returns>
        private string FormatConnectionMetrics(long messagesSent, long connectionAttempts, long uptimeSeconds)
        {
            var uptimeFormatted = FormatUptime(uptimeSeconds);
            return $"Connection: {messagesSent} msgs sent | {connectionAttempts} attempts | {uptimeFormatted} uptime";
        }
        
        /// <summary>
        /// Formats uptime into a human-readable string
        /// </summary>
        /// <param name="uptimeSeconds">Uptime in seconds</param>
        /// <returns>Formatted uptime string</returns>
        private string FormatUptime(long uptimeSeconds)
        {
            if (uptimeSeconds < 60)
                return $"{uptimeSeconds}s";
            else if (uptimeSeconds < 3600)
                return $"{uptimeSeconds / 60}m";
            else if (uptimeSeconds < 86400)
                return $"{uptimeSeconds / 3600}h";
            else
                return $"{uptimeSeconds / 86400}d";
        }
        
        /// <summary>
        /// Formats health status for PC client
        /// </summary>
        /// <param name="isHealthy">Whether the service is healthy</param>
        /// <param name="lastSuccess">Last successful operation timestamp</param>
        /// <param name="lastError">Last error message (optional)</param>
        /// <returns>Formatted health status string</returns>
        private string FormatHealthStatus(bool isHealthy, DateTime? lastSuccess, string lastError = null)
        {
            var healthIcon = isHealthy ? "√" : "X";
            var healthText = (isHealthy ? "Healthy" : "Unhealthy");
            var healthColor = ConsoleColors.GetHealthColor(isHealthy);
            
            var timeAgo = lastSuccess.HasValue && !(lastSuccess.Value == DateTime.MinValue)
                ? FormatTimeAgo(DateTime.UtcNow - lastSuccess.Value)
                : "Never";
            
            var healthContent = $"{healthIcon} {healthText}";
            var colorizedHealth = ConsoleColors.Colorize(healthContent, healthColor);

            var result = $"Health: {colorizedHealth}";
            result += Environment.NewLine;
            result += $"Last Success: {timeAgo}";
            
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
        /// Formats a time span into a human-readable string
        /// </summary>
        /// <param name="timeSpan">Time span to format</param>
        /// <returns>Formatted time string</returns>
        private string FormatTimeAgo(TimeSpan timeSpan)
        {
            if (timeSpan.TotalSeconds < 60)
                return $"{timeSpan.TotalSeconds:F0}s";
            else if (timeSpan.TotalMinutes < 60)
                return $"{timeSpan.TotalMinutes:F0}m";
            else if (timeSpan.TotalHours < 24)
                return $"{timeSpan.TotalHours:F0}h";
            else
                return $"{timeSpan.TotalDays:F0}d";
        }
        
        /// <summary>
        /// Formats a service header with status and color coding
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="status">Current status</param>
        /// <param name="shortcut">Keyboard shortcut (e.g., "Alt+P")</param>
        /// <returns>Formatted header string</returns>
        private string FormatServiceHeader(string serviceName, string status, string shortcut)
        {
            var statusColor = ConsoleColors.GetStatusColor(status);
            var colorizedStatus = ConsoleColors.Colorize(status, statusColor);
            return $"=== {serviceName} ({colorizedStatus}) === [{shortcut}]";
        }
    }
} 