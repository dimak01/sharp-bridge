using System;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for receiving tracking data from iPhone VTube Studio
    /// </summary>
    public interface ITrackingReceiver
    {
        /// <summary>
        /// Starts listening for tracking data from the iPhone configured in TrackingReceiverConfig
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to stop listening</param>
        /// <returns>An asynchronous operation that completes when stopped</returns>
        Task RunAsync(CancellationToken cancellationToken);
        
        /// <summary>
        /// Event that fires when tracking data is received
        /// </summary>
        event EventHandler<TrackingResponse> TrackingDataReceived;
    }
} 