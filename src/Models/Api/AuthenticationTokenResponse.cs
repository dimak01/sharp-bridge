// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Text.Json.Serialization;

namespace SharpBridge.Models.Api
{
    /// <summary>
    /// Response containing authentication token
    /// </summary>
    public class AuthenticationTokenResponse
    {
        /// <summary>Authentication token</summary>
        [JsonPropertyName("authenticationToken")]
        public string AuthenticationToken { get; set; } = string.Empty;
    }
} 