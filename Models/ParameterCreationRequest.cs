using System;
using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Parameter creation request
    /// </summary>
    public class ParameterCreationRequest
    {
        /// <summary>Parameter name</summary>
        [JsonPropertyName("parameterName")]
        public string ParameterName { get; set; }
        
        /// <summary>Parameter explanation</summary>
        [JsonPropertyName("explanation")]
        public string Explanation { get; set; }
        
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