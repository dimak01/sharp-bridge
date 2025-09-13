using System;
using System.Text.Json.Serialization;

namespace SharpBridge.Models.Domain
{
    /// <summary>
    /// Represents a blend shape with a key-value pair
    /// </summary>
    public class BlendShape
    {
        /// <summary>The name of the blend shape</summary>
        [JsonPropertyName("k")]
        public string Key { get; set; } = string.Empty;
        
        /// <summary>The value of the blend shape (0-1)</summary>
        [JsonPropertyName("v")]
        public double Value { get; set; }
    }
} 