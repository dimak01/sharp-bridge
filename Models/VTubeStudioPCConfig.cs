using System;

namespace SharpBridge.Models
{
    /// <summary>
    /// Configuration for VTube Studio PC client
    /// </summary>
    public class VTubeStudioPCConfig
    {
        /// <summary>
        /// Host address of VTube Studio, defaults to localhost
        /// </summary>
        public string Host { get; set; } = "localhost";
        
        /// <summary>
        /// Port number of VTube Studio API, defaults to 8001
        /// </summary>
        public int Port { get; set; } = 8001;
        
        /// <summary>
        /// Plugin name for authentication, defaults to SharpBridge
        /// </summary>
        public string PluginName { get; set; } = "SharpBridge";
        
        /// <summary>
        /// Plugin developer name for authentication
        /// </summary>
        public string PluginDeveloper { get; set; } = "SharpBridge Developer";
        
        /// <summary>
        /// Path to saved authentication token file, defaults to "auth_token.txt"
        /// </summary>
        public string TokenFilePath { get; set; } = "auth_token.txt";
        
        /// <summary>
        /// Connection timeout in milliseconds, defaults to 1000 (1 second)
        /// </summary>
        public int ConnectionTimeoutMs { get; set; } = 1000;
        
        /// <summary>
        /// Reconnection delay in milliseconds, defaults to 2000 (2 seconds)
        /// </summary>
        public int ReconnectionDelayMs { get; set; } = 2000;
        
        /// <summary>
        /// Use port discovery if the specified port doesn't connect
        /// </summary>
        public bool UsePortDiscovery { get; set; } = true;
        
        /// <summary>
        /// The interval in seconds between recovery attempts when services are unhealthy
        /// </summary>
        public double RecoveryIntervalSeconds { get; set; } = 2.0;
    }
} 