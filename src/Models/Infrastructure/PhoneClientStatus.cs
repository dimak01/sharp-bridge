using System.ComponentModel;

namespace SharpBridge.Models.Infrastructure
{
    /// <summary>
    /// Represents the current status of the VTube Studio Phone Client
    /// </summary>
    public enum PhoneClientStatus
    {
        /// <summary>
        /// Client is starting up and attempting initial connection
        /// </summary>
        [Description("Preparing iPhone connection...")]
        Initializing,

        /// <summary>
        /// Client has successfully established connectivity with the iPhone
        /// </summary>
        [Description("[OK] iPhone connection established")]
        Connected,

        /// <summary>
        /// Client is actively sending tracking requests to the iPhone
        /// </summary>
        [Description("Sending tracking requests to iPhone...")]
        SendingRequests,

        /// <summary>
        /// Client is receiving and processing tracking data from the iPhone
        /// </summary>
        [Description("Receiving tracking data from iPhone...")]
        ReceivingData,

        /// <summary>
        /// Client failed to initialize or establish connectivity
        /// </summary>
        [Description("[FAIL] iPhone client initialization failed")]
        InitializationFailed,

        /// <summary>
        /// Client encountered an error while sending requests
        /// </summary>
        [Description("[FAIL] Error sending requests to iPhone")]
        SendError,

        /// <summary>
        /// Client encountered an error while receiving data
        /// </summary>
        [Description("[FAIL] Error receiving data from iPhone")]
        ReceiveError,

        /// <summary>
        /// Client encountered an error while processing received data
        /// </summary>
        [Description("[FAIL] Error processing iPhone data")]
        ProcessingError,

        /// <summary>
        /// Client is disconnected and not operational
        /// </summary>
        [Description("iPhone client disconnected")]
        Disconnected
    }
}