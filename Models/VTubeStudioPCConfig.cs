using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using SharpBridge.Interfaces;

namespace SharpBridge.Models
{
    /// <summary>
    /// Configuration for VTube Studio PC client
    /// </summary>
    public class VTubeStudioPCConfig : IConfigSection
    {
        // ========================================
        // User-Configurable Settings
        // ========================================

        /// <summary>
        /// Host address of VTube Studio
        /// </summary>
        [Description("VTube Studio PC Host Address")]
        public string Host { get; set; }

        /// <summary>
        /// Port number of VTube Studio API
        /// </summary>
        [Description("VTube Studio PC API Port")]
        public int Port { get; set; }

        /// <summary>
        /// Use port discovery if the specified port doesn't connect
        /// </summary>
        [Description("Enable Automatic Port Discovery")]
        public bool UsePortDiscovery { get; set; }

        // ========================================
        // Internal Settings (Not User-Configurable)
        // ========================================

        /// <summary>
        /// Plugin name for authentication
        /// </summary>
        [JsonIgnore]
        public string PluginName { get; set; }

        /// <summary>
        /// Plugin developer name for authentication
        /// </summary>
        [JsonIgnore]
        public string PluginDeveloper { get; set; }

        /// <summary>
        /// Path to saved authentication token file
        /// </summary>
        [JsonIgnore]
        public string TokenFilePath { get; set; }

        /// <summary>
        /// Connection timeout in milliseconds
        /// </summary>
        [JsonIgnore]
        public int ConnectionTimeoutMs { get; set; }

        /// <summary>
        /// Reconnection delay in milliseconds
        /// </summary>
        [JsonIgnore]
        public int ReconnectionDelayMs { get; set; }

        /// <summary>
        /// The interval in seconds between recovery attempts when services are unhealthy
        /// </summary>
        [JsonIgnore]
        public double RecoveryIntervalSeconds { get; set; }

        /// <summary>
        /// Constructor to ensure all required fields are properly initialized
        /// </summary>
        /// <param name="host">Host address for VTube Studio (default: "localhost")</param>
        /// <param name="port">Port number for VTube Studio API (default: 8001)</param>
        /// <param name="usePortDiscovery">Enable port discovery (default: true)</param>
        public VTubeStudioPCConfig(string host = "localhost", int port = 8001, bool usePortDiscovery = true)
        {
            Host = host;
            Port = port;
            UsePortDiscovery = usePortDiscovery;

            // Set internal defaults
            PluginName = "SharpBridge";
            PluginDeveloper = "Dimak@Shift";
            TokenFilePath = "auth_token.txt";
            ConnectionTimeoutMs = 5000;
            ReconnectionDelayMs = 2000;
            RecoveryIntervalSeconds = 2.0;
        }
    }
}