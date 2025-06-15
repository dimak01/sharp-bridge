using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Tests.Services
{
    /// <summary>
    /// Mock implementation of IWebSocketWrapper for testing purposes
    /// </summary>
    public class MockWebSocketWrapper : IWebSocketWrapper
    {
        private WebSocketState _state = WebSocketState.None;
        private readonly ConcurrentQueue<byte[]> _responseQueue = new();
        private bool _disposed;
        
        /// <summary>
        /// Gets or sets the current state of the WebSocket connection
        /// </summary>
        public WebSocketState State 
        { 
            get => _state;
            set => _state = value;
        }
        
        /// <summary>
        /// Recreates the internal WebSocket instance to allow reconnection
        /// </summary>
        public void RecreateWebSocket()
        {
            if (!_disposed)
            {
                State = WebSocketState.None;
                _responseQueue.Clear();
            }
        }
        
        /// <summary>
        /// Enqueues a response to be returned on the next SendRequestAsync call
        /// </summary>
        /// <param name="response">The response object to serialize and enqueue</param>
        public void EnqueueResponse<T>(T response)
        {
            var wrappedResponse = new VTSApiResponse<T>
            {
                ApiName = "VTubeStudioPublicAPI",
                ApiVersion = "1.0",
                RequestId = Guid.NewGuid().ToString(),
                MessageType = typeof(T).Name,
                Data = response,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            
            var json = JsonSerializer.Serialize(wrappedResponse);
            var bytes = Encoding.UTF8.GetBytes(json);
            _responseQueue.Enqueue(bytes);
        }
        
        /// <summary>
        /// Enqueues a raw response to be returned on the next SendRequestAsync call
        /// </summary>
        /// <param name="bytes">The raw bytes to enqueue</param>
        public void EnqueueRawResponse(byte[] bytes)
        {
            _responseQueue.Enqueue(bytes);
        }
        
        /// <summary>
        /// Connects to a WebSocket server
        /// </summary>
        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (State != WebSocketState.None)
            {
                throw new InvalidOperationException($"Cannot connect in state {State}");
            }
            
            State = WebSocketState.Open;
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Sends a request and receives a response
        /// </summary>
        public Task<TResponse> SendRequestAsync<TRequest, TResponse>(
            string messageType,
            TRequest requestData,
            CancellationToken cancellationToken)
            where TRequest : class
            where TResponse : class
        {
            if (State != WebSocketState.Open)
            {
                throw new InvalidOperationException($"Cannot send in state {State}");
            }
            
            if (!_responseQueue.TryDequeue(out var responseBytes))
            {
                throw new InvalidOperationException("No response queued");
            }
            
            var responseJson = Encoding.UTF8.GetString(responseBytes);
            var response = JsonSerializer.Deserialize<VTSApiResponse<TResponse>>(responseJson);
            
            if (response?.Data == null)
            {
                throw new InvalidOperationException($"Response data was null for message type {messageType}");
            }
            
            return Task.FromResult(response.Data);
        }
        
        /// <summary>
        /// Closes the WebSocket connection
        /// </summary>
        public Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            if (State != WebSocketState.Open)
            {
                return Task.CompletedTask;
            }
            
            State = WebSocketState.Closed;
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
            if (!_disposed)
            {
                if (disposing)
                {
                    _responseQueue.Clear();
                    State = WebSocketState.Closed;
                }
                
                _disposed = true;
            }
        }
    }
} 