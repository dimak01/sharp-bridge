using System;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Models;

namespace SharpBridge.Services
{
    /// <summary>
    /// Interface for receiving tracking data from iPhone VTube Studio
    /// </summary>
    public interface ITrackingReceiver
    {
        /// <summary>
        /// Starts listening for tracking data from the specified iPhone IP
        /// </summary>
        /// <param name="iphoneIp">IP address of the iPhone</param>
        /// <param name="cancellationToken">Cancellation token to stop listening</param>
        /// <returns>An asynchronous operation that completes when stopped</returns>
        Task RunAsync(string iphoneIp, CancellationToken cancellationToken);
        
        /// <summary>
        /// Event that fires when tracking data is received
        /// </summary>
        event EventHandler<TrackingResponse> TrackingDataReceived;
    }
} 