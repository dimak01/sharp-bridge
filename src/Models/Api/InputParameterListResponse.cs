// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using SharpBridge.Models.Domain;

namespace SharpBridge.Models.Api
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
        public string ModelName { get; set; } = string.Empty;

        /// <summary>Unique ID of the currently loaded model</summary>
        [JsonPropertyName("modelID")]
        public string ModelId { get; set; } = string.Empty;

        /// <summary>List of custom parameters</summary>
        [JsonPropertyName("customParameters")]
        public IEnumerable<VTSParameter> CustomParameters { get; set; } = Array.Empty<VTSParameter>();

        /// <summary>List of default parameters</summary>
        [JsonPropertyName("defaultParameters")]
        public IEnumerable<VTSParameter> DefaultParameters { get; set; } = Array.Empty<VTSParameter>();
    }
}