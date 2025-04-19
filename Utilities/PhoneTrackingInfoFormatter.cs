using System;
using System.Linq;
using System.Text;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Formatter for PhoneTrackingInfo objects
    /// </summary>
    public class PhoneTrackingInfoFormatter : IFormatter
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
        /// Formats a PhoneTrackingInfo object into a display string
        /// </summary>
        public string Format(IFormattableObject formattableEntity)
        {
            if (formattableEntity == null) return "No tracking data";
            if (!(formattableEntity is PhoneTrackingInfo phoneTrackingInfo))
                throw new ArgumentException(nameof(formattableEntity));
            
            var builder = new StringBuilder();
            builder.AppendLine("=== iPhone Tracking Data === [Alt+O]");
            builder.AppendLine($"Verbosity: {CurrentVerbosity}");
            builder.AppendLine($"Face Detected: {phoneTrackingInfo.FaceFound}");
            
            if (CurrentVerbosity >= VerbosityLevel.Normal && phoneTrackingInfo.Rotation != null)
            {
                builder.AppendLine($"Head Rotation (X,Y,Z): " +
                    $"{phoneTrackingInfo.Rotation.X:F1}°, " +
                    $"{phoneTrackingInfo.Rotation.Y:F1}°, " +
                    $"{phoneTrackingInfo.Rotation.Z:F1}°");
            }
            
            if (CurrentVerbosity >= VerbosityLevel.Normal && phoneTrackingInfo.Position != null)
            {
                builder.AppendLine($"Head Position (X,Y,Z): " +
                    $"{phoneTrackingInfo.Position.X:F1}, " +
                    $"{phoneTrackingInfo.Position.Y:F1}, " +
                    $"{phoneTrackingInfo.Position.Z:F1}");
            }
            
            // Show blend shapes data in detailed mode
            if (CurrentVerbosity >= VerbosityLevel.Normal && phoneTrackingInfo.BlendShapes != null && phoneTrackingInfo.BlendShapes.Count > 0)
            {
                var expressions = new[] { "JawOpen", "EyeBlinkLeft", "EyeBlinkRight", "BrowInnerUp", "MouthSmile" };
                builder.AppendLine("\nKey Expressions:");
                int displayCount = CurrentVerbosity == VerbosityLevel.Detailed ? phoneTrackingInfo.BlendShapes.Count : PARAM_DISPLAY_COUNT_NORMAL;

                // Calculate the length of the longest blend shape key for proper alignment
                int maxKeyLength = phoneTrackingInfo.BlendShapes.Take(displayCount)
                    .Select(s => s?.Key?.Length ?? 0)
                    .DefaultIfEmpty(0)
                    .Max();
                
                // Add 1 for extra spacing
                maxKeyLength += 1;

                foreach (var shape in phoneTrackingInfo.BlendShapes.Take(displayCount))
                {
                    if (shape != null)
                    {
                        var barLength = (int)(shape.Value * 20); // Scale to 0-20 characters
                        var bar = new string('█', barLength) + new string('░', 20 - barLength);
                        builder.AppendLine($"{shape.Key.PadRight(maxKeyLength)}: {bar} {shape.Value:F2}");
                    }
                }
                
                builder.AppendLine($"\nTotal Blend Shapes: {phoneTrackingInfo.BlendShapes.Count}");
            }
            
            return builder.ToString();
        }
        
        // Keep this method for compatibility with the IFormatter interface
        string IFormatter.Format(IFormattableObject entity, VerbosityLevel verbosity)
        {
            // Temporarily use the provided verbosity if needed
            var savedVerbosity = CurrentVerbosity;
            try
            {
                CurrentVerbosity = verbosity;
                return Format(entity);
            }
            finally
            {
                CurrentVerbosity = savedVerbosity;
            }
        }
    }
} 