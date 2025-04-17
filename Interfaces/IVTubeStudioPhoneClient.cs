using System;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Client for receiving tracking data from VTube Studio on iPhone
    /// </summary>
    public interface IVTubeStudioPhoneClient : IDisposable
    {
        /// <summary>
        /// Event raised when tracking data is received from iPhone
        /// </summary>
        event EventHandler<PhoneTrackingInfo> TrackingDataReceived;
        
        /// <summary>
        /// Sends a tracking request to the iPhone
        /// </summary>
        /// <returns>Task representing the asynchronous operation</returns>
        Task SendTrackingRequestAsync();
        
        /// <summary>
        /// Attempts to receive a single response from the iPhone
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if data was received, false on timeout</returns>
        Task<bool> ReceiveResponseAsync(CancellationToken cancellationToken);
    }
} 