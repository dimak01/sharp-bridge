using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Models;

namespace SharpBridge.Interfaces.Infrastructure.Wrappers
{
    /// <summary>
    /// Wrapper for WebSocket client to enable easier testing and standardized VTS API communication
    /// </summary>
    public interface IWebSocketWrapper : IDisposable
    {
        /// <summary>
        /// Gets the current state of the WebSocket connection
        /// </summary>
        WebSocketState State { get; }
        
        /// <summary>
        /// Recreates the internal WebSocket instance to allow reconnection
        /// </summary>
        void RecreateWebSocket();
        
        /// <summary>
        /// Connects to a WebSocket server
        /// </summary>
        /// <param name="uri">The URI of the WebSocket server</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
        
        /// <summary>
        /// Closes the WebSocket connection
        /// </summary>
        /// <param name="closeStatus">The close status to use</param>
        /// <param name="statusDescription">Description of why the connection is closing</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken);
        
        /// <summary>
        /// Sends a request to the VTube Studio API and receives the response
        /// </summary>
        /// <typeparam name="TRequest">Type of the request data</typeparam>
        /// <typeparam name="TResponse">Type of the response data</typeparam>
        /// <param name="messageType">The type of message being sent</param>
        /// <param name="requestData">The request data to send</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Task containing the response data</returns>
        Task<TResponse> SendRequestAsync<TRequest, TResponse>(
            string messageType,
            TRequest requestData,
            CancellationToken cancellationToken)
            where TRequest : class
            where TResponse : class;
    }
} 