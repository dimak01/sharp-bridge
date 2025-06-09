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
using SharpBridge.Utilities;

namespace SharpBridge.Services
{
    /// <summary>
    /// Client for communicating with VTube Studio on PC via WebSocket
    /// </summary>
    public class VTubeStudioPCClient : IVTubeStudioPCClient, IAuthTokenProvider, IServiceStatsProvider, IInitializable
    {
        private readonly IAppLogger _logger;
        private readonly VTubeStudioPCConfig _config;
        private readonly IWebSocketWrapper _webSocket;
        private readonly IPortDiscoveryService _portDiscoveryService;
        private bool _isDisposed;
        private DateTime _startTime;
        private int _messagesSent;
        private PCTrackingInfo _lastTrackingData = new PCTrackingInfo();
        private int _connectionAttempts;
        private int _failedConnections;
        private string _authToken = string.Empty;
        private string _lastInitializationError = string.Empty;
        private DateTime _lastSuccessfulOperation;
        private PCClientStatus _status = PCClientStatus.Initializing;
        
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
        /// Gets the last error that occurred during initialization
        /// </summary>
        public string LastInitializationError => _lastInitializationError;
        
        /// <summary>
        /// Creates a new instance of the VTubeStudioPCClient
        /// </summary>
        /// <param name="logger">Application logger</param>
        /// <param name="config">VTube Studio PC configuration</param>
        /// <param name="webSocket">WebSocket wrapper for communication</param>
        /// <param name="portDiscoveryService">Service for discovering VTube Studio's port</param>
        public VTubeStudioPCClient(
            IAppLogger logger, 
            VTubeStudioPCConfig config, 
            IWebSocketWrapper webSocket,
            IPortDiscoveryService portDiscoveryService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            _portDiscoveryService = portDiscoveryService ?? throw new ArgumentNullException(nameof(portDiscoveryService));
            _startTime = DateTime.Now;
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
                _logger.Info("Connected to VTube Studio");
            }
            catch (Exception ex)
            {
                _failedConnections++;
                _status = PCClientStatus.ConnectionFailed;
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
                _status = PCClientStatus.Disconnected;
                _logger.Info("Connection closed");
            }
            catch (Exception ex)
            {
                _status = PCClientStatus.Disconnected;
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
                
                var authResponse = await _webSocket.SendRequestAsync<AuthRequest, AuthenticationResponse>(
                    "AuthenticationRequest", authRequest, cancellationToken);
                
                if (!authResponse.Authenticated)
                {
                    _logger.Info("Authentication failed: {0}. Requesting new token.", authResponse.Reason);
                    await GetTokenAsync(cancellationToken);
                    
                    // Try authenticating again with new token
                    authRequest.AuthenticationToken = _authToken;
                    authResponse = await _webSocket.SendRequestAsync<AuthRequest, AuthenticationResponse>(
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
            
            var response = await _portDiscoveryService.DiscoverAsync(_config.ConnectionTimeoutMs, cancellationToken);
            if (response != null)
            {
                return response.Port;
            }
            
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
                _status = PCClientStatus.SendingData;
                
                var request = new InjectParamsRequest
                {
                    FaceFound = trackingData.FaceFound,
                    Mode = "set",
                    ParameterValues = trackingData.Parameters
                };
                
                await _webSocket.SendRequestAsync<InjectParamsRequest, object>(
                    "InjectParameterDataRequest", request, cancellationToken);
                
                _messagesSent++;
                _lastTrackingData = trackingData;
                _lastSuccessfulOperation = DateTime.UtcNow;
                _status = PCClientStatus.Connected; // Reset status back to Connected after successful send
            }
            catch (Exception ex)
            {
                _status = PCClientStatus.SendError;
                _logger.Error("Failed to send tracking data: {0}", ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Attempts to initialize the client
        /// </summary>
        public async Task<bool> TryInitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                _status = PCClientStatus.Initializing;
                _logger.Info("Attempting to initialize VTube Studio PC Client...");
                
                // Load existing auth token first
                LoadAuthToken();
                
                // Recreate WebSocket if it's in a closed or aborted state
                if (_webSocket.State == WebSocketState.Closed || _webSocket.State == WebSocketState.Aborted)
                {
                    _logger.Info("WebSocket is in closed/aborted state, recreating...");
                    _webSocket.RecreateWebSocket();
                }
                
                // Discover port
                _status = PCClientStatus.DiscoveringPort;
                var port = await DiscoverPortAsync(cancellationToken);
                if (port == 0)
                {
                    _logger.Error("Failed to discover VTube Studio port");
                    _lastInitializationError = "Failed to discover VTube Studio port";
                    _status = PCClientStatus.PortDiscoveryFailed;
                    return false;
                }
                
                // Connect
                _status = PCClientStatus.Connecting;
                await ConnectAsync(cancellationToken);
                
                // Authenticate
                _status = PCClientStatus.Authenticating;
                var authSuccess = await AuthenticateAsync(cancellationToken);
                if (!authSuccess)
                {
                    _logger.Error("Failed to authenticate with VTube Studio");
                    _lastInitializationError = "Failed to authenticate with VTube Studio";
                    _status = PCClientStatus.AuthenticationFailed;
                    return false;
                }
                
                _logger.Info("VTube Studio PC Client initialized successfully");
                _lastInitializationError = string.Empty;
                _lastSuccessfulOperation = DateTime.UtcNow;
                _status = PCClientStatus.Connected;
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to initialize VTube Studio PC Client: {ex.Message}");
                _lastInitializationError = ex.Message;
                _status = PCClientStatus.InitializationFailed;
                return false;
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
            
            var isHealthy = _status == PCClientStatus.Connected || _status == PCClientStatus.SendingData;
            
            return new ServiceStats(
                serviceName: "VTubeStudioPCClient",
                status: _status.ToString(),
                currentEntity: _lastTrackingData,
                isHealthy: isHealthy,
                lastSuccessfulOperation: _lastSuccessfulOperation,
                lastError: _lastInitializationError,
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
                    try
                    {
                        _webSocket?.Dispose();
                        _logger.Debug("VTubeStudioPCClient disposed");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Error disposing WebSocket: {0}", ex.Message);
                    }
                }
                
                _isDisposed = true;
            }
        }
        
        #region Token Management
        /// <inheritdoc />
        public void LoadAuthToken()
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
                
                var response = await _webSocket.SendRequestAsync<AuthTokenRequest, AuthenticationTokenResponse>(
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
            _authToken = string.Empty;
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
                
                var response = await _webSocket.SendRequestAsync<AuthRequest, AuthenticationResponse>(
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