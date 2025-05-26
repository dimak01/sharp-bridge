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
        /// Legacy Format method for IFormatter interface compatibility
        /// </summary>
        public string Format(IFormattableObject formattableEntity)
        {
            // For legacy compatibility, create a minimal service stats object
            if (formattableEntity == null) return "No PC tracking data";
            if (!(formattableEntity is PCTrackingInfo))
                throw new ArgumentException("Entity must be of type PCTrackingInfo", nameof(formattableEntity));
            
            // Create a basic service stats for legacy calls
            var basicStats = new ServiceStats(
                serviceName: "VTube Studio Parameters",
                status: "Unknown",
                currentEntity: formattableEntity,
                isHealthy: true,
                lastSuccessfulOperation: DateTime.UtcNow,
                lastError: null,
                counters: new Dictionary<string, long>()
            );
            
            return Format(basicStats);
        }
        
        /// <summary>
        /// Appends the service header information to the string builder
        /// </summary>
        private void AppendServiceHeader(StringBuilder builder, IServiceStats serviceStats)
        {
            builder.AppendLine($"=== {serviceStats.ServiceName} ({serviceStats.Status}) === [Alt+P]");
            builder.AppendLine($"Verbosity: {CurrentVerbosity}");
            
            // Health status
            if (!serviceStats.IsHealthy && !string.IsNullOrEmpty(serviceStats.LastError))
            {
                builder.AppendLine($"Error: {serviceStats.LastError}");
            }
            
            // Show connection metrics if available
            if (serviceStats.Counters.ContainsKey("MessagesSent"))
            {
                var messagesSent = serviceStats.Counters["MessagesSent"];
                var connectionAttempts = serviceStats.Counters.ContainsKey("ConnectionAttempts") ? serviceStats.Counters["ConnectionAttempts"] : 0;
                var uptimeSeconds = serviceStats.Counters.ContainsKey("UptimeSeconds") ? serviceStats.Counters["UptimeSeconds"] : 0;
                
                builder.AppendLine($"Messages Sent: {messagesSent} | Connections: {connectionAttempts} | Uptime: {uptimeSeconds}s");
            }
            
            // Show tracking data info if available
            if (serviceStats.CurrentEntity is PCTrackingInfo pcTrackingInfo)
            {
                builder.AppendLine($"Face Detected: {pcTrackingInfo.FaceFound}");
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
    }
} 