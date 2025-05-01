using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Data container for input parameter list response
    /// </summary>
    public class InputParameterListResponse
    {
        /// <summary>Whether a model is currently loaded</summary>
        [JsonPropertyName("modelLoaded")]
        public bool ModelLoaded { get; set; }

        /// <summary>Name of the currently loaded model</summary>
        [JsonPropertyName("modelName")]
        public string ModelName { get; set; }

        /// <summary>Unique ID of the currently loaded model</summary>
        [JsonPropertyName("modelID")]
        public string ModelId { get; set; }

        /// <summary>List of custom parameters</summary>
        [JsonPropertyName("customParameters")]
        public IEnumerable<VTSParameter> CustomParameters { get; set; }

        /// <summary>List of default parameters</summary>
        [JsonPropertyName("defaultParameters")]
        public IEnumerable<VTSParameter> DefaultParameters { get; set; }
    }
} 