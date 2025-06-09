using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Request to inject parameter values into VTube Studio
    /// </summary>
    public class InjectParamsRequest
    {
        /// <summary>Whether a face is found</summary>
        [JsonPropertyName("faceFound")]
        public bool FaceFound { get; set; }
        
        /// <summary>Injection mode</summary>
        [JsonPropertyName("mode")]
        public string Mode { get; set; } = string.Empty;
        
        /// <summary>Parameter values to inject</summary>
        [JsonPropertyName("parameterValues")]
        public IEnumerable<TrackingParam> ParameterValues { get; set; } = Array.Empty<TrackingParam>();
    }
} 