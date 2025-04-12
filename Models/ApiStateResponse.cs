using System;
using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// VTube Studio API state response
    /// </summary>
    public class ApiStateResponse
    {
        /// <summary>Whether the API is active</summary>
        [JsonPropertyName("active")]
        public bool Active { get; set; }
        
        /// <summary>VTube Studio version</summary>
        [JsonPropertyName("vTubeStudioVersion")]
        public string VTubeStudioVersion { get; set; }
        
        /// <summary>Whether the current session is authenticated</summary>
        [JsonPropertyName("currentSessionAuthenticated")]
        public bool CurrentSessionAuthenticated { get; set; }
    }
} 