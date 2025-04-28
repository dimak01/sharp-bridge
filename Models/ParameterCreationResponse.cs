using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Response model for parameter creation
    /// </summary>
    public class ParameterCreationResponse
    {
        /// <summary>Created parameter name</summary>
        [JsonPropertyName("parameterName")]
        public string ParameterName { get; set; }
    }
} 