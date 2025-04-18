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
    public class PCTrackingInfoFormatter : IFormatter<PCTrackingInfo>
    {
        private const int PARAM_DISPLAY_COUNT_NORMAL = 10;
        /// <summary>
        /// Formats a PCTrackingInfo object into a display string
        /// </summary>
        public string Format(PCTrackingInfo entity, VerbosityLevel verbosity)
        {
            if (entity == null) return "No PC tracking data";
            
            var builder = new StringBuilder();
            builder.AppendLine("=== VTube Studio Parameters ===");
            builder.AppendLine($"Face Detected: {entity.FaceFound}");
            
            var parameters = entity.Parameters?.ToList() ?? new List<TrackingParam>();
            builder.AppendLine($"Parameter Count: {parameters.Count}");
            
            if (verbosity >= VerbosityLevel.Normal && parameters.Any())
            {
                builder.AppendLine("\nTop Parameters:");
                int displayCount = verbosity == VerbosityLevel.Detailed ? parameters.Count : PARAM_DISPLAY_COUNT_NORMAL;
                
                // Calculate the length of the longest parameter ID for proper alignment
                int maxIdLength = parameters.Take(displayCount)
                    .Select(p => p.Id?.Length ?? 0)
                    .DefaultIfEmpty(0)
                    .Max();
                
                // Add 1 for extra spacing
                maxIdLength += 1;
                
                // Fixed width for the value portion (accounting for "-xxx.xx" format)
                const int valueWidth = 8;
                
                foreach (var param in parameters.Take(displayCount))
                {
                    // Format the value with sign-aware padding to ensure decimal point alignment
                    // Add a space before positive numbers to align with negative numbers
                    string formattedValue = param.Value >= 0 
                        ? $" {param.Value:F2}" 
                        : $"{param.Value:F2}";
                    
                    string valueStr = formattedValue.PadRight(valueWidth);
                    
                    // Format the weight part if available
                    string weightStr = param.Weight.HasValue 
                        ? $"(weight: {(param.Weight >= 0 ? " " : "")}{param.Weight:F2})" 
                        : "";
                    
                    builder.AppendLine($"  {param.Id.PadRight(maxIdLength)}: {valueStr} {weightStr}");
                }
                
                if (parameters.Count > displayCount)
                {
                    builder.AppendLine($"  ... and {parameters.Count - displayCount} more");
                }
            }
            
            return builder.ToString();
        }
    }
} 