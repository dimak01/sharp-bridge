using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;

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
        public Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            return _webSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
        }
        
        /// <inheritdoc/>
        public async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
            string messageType,
            TRequest requestData,
            CancellationToken cancellationToken)
            where TRequest : class
            where TResponse : class
        {
            var request = new VTSApiRequest<TRequest>
            {
                ApiName = "VTubeStudioPublicAPI",
                ApiVersion = "1.0",
                RequestId = Guid.NewGuid().ToString(),
                MessageType = messageType,
                Data = requestData
            };
            
            var json = JsonSerializer.Serialize(request);
            var bytes = Encoding.UTF8.GetBytes(json);
            
            await _webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                cancellationToken);
            
            // Receive response
            var buffer = new byte[4096];
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var responseJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var response = JsonSerializer.Deserialize<VTSApiResponse<TResponse>>(responseJson);
                
                if (response.Data == null)
                {
                    throw new InvalidOperationException($"Response data was null for message type {messageType}");
                }
                
                return response.Data;
            }
            
            throw new InvalidOperationException($"Unexpected message type: {result.MessageType}");
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