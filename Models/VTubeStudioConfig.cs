using System;

namespace SharpBridge.Models
{
    /// <summary>
    /// Configuration for VTube Studio client
    /// </summary>
    public class VTubeStudioConfig
    {
        /// <summary>
        /// Host address of VTube Studio, defaults to localhost
        /// </summary>
        public string Host { get; init; } = "localhost";
        
        /// <summary>
        /// Port number of VTube Studio API, defaults to 8001
        /// </summary>
        public int Port { get; init; } = 8001;
        
        /// <summary>
        /// Plugin name for authentication, defaults to SharpBridge
        /// </summary>
        public string PluginName { get; init; } = "SharpBridge";
        
        /// <summary>
        /// Plugin developer name for authentication
        /// </summary>
        public string PluginDeveloper { get; init; } = "SharpBridge Developer";
        
        /// <summary>
        /// Path to saved authentication token file, defaults to "auth_token.txt"
        /// </summary>
        public string TokenFilePath { get; init; } = "auth_token.txt";
        
        /// <summary>
        /// Connection timeout in milliseconds, defaults to 5000 (5 seconds)
        /// </summary>
        public int ConnectionTimeoutMs { get; init; } = 5000;
        
        /// <summary>
        /// Reconnection delay in milliseconds, defaults to 2000 (2 seconds)
        /// </summary>
        public int ReconnectionDelayMs { get; init; } = 2000;
        
        /// <summary>
        /// Use port discovery if the specified port doesn't connect
        /// </summary>
        public bool UsePortDiscovery { get; init; } = true;
    }
} 