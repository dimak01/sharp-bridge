using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SharpBridge.Models
{
    /// <summary>
    /// Configuration for VTube Studio PC client
    /// </summary>
    public class VTubeStudioPCConfig
    {
        // ========================================
        // User-Configurable Settings
        // ========================================

        /// <summary>
        /// Host address of VTube Studio, defaults to localhost
        /// </summary>
        [Description("Host Address")]
        public string Host { get; set; } = "localhost";

        /// <summary>
        /// Port number of VTube Studio API, defaults to 8001
        /// </summary>
        [Description("Port Number")]
        public int Port { get; set; } = 8001;

        /// <summary>
        /// Use port discovery if the specified port doesn't connect
        /// </summary>
        [Description("Use Port Discovery")]
        public bool UsePortDiscovery { get; set; } = true;

        // ========================================
        // Internal Settings (Not User-Configurable)
        // ========================================

        /// <summary>
        /// Plugin name for authentication, defaults to SharpBridge
        /// </summary>
        [JsonIgnore]
        public string PluginName { get; set; } = "SharpBridge";

        /// <summary>
        /// Plugin developer name for authentication
        /// </summary>
        [JsonIgnore]
        public string PluginDeveloper { get; set; } = "Dimak@Shift";

        /// <summary>
        /// Path to saved authentication token file, defaults to "auth_token.txt"
        /// </summary>
        [JsonIgnore]
        public string TokenFilePath { get; set; } = "auth_token.txt";

        /// <summary>
        /// Connection timeout in milliseconds, defaults to 1000 (1 second)
        /// </summary>
        [JsonIgnore]
        public int ConnectionTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// Reconnection delay in milliseconds, defaults to 2000 (2 seconds)
        /// </summary>
        [JsonIgnore]
        public int ReconnectionDelayMs { get; set; } = 2000;

        /// <summary>
        /// The interval in seconds between recovery attempts when services are unhealthy
        /// </summary>
        [JsonIgnore]
        public double RecoveryIntervalSeconds { get; set; } = 2.0;
    }
}