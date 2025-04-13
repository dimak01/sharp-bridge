using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Wrapper for WebSocket client to enable easier testing
    /// </summary>
    public interface IWebSocketWrapper : IDisposable
    {
        /// <summary>
        /// Gets the current state of the WebSocket connection
        /// </summary>
        WebSocketState State { get; }
        
        /// <summary>
        /// Connects to a WebSocket server
        /// </summary>
        /// <param name="uri">The URI of the WebSocket server</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
        
        /// <summary>
        /// Sends data over the WebSocket connection
        /// </summary>
        /// <param name="buffer">The buffer containing data to send</param>
        /// <param name="messageType">Type of message being sent</param>
        /// <param name="endOfMessage">Whether this is the end of the message</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken);
        
        /// <summary>
        /// Receives data from the WebSocket connection
        /// </summary>
        /// <param name="buffer">The buffer to receive data into</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Result of the receive operation</returns>
        Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken);
        
        /// <summary>
        /// Closes the WebSocket connection
        /// </summary>
        /// <param name="closeStatus">The close status to use</param>
        /// <param name="statusDescription">Description of why the connection is closing</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken);
    }
} 