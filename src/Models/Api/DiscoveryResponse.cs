// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Text.Json.Serialization;

namespace SharpBridge.Models.Api
{
    /// <summary>
    /// VTube Studio discovery response
    /// </summary>
    public class DiscoveryResponse
    {
        /// <summary>Whether VTube Studio is active</summary>
        [JsonPropertyName("active")]
        public bool Active { get; set; }
        
        /// <summary>Port VTube Studio is listening on</summary>
        [JsonPropertyName("port")]
        public ushort Port { get; set; }
        
        /// <summary>VTS instance ID</summary>
        [JsonPropertyName("instanceID")]
        public string InstanceId { get; set; } = string.Empty;
        
        /// <summary>Window title of VTS instance</summary>
        [JsonPropertyName("windowTitle")]
        public string WindowTitle { get; set; } = string.Empty;
    }
} 