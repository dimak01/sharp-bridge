// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Text.Json.Serialization;

namespace SharpBridge.Models.Api
{
    /// <summary>
    /// Response indicating authentication status
    /// </summary>
    public class AuthenticationResponse
    {
        /// <summary>Whether authentication was successful</summary>
        [JsonPropertyName("authenticated")]
        public bool Authenticated { get; set; }
        
        /// <summary>Reason for authentication response</summary>
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }
} 