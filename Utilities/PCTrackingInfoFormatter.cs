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
                int displayCount = verbosity == VerbosityLevel.Detailed ? 10 : 5;
                foreach (var param in parameters.Take(displayCount))
                {
                    builder.AppendLine($"  {param.Id}: {param.Value:F2}" + (param.Weight.HasValue ? $" (weight: {param.Weight:F2})" : ""));
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