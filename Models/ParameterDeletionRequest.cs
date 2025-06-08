using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Parameter deletion request
    /// </summary>
    public class ParameterDeletionRequest
    {
        /// <summary>Name of parameter to delete</summary>
        [JsonPropertyName("parameterName")]
        public string ParameterName { get; set; } = string.Empty;
    }
} 