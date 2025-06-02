namespace SharpBridge.Models
{
    /// <summary>
    /// Represents the current status of the VTube Studio Phone Client
    /// </summary>
    public enum PhoneClientStatus
    {
        /// <summary>
        /// Client is starting up and attempting initial connection
        /// </summary>
        Initializing,
        
        /// <summary>
        /// Client has successfully established connectivity with the iPhone
        /// </summary>
        Connected,
        
        /// <summary>
        /// Client is actively sending tracking requests to the iPhone
        /// </summary>
        SendingRequests,
        
        /// <summary>
        /// Client is receiving and processing tracking data from the iPhone
        /// </summary>
        ReceivingData,
        
        /// <summary>
        /// Client failed to initialize or establish connectivity
        /// </summary>
        InitializationFailed,
        
        /// <summary>
        /// Client encountered an error while sending requests
        /// </summary>
        SendError,
        
        /// <summary>
        /// Client encountered an error while receiving data
        /// </summary>
        ReceiveError,
        
        /// <summary>
        /// Client encountered an error while processing received data
        /// </summary>
        ProcessingError,
        
        /// <summary>
        /// Client is disconnected and not operational
        /// </summary>
        Disconnected
    }
} 