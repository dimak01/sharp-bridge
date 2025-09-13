using System;
using System.Text.Json.Serialization;
using SharpBridge.Infrastructure.Utilities;
using SharpBridge.Utilities;

namespace SharpBridge.Models.Domain
{
    /// <summary>
    /// JSON DTO for transformation rules
    /// </summary>
    public class ParameterRuleDefinition
    {
        /// <summary>Name of the transformation rule</summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Mathematical expression for the transformation</summary>
        [JsonPropertyName("func")]
        public string Func { get; set; } = string.Empty;

        /// <summary>Minimum value for the parameter</summary>
        [JsonPropertyName("min")]
        public double Min { get; set; }

        /// <summary>Maximum value for the parameter</summary>
        [JsonPropertyName("max")]
        public double Max { get; set; }

        /// <summary>Default value for the parameter</summary>
        [JsonPropertyName("defaultValue")]
        public double DefaultValue { get; set; }

        /// <summary>Interpolation method for parameter transformation</summary>
        [JsonPropertyName("interpolation")]
        [JsonConverter(typeof(InterpolationConverter))]
        public IInterpolationDefinition? Interpolation { get; set; }
    }
}