using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services
{
    /// <summary>
    /// Client for communicating with VTube Studio on PC via WebSocket
    /// </summary>
    public class VTubeStudioPCClient : IVTubeStudioPCClient, IAuthTokenProvider, IServiceStatsProvider
    {
        private readonly IAppLogger _logger;
        private readonly VTubeStudioPCConfig _config;
        private readonly IWebSocketWrapper _webSocket;
        private bool _isDisposed;
        private DateTime _startTime;
        private int _messagesSent;
        private int _lastSuccessfulSend;
        private PCTrackingInfo _lastTrackingData;
        private int _connectionAttempts;
        private int _failedConnections;
        private int _lastSuccessfulConnection;
        private string _authToken;
        
        /// <summary>
        /// Gets the current state of the WebSocket connection
        /// </summary>
        public WebSocketState State => _webSocket.State;
        
        /// <summary>
        /// Gets the current authentication token
        /// </summary>
        public string Token => _authToken;
        
        /// <summary>
        /// Gets the client configuration
        /// </summary>
        public VTubeStudioPCConfig Config => _config;
        
        /// <summary>
        /// Creates a new instance of the VTubeStudioPCClient
        /// </summary>
        /// <param name="logger">Application logger</param>
        /// <param name="config">VTube Studio PC configuration</param>
        /// <param name="webSocket">WebSocket wrapper for communication</param>
        public VTubeStudioPCClient(IAppLogger logger, VTubeStudioPCConfig config, IWebSocketWrapper webSocket)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            _startTime = DateTime.Now;
            
            // Try to load existing auth token
            LoadAuthToken();
        }
        
        /// <summary>
        /// Connects to VTube Studio using the configured host and port
        /// </summary>
        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (_webSocket.State != WebSocketState.None)
            {
                throw new InvalidOperationException($"Cannot connect in state {_webSocket.State}");
            }
            
            _connectionAttempts++;
            _logger.Info("Connecting to VTube Studio at {0}:{1}", _config.Host, _config.Port);
            
            try
            {
                var uri = new Uri($"ws://{_config.Host}:{_config.Port}");
                await _webSocket.ConnectAsync(uri, cancellationToken);
                _lastSuccessfulConnection = Environment.TickCount;
                _logger.Info("Connected to VTube Studio");
            }
            catch (Exception ex)
            {
                _failedConnections++;
                _logger.Error("Failed to connect to VTube Studio: {0}", ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Closes the WebSocket connection gracefully
        /// </summary>
        public async Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                return;
            }
            
            _logger.Info("Closing connection to VTube Studio: {0}", statusDescription);
            
            try
            {
                await _webSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
                _logger.Info("Connection closed");
            }
            catch (Exception ex)
            {
                _logger.Error("Error closing connection: {0}", ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Authenticates with VTube Studio, using stored token if available or requesting a new one
        /// </summary>
        public async Task<bool> AuthenticateAsync(CancellationToken cancellationToken)
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("Must be connected to authenticate");
            }
            
            _logger.Info("Authenticating with VTube Studio");
            
            try
            {
                if (string.IsNullOrEmpty(_authToken))
                {
                    _logger.Info("No token found, requesting new authentication token");
                    await GetTokenAsync(cancellationToken);
                }
                
                // Authenticate with token
                var authRequest = new AuthRequest
                {
                    PluginName = _config.PluginName,
                    PluginDeveloper = _config.PluginDeveloper,
                    AuthenticationToken = _authToken
                };
                
                var authResponse = await SendRequestAsync<AuthRequest, AuthenticationResponse>(
                    "AuthenticationRequest", authRequest, cancellationToken);
                
                if (!authResponse.Authenticated)
                {
                    _logger.Info("Authentication failed: {0}. Requesting new token.", authResponse.Reason);
                    await GetTokenAsync(cancellationToken);
                    
                    // Try authenticating again with new token
                    authRequest.AuthenticationToken = _authToken;
                    authResponse = await SendRequestAsync<AuthRequest, AuthenticationResponse>(
                        "AuthenticationRequest", authRequest, cancellationToken);
                }
                
                _logger.Info("Authentication result: {0}", authResponse.Authenticated);
                return authResponse.Authenticated;
            }
            catch (Exception ex)
            {
                _logger.Error("Authentication failed: {0}", ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Discovers the port VTube Studio is running on via UDP broadcast
        /// </summary>
        public async Task<int> DiscoverPortAsync(CancellationToken cancellationToken)
        {
            if (!_config.UsePortDiscovery)
            {
                return _config.Port;
            }
            
            _logger.Info("Discovering VTube Studio port");
            
            // TODO: Implement UDP discovery
            // For now, return the configured port
            _logger.Info("Found port {0}", _config.Port);
            return _config.Port;
        }
        
        /// <summary>
        /// Sends tracking data to VTube Studio
        /// </summary>
        public async Task SendTrackingAsync(PCTrackingInfo trackingData, CancellationToken cancellationToken)
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("Must be connected to send tracking data");
            }
            
            try
            {
                var request = new InjectParamsRequest
                {
                    FaceFound = trackingData.FaceFound,
                    Mode = "set",
                    ParameterValues = trackingData.Parameters
                };
                
                await SendRequestAsync<InjectParamsRequest, object>(
                    "InjectParameterDataRequest", request, cancellationToken);
                
                _messagesSent++;
                _lastSuccessfulSend = Environment.TickCount;
                _lastTrackingData = trackingData;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to send tracking data: {0}", ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Gets the current service statistics
        /// </summary>
        public IServiceStats GetServiceStats()
        {
            var counters = new Dictionary<string, long>
            {
                ["MessagesSent"] = _messagesSent,
                ["ConnectionAttempts"] = _connectionAttempts,
                ["FailedConnections"] = _failedConnections,
                ["UptimeSeconds"] = (int)(DateTime.Now - _startTime).TotalSeconds
            };
            
            return new ServiceStats(
                serviceName: "VTubeStudioPCClient",
                status: _webSocket.State.ToString(),
                currentEntity: _lastTrackingData,
                counters: counters
            );
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
                    _webSocket?.Dispose();
                    _logger.Debug("VTubeStudioPCClient disposed");
                }
                
                _isDisposed = true;
            }
        }
        
        private async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
            string messageType, TRequest requestData, CancellationToken cancellationToken)
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
        
        private void LoadAuthToken()
        {
            try
            {
                if (File.Exists(_config.TokenFilePath))
                {
                    _authToken = File.ReadAllText(_config.TokenFilePath);
                    _logger.Debug("Loaded authentication token from {0}", _config.TokenFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning("Failed to load authentication token: {0}", ex.Message);
            }
        }
        
        private void SaveAuthToken()
        {
            try
            {
                File.WriteAllText(_config.TokenFilePath, _authToken);
                _logger.Debug("Saved authentication token to {0}", _config.TokenFilePath);
            }
            catch (Exception ex)
            {
                _logger.Warning("Failed to save authentication token: {0}", ex.Message);
            }
        }
        
        #region Token Management
        /// <inheritdoc />
        public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("Must be connected to get token");
            }
            
            _logger.Info("Requesting new authentication token");
            
            try
            {
                var tokenRequest = new AuthTokenRequest
                {
                    PluginName = _config.PluginName,
                    PluginDeveloper = _config.PluginDeveloper
                };
                
                var response = await SendRequestAsync<AuthTokenRequest, AuthenticationTokenResponse>(
                    "AuthenticationTokenRequest", tokenRequest, cancellationToken);
                
                _authToken = response.AuthenticationToken;
                await SaveTokenAsync(_authToken);
                return _authToken;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to get authentication token: {0}", ex.Message);
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task SaveTokenAsync(string token)
        {
            _authToken = token;
            try
            {
                File.WriteAllText(_config.TokenFilePath, token);
                _logger.Debug("Saved authentication token to {0}", _config.TokenFilePath);
            }
            catch (Exception ex)
            {
                _logger.Warning("Failed to save authentication token: {0}", ex.Message);
            }
        }
        
        /// <inheritdoc />
        public async Task ClearTokenAsync()
        {
            _authToken = null;
            try
            {
                if (File.Exists(_config.TokenFilePath))
                {
                    File.Delete(_config.TokenFilePath);
                    _logger.Debug("Cleared authentication token from {0}", _config.TokenFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning("Failed to clear authentication token: {0}", ex.Message);
            }
        }
        #endregion
        
        #region Authentication
        /// <inheritdoc />
        public async Task<bool> AuthenticateAsync(string token, CancellationToken cancellationToken)
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("Must be connected to authenticate");
            }
            
            _logger.Info("Authenticating with VTube Studio");
            
            try
            {
                var authRequest = new AuthRequest
                {
                    PluginName = _config.PluginName,
                    PluginDeveloper = _config.PluginDeveloper,
                    AuthenticationToken = token
                };
                
                var response = await SendRequestAsync<AuthRequest, AuthenticationResponse>(
                    "AuthenticationRequest", authRequest, cancellationToken);
                
                _logger.Info("Authentication result: {0}", response.Authenticated);
                return response.Authenticated;
            }
            catch (Exception ex)
            {
                _logger.Error("Authentication failed: {0}", ex.Message);
                return false;
            }
        }
        #endregion
    }
} 