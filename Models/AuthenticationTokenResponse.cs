using System;
using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Authentication token response
    /// </summary>
    public class AuthenticationTokenResponse
    {
        /// <summary>Authentication token</summary>
        [JsonPropertyName("authenticationToken")]
        public string AuthenticationToken { get; set; }
    }
} 