using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Formatter for PhoneTrackingInfo objects with service statistics
    /// </summary>
    public class PhoneTrackingInfoFormatter : IFormatter
    {
        private const int TARGET_COLUMN_COUNT = 4;
        private const int TARGET_ROWS_NORMAL = 13;
        
        private readonly IConsole _console;
        
        /// <summary>
        /// Initializes a new instance of the PhoneTrackingInfoFormatter
        /// </summary>
        /// <param name="console">Console abstraction for getting window dimensions</param>
        public PhoneTrackingInfoFormatter(IConsole console)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
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
            builder.AppendLine(MetricsFormatter.FormatServiceHeader("iPhone Tracking Data", serviceStats.Status, "Alt+O"));
            builder.AppendLine($"Verbosity: {CurrentVerbosity}");
            builder.AppendLine();
            
            // Health status
            builder.AppendLine(MetricsFormatter.FormatHealthStatus(
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
                
                builder.AppendLine(MetricsFormatter.FormatMetrics(totalFrames, failedFrames, fps));
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
        /// Legacy Format method for IFormatter interface compatibility
        /// </summary>
        public string Format(IFormattableObject formattableEntity)
        {
            // For legacy compatibility, create a minimal service stats object
            if (formattableEntity == null) return "No tracking data";
            if (!(formattableEntity is PhoneTrackingInfo))
                throw new ArgumentException("Entity must be of type PhoneTrackingInfo", nameof(formattableEntity));
            
            // Create a basic service stats for legacy calls
            var basicStats = new ServiceStats(
                serviceName: "iPhone Tracking Data",
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
            if (CurrentVerbosity >= VerbosityLevel.Normal && phoneTrackingInfo.BlendShapes != null && phoneTrackingInfo.BlendShapes.Count > 0)
            {
                builder.AppendLine();
                
                // Calculate initial display count based on target rows and columns for normal mode
                int maxDisplayCount = CurrentVerbosity == VerbosityLevel.Detailed 
                    ? phoneTrackingInfo.BlendShapes.Count 
                    : TARGET_ROWS_NORMAL * TARGET_COLUMN_COUNT;

                // Sort blend shapes by name and prepare table data
                var sortedShapes = phoneTrackingInfo.BlendShapes
                    .Where(s => s != null)
                    .OrderBy(s => s.Key)
                    .Take(maxDisplayCount)
                    .ToList();

                var tableRows = sortedShapes.Select(s => (
                    Name: s.Key,
                    ProgressValue: (double)s.Value,
                    DisplayValue: $"{s.Value:F2}"
                ));

                // Use TableFormatter to create the table and get layout mode
                // In single-column mode, limit to TARGET_ROWS_NORMAL for normal verbosity, or show all for detailed
                var singleColumnLimit = CurrentVerbosity == VerbosityLevel.Detailed ? (int?)null : TARGET_ROWS_NORMAL;
                var layoutMode = TableFormatter.AppendTable(builder, "Key Expressions:", tableRows, TARGET_COLUMN_COUNT, _console.WindowWidth, 
                    singleColumnBarWidth: 20, singleColumnMaxItems: singleColumnLimit);

                // Adjust display count based on actual layout mode used
                int actualDisplayCount = layoutMode == TableLayoutMode.MultiColumn 
                    ? maxDisplayCount 
                    : Math.Min(TARGET_ROWS_NORMAL, maxDisplayCount);

                if (CurrentVerbosity == VerbosityLevel.Normal && phoneTrackingInfo.BlendShapes.Count > actualDisplayCount)
                {
                    builder.AppendLine($"  ... and {phoneTrackingInfo.BlendShapes.Count - actualDisplayCount} more");
                }

                builder.AppendLine();
                builder.AppendLine($"Total Blend Shapes: {phoneTrackingInfo.BlendShapes.Count}");
            }
        }
    }
} 