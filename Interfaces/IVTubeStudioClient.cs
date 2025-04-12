using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Models;

namespace SharpBridge.Services
{
    /// <summary>
    /// Interface for communication with VTube Studio on PC
    /// </summary>
    public interface IVTubeStudioClient
    {
        /// <summary>
        /// Starts the client and connects to VTube Studio
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to stop the client</param>
        /// <returns>An asynchronous operation that completes when stopped</returns>
        Task RunAsync(CancellationToken cancellationToken);
        
        /// <summary>
        /// Sends tracking parameters to VTube Studio
        /// </summary>
        /// <param name="parameters">The parameters to send</param>
        /// <param name="faceFound">Whether a face is detected</param>
        /// <returns>An asynchronous operation that completes when sent</returns>
        Task SendTrackingAsync(IEnumerable<TrackingParam> parameters, bool faceFound);
    }
} 