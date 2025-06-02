namespace SharpBridge.Models
{
    /// <summary>
    /// Represents the current status of the VTube Studio PC Client
    /// </summary>
    public enum PCClientStatus
    {
        /// <summary>
        /// Client is starting up and attempting initial connection
        /// </summary>
        Initializing,
        
        /// <summary>
        /// Client is discovering VTube Studio's port via UDP broadcast
        /// </summary>
        DiscoveringPort,
        
        /// <summary>
        /// Client is attempting to establish WebSocket connection
        /// </summary>
        Connecting,
        
        /// <summary>
        /// Client is authenticating with VTube Studio
        /// </summary>
        Authenticating,
        
        /// <summary>
        /// Client is successfully connected and authenticated
        /// </summary>
        Connected,
        
        /// <summary>
        /// Client is actively sending tracking data to VTube Studio
        /// </summary>
        SendingData,
        
        /// <summary>
        /// Client failed to discover VTube Studio's port
        /// </summary>
        PortDiscoveryFailed,
        
        /// <summary>
        /// Client failed to establish WebSocket connection
        /// </summary>
        ConnectionFailed,
        
        /// <summary>
        /// Client failed to authenticate with VTube Studio
        /// </summary>
        AuthenticationFailed,
        
        /// <summary>
        /// Client failed to initialize or establish connectivity
        /// </summary>
        InitializationFailed,
        
        /// <summary>
        /// Client encountered an error while sending tracking data
        /// </summary>
        SendError,
        
        /// <summary>
        /// Client is disconnected and not operational
        /// </summary>
        Disconnected
    }
} 