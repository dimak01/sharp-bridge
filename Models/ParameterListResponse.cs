using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Response model for parameter list request
    /// </summary>
    public class ParameterListResponse
    {
        /// <summary>List of parameters</summary>
        [JsonPropertyName("parameters")]
        public IEnumerable<VTSParameter> Parameters { get; set; }
    }
} 