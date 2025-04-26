using System;

namespace SharpBridge.Models
{
    /// <summary>
    /// Represents a VTube Studio parameter definition for creation
    /// </summary>
    public class VTSParameter
    {
        /// <summary>
        /// Name of the parameter in VTube Studio
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Minimum allowed value for the parameter
        /// </summary>
        public double Min { get; }

        /// <summary>
        /// Maximum allowed value for the parameter
        /// </summary>
        public double Max { get; }

        /// <summary>
        /// Default value for the parameter
        /// </summary>
        public double DefaultValue { get; }

        /// <summary>
        /// Creates a new VTube Studio parameter definition
        /// </summary>
        public VTSParameter(string name, double min, double max, double defaultValue)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Min = min;
            Max = max;
            DefaultValue = defaultValue;

            if (min > max)
            {
                throw new ArgumentException($"Min value ({min}) cannot be greater than max value ({max})");
            }
        }
    }
} 