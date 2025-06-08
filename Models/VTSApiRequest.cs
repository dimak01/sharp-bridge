using System;
using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Base request to VTube Studio API
    /// </summary>
    public class VTSApiRequest<T>
    {
        /// <summary>API name</summary>
        [JsonPropertyName("apiName")]
        public string ApiName { get; set; } = string.Empty;
        
        /// <summary>API version</summary>
        [JsonPropertyName("apiVersion")]
        public string ApiVersion { get; set; } = string.Empty;
        
        /// <summary>Request ID</summary>
        [JsonPropertyName("requestID")]
        public string RequestId { get; set; } = string.Empty;
        
        /// <summary>Message type identifier</summary>
        [JsonPropertyName("messageType")]
        public string MessageType { get; set; } = string.Empty;
        
        /// <summary>Request data payload</summary>
        [JsonPropertyName("data")]
        public T Data { get; set; } = default!;
    }
} 