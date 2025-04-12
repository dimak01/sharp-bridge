using System;
using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// VTube Studio discovery response
    /// </summary>
    public class DiscoveryResponse
    {
        /// <summary>Whether VTube Studio is active</summary>
        [JsonPropertyName("active")]
        public bool Active { get; set; }
        
        /// <summary>Port VTube Studio is listening on</summary>
        [JsonPropertyName("port")]
        public ushort Port { get; set; }
        
        /// <summary>VTube Studio instance ID</summary>
        [JsonPropertyName("instanceID")]
        public string InstanceId { get; set; }
        
        /// <summary>Window title of VTube Studio</summary>
        [JsonPropertyName("windowTitle")]
        public string WindowTitle { get; set; }
    }
} 