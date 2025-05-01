using System;
using System.Text.Json.Serialization;

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
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Name of the plugin that added this parameter
        /// </summary>
        [JsonPropertyName("addedBy")]
        public string AddedBy { get; set;  }

        /// <summary>
        /// Minimum allowed value for the parameter
        /// </summary>
        [JsonPropertyName("min")]
        public double Min { get; set;  }

        /// <summary>
        /// Maximum allowed value for the parameter
        /// </summary>
        [JsonPropertyName("max")]
        public double Max { get; set;  }

        /// <summary>
        /// Default value for the parameter
        /// </summary>
        [JsonPropertyName("defaultValue")]
        public double DefaultValue { get; set;  }

        /// <summary>
        /// Creates a new VTube Studio parameter definition
        /// </summary>
        public VTSParameter(string name, double min, double max, double defaultValue, string addedBy = "SharpBridge")
        {
            Name = name; // ?? throw new ArgumentNullException(nameof(name));
            Min = min;
            Max = max;
            DefaultValue = defaultValue;
            AddedBy = addedBy;

            if (min > max)
            {
                throw new ArgumentException($"Min value ({min}) cannot be greater than max value ({max})");
            }
        }
    }
} 