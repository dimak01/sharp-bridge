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
        private const int PARAM_DISPLAY_COUNT_NORMAL = 25;
        
        private readonly IConsole _console;
        private readonly ITableFormatter _tableFormatter;
        private readonly IParameterColorService _colorService;
        
        /// <summary>
        /// Initializes a new instance of the PCTrackingInfoFormatter
        /// </summary>
        /// <param name="console">Console abstraction for getting window dimensions</param>
        /// <param name="tableFormatter">Table formatter for generating tables</param>
        /// <param name="colorService">Parameter color service for colored display</param>
        public PCTrackingInfoFormatter(IConsole console, ITableFormatter tableFormatter, IParameterColorService colorService)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
            _tableFormatter = tableFormatter ?? throw new ArgumentNullException(nameof(tableFormatter));
            _colorService = colorService ?? throw new ArgumentNullException(nameof(colorService));
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
        /// Formats a PCTrackingInfo object with service statistics into a display string
        /// </summary>
        public string Format(IServiceStats stats)
        {
            if (stats == null) 
                return "No service data available";
            
            var builder = new StringBuilder();
            
            // Header with service status
            AppendServiceHeader(builder, stats);
            
            // Tracking data details
            if (stats.CurrentEntity is PCTrackingInfo pcTrackingInfo)
            {
                var parameters = pcTrackingInfo.Parameters?.ToList() ?? new List<TrackingParam>();
            
                if (CurrentVerbosity >= VerbosityLevel.Normal)
                {
                    AppendParameters(builder, pcTrackingInfo);
                }
            }
            else if (stats.CurrentEntity != null)
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
        /// Appends the parameter information to the string builder using the new table format
        /// </summary>
        private void AppendParameters(StringBuilder builder, PCTrackingInfo trackingInfo)
        {
            var parameters = trackingInfo.Parameters.ToList();
            
            // Sort parameters by ID - let TableFormatter handle display limits
            var parametersToShow = parameters
                .OrderBy(p => p.Id)
                .ToList();
            
            // Define columns for the generic table
            var columns = new List<ITableColumn<TrackingParam>>
            {
                new TextColumn<TrackingParam>("Parameter", param => _colorService.GetColoredParameterName(param.Id), minWidth: 8),
                new ProgressBarColumn<TrackingParam>("", param => CalculateNormalizedValue(param, trackingInfo), minWidth: 6, maxWidth: 20, _tableFormatter),
                new NumericColumn<TrackingParam>("Value", param => param.Value, "0.##", minWidth: 6, padLeft: true),
                new TextColumn<TrackingParam>("Width x Range", param => FormatCompactRange(param, trackingInfo), minWidth: 12, maxWidth: 25),
                new TextColumn<TrackingParam>("Expression", param => _colorService.GetColoredExpression(FormatExpression(param, trackingInfo)), minWidth: 15, maxWidth: 90)
            };

            // Use the new generic table formatter - let it handle display limits
            var singleColumnLimit = CurrentVerbosity == VerbosityLevel.Detailed ? (int?)null : PARAM_DISPLAY_COUNT_NORMAL;
            _tableFormatter.AppendTable(builder, "=== Parameters ===", parametersToShow, columns, 2, _console.WindowWidth, 20, singleColumnLimit);
        }
        
        /// <summary>
        /// Calculates the normalized value (0.0 to 1.0) for a parameter based on its definition
        /// </summary>
        private double CalculateNormalizedValue(TrackingParam param, PCTrackingInfo trackingInfo)
        {
            if (trackingInfo.ParameterDefinitions?.TryGetValue(param.Id, out var definition) == true)
            {
                double range = definition.Max - definition.Min;
                if (Math.Abs(range) > double.Epsilon)
                {
                    return Math.Max(0, Math.Min(1, (param.Value - definition.Min) / range));
                }
            }
            
            // Fallback: assume range of -1 to 1
            return Math.Max(0, Math.Min(1, (param.Value + 1) / 2));
        }
        
        /// <summary>
        /// Formats the compact range information for a parameter
        /// </summary>
        private string FormatCompactRange(TrackingParam param, PCTrackingInfo trackingInfo)
        {
            var weight = param.Weight?.ToString("0.##") ?? "1";
            
            if (trackingInfo.ParameterDefinitions?.TryGetValue(param.Id, out var definition) == true)
            {
                var min = definition.Min.ToString("0.##");
                var defaultVal = definition.DefaultValue.ToString("0.##");
                var max = definition.Max.ToString("0.##");
                return $"{weight} x [{min}; {defaultVal}; {max}]";
            }
            
            // Fallback for parameters without definitions
            return $"{weight} x [no definition]";
        }
        
        /// <summary>
        /// Formats the transformation expression for a parameter
        /// </summary>
        private string FormatExpression(TrackingParam param, PCTrackingInfo trackingInfo)
        {
            if (trackingInfo?.ParameterCalculationExpressions == null)
                return "[no expression]";

            if (trackingInfo.ParameterCalculationExpressions.TryGetValue(param.Id, out var expression))
            {
                if (string.IsNullOrEmpty(expression))
                    return "[no expression]";

                // Truncate long expressions for display
                const int maxLength = 90;
                if (expression.Length > maxLength)
                {
                    return expression.Substring(0, maxLength - 3) + "...";
                }
                return expression;
            }
            
            // Fallback for parameters without expressions
            return "[no expression]";
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
            return DisplayFormatting.FormatDuration(TimeSpan.FromSeconds(uptimeSeconds));
        }
        
        /// <summary>
        /// Formats a time span into a human-readable string
        /// </summary>
        /// <param name="timeSpan">Time span to format</param>
        /// <returns>Formatted time string</returns>
        private string FormatTimeAgo(TimeSpan timeSpan)
        {
            return DisplayFormatting.FormatDuration(timeSpan);
        }
        
        /// <summary>
        /// Formats health status for PC client
        /// </summary>
        /// <param name="isHealthy">Whether the service is healthy</param>
        /// <param name="lastSuccess">Last successful operation timestamp</param>
        /// <param name="lastError">Last error message (optional)</param>
        /// <returns>Formatted health status string</returns>
        private string FormatHealthStatus(bool isHealthy, DateTime lastSuccess, string? lastError = null)
        {
            var healthIcon = isHealthy ? "√" : "X";
            var healthText = (isHealthy ? "Healthy" : "Unhealthy");
            var healthColor = ConsoleColors.GetHealthColor(isHealthy);
            
            var timeAgo = lastSuccess != DateTime.MinValue
                ? FormatTimeAgo(DateTime.UtcNow - lastSuccess)
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