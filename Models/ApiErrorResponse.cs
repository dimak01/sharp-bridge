using System;
using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// API error response
    /// </summary>
    public class ApiErrorResponse
    {
        /// <summary>Error ID</summary>
        [JsonPropertyName("errorID")]
        public ushort ErrorId { get; set; }
        
        /// <summary>Error message</summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
} 