// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;

namespace SharpBridge.Models.Api
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