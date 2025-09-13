// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System.ComponentModel;

namespace SharpBridge.Models.Infrastructure
{
    /// <summary>
    /// Represents the current status of the VTube Studio PC Client
    /// </summary>
    public enum PCClientStatus
    {
        /// <summary>
        /// Client is starting up and attempting initial connection
        /// </summary>
        [Description("Preparing PC connection...")]
        Initializing,

        /// <summary>
        /// Client is discovering VTube Studio's port via UDP broadcast
        /// </summary>
        [Description("Discovering VTube Studio port...")]
        DiscoveringPort,

        /// <summary>
        /// Client is attempting to establish WebSocket connection
        /// </summary>
        [Description("Connecting to VTube Studio...")]
        Connecting,

        /// <summary>
        /// Client is authenticating with VTube Studio
        /// </summary>
        [Description("Authenticating with VTube Studio...")]
        Authenticating,

        /// <summary>
        /// Client is successfully connected and authenticated
        /// </summary>
        [Description("[OK] PC connection established")]
        Connected,

        /// <summary>
        /// Client is actively sending tracking data to VTube Studio
        /// </summary>
        [Description("Sending tracking data to VTube Studio...")]
        SendingData,

        /// <summary>
        /// Client failed to discover VTube Studio's port
        /// </summary>
        [Description("[FAIL] Failed to discover VTube Studio port")]
        PortDiscoveryFailed,

        /// <summary>
        /// Client failed to establish WebSocket connection
        /// </summary>
        [Description("[FAIL] Failed to connect to VTube Studio")]
        ConnectionFailed,

        /// <summary>
        /// Client failed to authenticate with VTube Studio
        /// </summary>
        [Description("[FAIL] Failed to authenticate with VTube Studio")]
        AuthenticationFailed,

        /// <summary>
        /// Client failed to initialize or establish connectivity
        /// </summary>
        [Description("[FAIL] PC client initialization failed")]
        InitializationFailed,

        /// <summary>
        /// Client encountered an error while sending tracking data
        /// </summary>
        [Description("[FAIL] Error sending data to VTube Studio")]
        SendError,

        /// <summary>
        /// Client is disconnected and not operational
        /// </summary>
        [Description("PC client disconnected")]
        Disconnected
    }
}