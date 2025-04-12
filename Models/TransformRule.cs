using System;
using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Parameter transformation configuration
    /// </summary>
    public class TransformRule
    {
        /// <summary>Parameter name</summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        /// <summary>Transformation expression</summary>
        [JsonPropertyName("func")]
        public string Func { get; set; }
        
        /// <summary>Minimum parameter value</summary>
        [JsonPropertyName("min")]
        public double Min { get; set; }
        
        /// <summary>Maximum parameter value</summary>
        [JsonPropertyName("max")]
        public double Max { get; set; }
        
        /// <summary>Default parameter value</summary>
        [JsonPropertyName("defaultValue")]
        public double DefaultValue { get; set; }
    }
} 