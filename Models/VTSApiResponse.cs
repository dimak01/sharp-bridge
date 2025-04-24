using System;
using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Base response from VTube Studio API
    /// </summary>
    public class VTSApiResponse<T>
    {
        /// <summary>API name</summary>
        [JsonPropertyName("apiName")]
        public string ApiName { get; set; }
        
        /// <summary>API version</summary>
        [JsonPropertyName("apiVersion")]
        public string ApiVersion { get; set; }
        
        /// <summary>Timestamp of the response</summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
        
        /// <summary>Message type identifier</summary>
        [JsonPropertyName("messageType")]
        public string MessageType { get; set; }
        
        /// <summary>Request ID this response is for</summary>
        [JsonPropertyName("requestID")]
        public string RequestId { get; set; }
        
        /// <summary>Response data payload</summary>
        [JsonPropertyName("data")]
        public T Data { get; set; }
    }
} 