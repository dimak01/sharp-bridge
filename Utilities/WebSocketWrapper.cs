using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Interfaces;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Implementation of the WebSocket wrapper around ClientWebSocket
    /// </summary>
    public class WebSocketWrapper : IWebSocketWrapper
    {
        private readonly ClientWebSocket _webSocket;
        private bool _disposed;
        
        /// <summary>
        /// Creates a new instance of the WebSocket wrapper
        /// </summary>
        public WebSocketWrapper()
        {
            _webSocket = new ClientWebSocket();
        }
        
        /// <inheritdoc/>
        public WebSocketState State => _webSocket.State;
        
        /// <inheritdoc/>
        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            return _webSocket.ConnectAsync(uri, cancellationToken);
        }
        
        /// <inheritdoc/>
        public Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            return _webSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
        }
        
        /// <inheritdoc/>
        public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            return _webSocket.ReceiveAsync(buffer, cancellationToken);
        }
        
        /// <inheritdoc/>
        public Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            return _webSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
        }
        
        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Releases resources used by the WebSocket wrapper
        /// </summary>
        /// <param name="disposing">Whether this is being called from Dispose</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
                
            if (disposing)
            {
                _webSocket.Dispose();
            }
            
            _disposed = true;
        }
    }
} 