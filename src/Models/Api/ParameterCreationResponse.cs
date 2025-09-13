// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace SharpBridge.Models.Api
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