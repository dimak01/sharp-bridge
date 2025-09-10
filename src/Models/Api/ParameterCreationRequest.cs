using System;
using System.Text.Json.Serialization;

namespace SharpBridge.Models.Api
{
    /// <summary>
    /// Parameter creation request
    /// </summary>
    public class ParameterCreationRequest
    {
        /// <summary>Name of the parameter to create</summary>
        [JsonPropertyName("parameterName")]
        public string ParameterName { get; set; } = string.Empty;
        
        /// <summary>Explanation of the parameter</summary>
        [JsonPropertyName("explanation")]
        public string Explanation { get; set; } = string.Empty;
        
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