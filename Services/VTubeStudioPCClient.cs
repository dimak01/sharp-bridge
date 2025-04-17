using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services
{
    /// <summary>
    /// Dummy implementation of VTubeStudioPCClient that doesn't do any real work
    /// </summary>
    public class VTubeStudioPCClient : IVTubeStudioPCClient
    {
        private WebSocketState _state = WebSocketState.None;
        private bool _isDisposed;
        
        /// <summary>
        /// Gets the current state of the WebSocket connection
        /// </summary>
        public WebSocketState State => _state;
        
        /// <summary>
        /// Creates a new instance of the VTubeStudioPCClient
        /// </summary>
        public VTubeStudioPCClient()
        {
            Console.WriteLine("Creating dummy VTubeStudioPCClient");
        }
        
        /// <summary>
        /// Connects to VTube Studio (dummy implementation)
        /// </summary>
        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Dummy VTubeStudioPCClient - Connecting");
            _state = WebSocketState.Open;
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Closes the connection to VTube Studio (dummy implementation)
        /// </summary>
        public Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Dummy VTubeStudioPCClient - Closing connection: {statusDescription}");
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Authenticates with VTube Studio (dummy implementation)
        /// </summary>
        public Task<bool> AuthenticateAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Dummy VTubeStudioPCClient - Authenticating");
            // Always return successful authentication
            return Task.FromResult(true);
        }
        
        /// <summary>
        /// Discovers the port VTube Studio is running on (dummy implementation)
        /// </summary>
        public Task<int> DiscoverPortAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Dummy VTubeStudioPCClient - Discovering port");
            // Return a fixed dummy port
            return Task.FromResult(8001);
        }
        
        /// <summary>
        /// Sends tracking data to VTube Studio (dummy implementation)
        /// </summary>
        /// <param name="trackingData">The tracking data to send</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public Task SendTrackingAsync(PCTrackingInfo trackingData, CancellationToken cancellationToken)
        {
            // Only log occasionally to avoid console spam
            if (DateTime.Now.Second % 5 == 0)
            {
                Console.WriteLine($"Dummy VTubeStudioPCClient - Sending tracking data. Face found: {trackingData.FaceFound}");
            }
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Disposes resources
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Nothing to dispose in this dummy implementation
                    Console.WriteLine("Dummy VTubeStudioPCClient - Disposing");
                }
                
                _isDisposed = true;
            }
        }
    }
} 