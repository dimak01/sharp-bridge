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
        /// Formats a PCTrackingInfo object into a display string
        /// </summary>
        public string Format(IFormattableObject formattableEntity)
        {
            if (formattableEntity == null) return "No PC tracking data";
            if (!(formattableEntity is PCTrackingInfo pcTrackingInfo))
                throw new ArgumentException("Entity must be of type PCTrackingInfo", nameof(formattableEntity));
            
            var builder = new StringBuilder();
            AppendHeader(builder, pcTrackingInfo);
            
            var parameters = pcTrackingInfo.Parameters?.ToList() ?? new List<TrackingParam>();
            
            if (CurrentVerbosity >= VerbosityLevel.Normal && parameters.Any())
            {
                AppendParameters(builder, parameters);
            }
            
            return builder.ToString();
        }
        
        /// <summary>
        /// Appends the header information to the string builder
        /// </summary>
        private void AppendHeader(StringBuilder builder, PCTrackingInfo entity)
        {
            builder.AppendLine("=== VTube Studio Parameters === [Alt+P]");
            builder.AppendLine($"Verbosity: {CurrentVerbosity}");
            builder.AppendLine($"Face Detected: {entity.FaceFound}");
            
            var parameterCount = entity.Parameters?.Count() ?? 0;
            builder.AppendLine($"Parameter Count: {parameterCount}");
        }
        
        /// <summary>
        /// Appends the parameter information to the string builder
        /// </summary>
        private void AppendParameters(StringBuilder builder, List<TrackingParam> parameters)
        {
            builder.AppendLine("\nTop Parameters:");
            int displayCount = CurrentVerbosity == VerbosityLevel.Detailed ? parameters.Count : PARAM_DISPLAY_COUNT_NORMAL;
            
            // Calculate the length of the longest parameter ID for proper alignment
            int maxIdLength = CalculateMaxIdLength(parameters, displayCount);
            
            // Display parameters
            for (int i = 0; i < Math.Min(displayCount, parameters.Count); i++)
            {
                AppendParameterInfo(builder, parameters[i], maxIdLength);
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
        private void AppendParameterInfo(StringBuilder builder, TrackingParam param, int maxIdLength)
        {
            string id = param.Id ?? string.Empty;
            string formattedValue = FormatNumericValue(param.Value);
            string progressBar = CreateProgressBar(param.Value, param.Min, param.Max);
            string weightPart = FormatWeightPart(param);
            string rangeInfo = FormatRangeInfo(param);
            
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
            
            // Format with sign-aware padding
            return $"weight: {FormatNumericValue(param.Weight.Value)}";
        }
        
        /// <summary>
        /// Formats the range information of a parameter
        /// </summary>
        private string FormatRangeInfo(TrackingParam param)
        {
            string minStr = $"min: {FormatNumericValue(param.Min)}";
            string maxStr = $"max: {FormatNumericValue(param.Max)}";
            string defaultStr = $"default: {FormatNumericValue(param.DefaultValue)}";
            
            return $"{minStr}, {maxStr}, {defaultStr}";
        }
    }
} 