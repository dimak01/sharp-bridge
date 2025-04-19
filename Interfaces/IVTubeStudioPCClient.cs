using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Client for communicating with VTube Studio on PC via WebSocket
    /// </summary>
    public interface IVTubeStudioPCClient : IDisposable, IServiceStatsProvider
    {
        /// <summary>
        /// Gets the current state of the WebSocket connection
        /// </summary>
        WebSocketState State { get; }
        
        /// <summary>
        /// Connects to VTube Studio using the configured host and port
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task ConnectAsync(CancellationToken cancellationToken);
        
        /// <summary>
        /// Closes the WebSocket connection gracefully
        /// </summary>
        /// <param name="closeStatus">The close status to use</param>
        /// <param name="statusDescription">Description of why the connection is closing</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken);
        
        /// <summary>
        /// Authenticates with VTube Studio, using stored token if available or requesting a new one
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if authentication was successful, otherwise false</returns>
        Task<bool> AuthenticateAsync(CancellationToken cancellationToken);
        
        /// <summary>
        /// Discovers the port VTube Studio is running on via UDP broadcast
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>The discovered port, or -1 if discovery failed</returns>
        Task<int> DiscoverPortAsync(CancellationToken cancellationToken);
        
        /// <summary>
        /// Sends tracking data to VTube Studio
        /// </summary>
        /// <param name="trackingData">The tracking data to send</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task SendTrackingAsync(PCTrackingInfo trackingData, CancellationToken cancellationToken);
    }
} 