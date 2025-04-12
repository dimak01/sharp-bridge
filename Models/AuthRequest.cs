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
        public string PluginName { get; set; }
        
        /// <summary>Plugin developer</summary>
        [JsonPropertyName("pluginDeveloper")]
        public string PluginDeveloper { get; set; }
        
        /// <summary>Authentication token</summary>
        [JsonPropertyName("authenticationToken")]
        public string AuthenticationToken { get; set; }
    }
} 