using System;
using System.Text.Json.Serialization;

namespace SharpBridge.Models.Api
{
    /// <summary>
    /// Base response from VTube Studio API
    /// </summary>
    public class VTSApiResponse<T>
    {
        /// <summary>API name</summary>
        [JsonPropertyName("apiName")]
        public string ApiName { get; set; } = string.Empty;
        
        /// <summary>API version</summary>
        [JsonPropertyName("apiVersion")]
        public string ApiVersion { get; set; } = string.Empty;
        
        /// <summary>Timestamp of the response</summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
        
        /// <summary>Response message type</summary>
        [JsonPropertyName("messageType")]
        public string MessageType { get; set; } = string.Empty;
        
        /// <summary>Request ID this response corresponds to</summary>
        [JsonPropertyName("requestID")]
        public string RequestId { get; set; } = string.Empty;
        
        /// <summary>Response data payload</summary>
        [JsonPropertyName("data")]
        public T Data { get; set; } = default!;
    }
} 