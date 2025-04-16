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
        event EventHandler<TrackingResponse> TrackingDataReceived;
        
        /// <summary>
        /// Starts listening for tracking data from the iPhone
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task RunAsync(CancellationToken cancellationToken);
    }
} 