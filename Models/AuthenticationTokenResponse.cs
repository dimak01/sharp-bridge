using System;
using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Response containing authentication token
    /// </summary>
    public class AuthenticationTokenResponse
    {
        /// <summary>Authentication token</summary>
        [JsonPropertyName("authenticationToken")]
        public string AuthenticationToken { get; set; } = string.Empty;
    }
} 