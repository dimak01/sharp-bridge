using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Client for communicating with VTube Studio on PC
    /// </summary>
    public interface IVTubeStudioPCClient : IDisposable
    {
        VTubeStudioPCConfig Config { get; }
        string Token { get; }
        /// <summary>
        /// Gets the current state of the WebSocket connection
        /// </summary>
        WebSocketState State { get; }
        
        /// <summary>
        /// Connects to VTube Studio
        /// </summary>
        Task ConnectAsync(CancellationToken cancellationToken);
        
        /// <summary>
        /// Closes the connection to VTube Studio
        /// </summary>
        Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken);
        
        /// <summary>
        /// Authenticates with VTube Studio using the provided token
        /// </summary>
        /// <param name="token">The authentication token to use</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if authentication was successful</returns>
        Task<bool> AuthenticateAsync(string token, CancellationToken cancellationToken);
        
        /// <summary>
        /// Discovers the port VTube Studio is running on
        /// </summary>
        Task<int> DiscoverPortAsync(CancellationToken cancellationToken);
        
        /// <summary>
        /// Sends tracking data to VTube Studio
        /// </summary>
        Task SendTrackingAsync(PCTrackingInfo trackingData, CancellationToken cancellationToken);
        
        /// <summary>
        /// Gets the current service statistics
        /// </summary>
        IServiceStats GetServiceStats();
    }
} 