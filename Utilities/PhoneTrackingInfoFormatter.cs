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
    public class PhoneTrackingInfoFormatter : IFormatter<PhoneTrackingInfo>
    {
        /// <summary>
        /// Formats a PhoneTrackingInfo object into a display string
        /// </summary>
        public string Format(PhoneTrackingInfo entity, VerbosityLevel verbosity)
        {
            if (entity == null) return "No tracking data";
            
            var builder = new StringBuilder();
            builder.AppendLine("=== iPhone Tracking Data ===");
            builder.AppendLine($"Face Detected: {entity.FaceFound}");
            
            if (verbosity >= VerbosityLevel.Normal && entity.Rotation != null)
            {
                builder.AppendLine($"Head Rotation (X,Y,Z): " +
                    $"{entity.Rotation.X:F1}°, " +
                    $"{entity.Rotation.Y:F1}°, " +
                    $"{entity.Rotation.Z:F1}°");
            }
            
            if (verbosity >= VerbosityLevel.Normal && entity.Position != null)
            {
                builder.AppendLine($"Head Position (X,Y,Z): " +
                    $"{entity.Position.X:F1}, " +
                    $"{entity.Position.Y:F1}, " +
                    $"{entity.Position.Z:F1}");
            }
            
            // Show blend shapes data in detailed mode
            if (verbosity == VerbosityLevel.Detailed && entity.BlendShapes != null && entity.BlendShapes.Count > 0)
            {
                var expressions = new[] { "JawOpen", "EyeBlinkLeft", "EyeBlinkRight", "BrowInnerUp", "MouthSmile" };
                builder.AppendLine("\nKey Expressions:");
                
                foreach (var expression in expressions)
                {
                    var shape = entity.BlendShapes.FirstOrDefault(s => s.Key == expression);
                    if (shape != null)
                    {
                        var barLength = (int)(shape.Value * 20); // Scale to 0-20 characters
                        var bar = new string('█', barLength) + new string('░', 20 - barLength);
                        builder.AppendLine($"{expression.PadRight(15)}: {bar} {shape.Value:F2}");
                    }
                }
                
                builder.AppendLine($"\nTotal Blend Shapes: {entity.BlendShapes.Count}");
            }
            
            return builder.ToString();
        }
    }
} 