using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Parameter injection request
    /// </summary>
    public class InjectParamsRequest
    {
        /// <summary>Whether a face is found</summary>
        [JsonPropertyName("faceFound")]
        public bool FaceFound { get; set; }
        
        /// <summary>Injection mode</summary>
        [JsonPropertyName("mode")]
        public string Mode { get; set; }
        
        /// <summary>Parameter values to inject</summary>
        [JsonPropertyName("parameterValues")]
        public List<TrackingParam> ParameterValues { get; set; }
    }
} 