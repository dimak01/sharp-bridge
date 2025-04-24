using System;
using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Response indicating authentication status
    /// </summary>
    public class AuthenticationResponse
    {
        /// <summary>Whether authentication was successful</summary>
        [JsonPropertyName("authenticated")]
        public bool Authenticated { get; set; }
        
        /// <summary>Reason for authentication result</summary>
        [JsonPropertyName("reason")]
        public string Reason { get; set; }
    }
} 