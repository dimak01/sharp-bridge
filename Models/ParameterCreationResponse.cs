using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Response model for parameter creation
    /// </summary>
    public class ParameterCreationResponse
    {
        /// <summary>Name of the created parameter</summary>
        [JsonPropertyName("parameterName")]
        public string ParameterName { get; set; } = string.Empty;
    }
} 