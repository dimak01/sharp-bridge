// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Infrastructure.Wrappers;
using SharpBridge.Models;
using SharpBridge.Models.Api;

namespace SharpBridge.Infrastructure.Wrappers
{
    /// <summary>
    /// Implementation of the WebSocket wrapper around ClientWebSocket
    /// </summary>
    public class WebSocketWrapper : IWebSocketWrapper
    {
        private ClientWebSocket _webSocket;
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

        /// <summary>
        /// Recreates the internal ClientWebSocket instance to allow reconnection after the socket has been closed or aborted
        /// </summary>
        public void RecreateWebSocket()
        {
            if (!_disposed)
            {
                _webSocket?.Dispose();
                _webSocket = new ClientWebSocket();
            }
        }

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

        private async Task SendMessageAsync(string json, CancellationToken cancellationToken)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            await _webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                cancellationToken);
        }

        private async Task<string> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[16384];
            var messageBuffer = new List<byte>();
            WebSocketReceiveResult result;

            do
            {
                result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                messageBuffer.AddRange(new ArraySegment<byte>(buffer, 0, result.Count));
            } while (!result.EndOfMessage);

            if (result.MessageType != WebSocketMessageType.Text)
            {
                throw new InvalidOperationException($"Unexpected message type: {result.MessageType}");
            }

            return Encoding.UTF8.GetString(messageBuffer.ToArray());
        }

        private VTSApiRequest<TRequest> CreateRequest<TRequest>(string messageType, TRequest requestData)
            where TRequest : class
        {
            return new VTSApiRequest<TRequest>
            {
                ApiName = "VTubeStudioPublicAPI",
                ApiVersion = "1.0",
                RequestId = Guid.NewGuid().ToString(),
                MessageType = messageType,
                Data = requestData
            };
        }

        /// <summary>
        /// Sends a request to the VTube Studio API and receives the response
        /// </summary>
        /// <typeparam name="TRequest">The type of the request data</typeparam>
        /// <typeparam name="TResponse">The type of the response data</typeparam>
        /// <param name="messageType">The VTube Studio API message type</param>
        /// <param name="requestData">The request data to send</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>The response data from the API</returns>
        /// <exception cref="InvalidOperationException">Thrown when response data is null</exception>
        public async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
            string messageType,
            TRequest requestData,
            CancellationToken cancellationToken)
            where TRequest : class
            where TResponse : class
        {
            var request = CreateRequest(messageType, requestData);
            var json = JsonSerializer.Serialize(request);

            await SendMessageAsync(json, cancellationToken);
            var responseJson = await ReceiveMessageAsync(cancellationToken);

            var response = JsonSerializer.Deserialize<VTSApiResponse<TResponse>>(responseJson);
            if (response?.Data == null)
            {
                throw new InvalidOperationException($"Response data was null for message type {messageType}");
            }

            return response.Data;
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