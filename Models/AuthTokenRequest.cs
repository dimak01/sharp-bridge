using System;
using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Authentication token request
    /// </summary>
    public class AuthTokenRequest
    {
        /// <summary>Plugin name</summary>
        [JsonPropertyName("pluginName")]
        public string PluginName { get; set; }
        
        /// <summary>Plugin developer</summary>
        [JsonPropertyName("pluginDeveloper")]
        public string PluginDeveloper { get; set; }
        
        /// <summary>Optional plugin icon (Base64)</summary>
        [JsonPropertyName("pluginIcon")]
        public string PluginIcon { get; set; }
    }
} 