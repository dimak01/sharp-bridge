using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Parameter deletion request
    /// </summary>
    public class ParameterDeletionRequest
    {
        /// <summary>Parameter name to delete</summary>
        [JsonPropertyName("parameterName")]
        public string ParameterName { get; set; }
    }
} 