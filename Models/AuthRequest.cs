using System;
using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Authentication request
    /// </summary>
    public class AuthRequest
    {
        /// <summary>Plugin name</summary>
        [JsonPropertyName("pluginName")]
        public string PluginName { get; set; } = string.Empty;
        
        /// <summary>Plugin developer</summary>
        [JsonPropertyName("pluginDeveloper")]
        public string PluginDeveloper { get; set; } = string.Empty;
        
        /// <summary>Authentication token</summary>
        [JsonPropertyName("authenticationToken")]
        public string AuthenticationToken { get; set; } = string.Empty;
    }
} 