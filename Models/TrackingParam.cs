using System;
using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Tracking parameter for injection
    /// </summary>
    public class TrackingParam
    {
        /// <summary>Parameter ID</summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        /// <summary>Optional parameter weight</summary>
        [JsonPropertyName("weight")]
        public double? Weight { get; set; }
        
        /// <summary>Parameter value</summary>
        [JsonPropertyName("value")]
        public double Value { get; set; }
    }
} 