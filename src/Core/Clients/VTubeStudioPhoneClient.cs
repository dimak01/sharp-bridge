// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Collections.Generic;
using System.IO;
using SharpBridge.Models;
using SharpBridge.Interfaces;
using SharpBridge.Utilities;
using System.Net.NetworkInformation;
using SharpBridge.Interfaces.UI;
using SharpBridge.Interfaces.Infrastructure.Services;
using SharpBridge.Interfaces.Core.Clients;
using SharpBridge.Interfaces.Infrastructure.Wrappers;
using SharpBridge.Interfaces.Configuration.Managers;
using SharpBridge.Interfaces.Infrastructure;
using SharpBridge.Models.Domain;
using SharpBridge.Models.Infrastructure;
using SharpBridge.Models.Configuration;
using SharpBridge.Configuration.Utilities;

namespace SharpBridge.Core.Clients;

/// <summary>
/// Client for receiving tracking data from VTube Studio on iPhone via UDP.
/// </summary>
public class VTubeStudioPhoneClient : IVTubeStudioPhoneClient, IServiceStatsProvider, IDisposable, IInitializable
{
    private readonly IUdpClientWrapper _udpClient;
    private readonly IConfigManager _configManager;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IAppLogger _logger;
    private readonly IFileChangeWatcher? _appConfigWatcher;

    private long _totalFramesReceived = 0;
    private long _failedFrames = 0;
    private readonly DateTime _startTime;
    private PhoneTrackingInfo _lastTrackingData = new PhoneTrackingInfo();
    private PhoneClientStatus _status = PhoneClientStatus.Initializing;
    private string _lastInitializationError = string.Empty;
    private DateTime _lastSuccessfulOperation;
    private bool _configChanged = false;
    private VTubeStudioPhoneClientConfig _config;

    // Health check timeout - consider unhealthy if no successful operation in this many seconds
    private const int HEALTH_TIMEOUT_SECONDS = 3;

    /// <summary>
    /// Event raised when tracking data is received from the iPhone
    /// </summary>
    public event EventHandler<PhoneTrackingInfo> TrackingDataReceived = delegate { };

    /// <summary>
    /// Initializes a new instance of the <see cref="VTubeStudioPhoneClient"/> class.
    /// </summary>
    /// <param name="udpClient">The UDP client to use.</param>
    /// <param name="configManager">The configuration manager for loading configs.</param>
    /// <param name="logger">The logger to use.</param>
    /// <param name="appConfigWatcher">Optional file watcher for application config changes.</param>
    public VTubeStudioPhoneClient(IUdpClientWrapper udpClient, IConfigManager configManager, IAppLogger logger, IFileChangeWatcher? appConfigWatcher = null)
    {
        _udpClient = udpClient ?? throw new ArgumentNullException(nameof(udpClient));
        _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appConfigWatcher = appConfigWatcher;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        _startTime = DateTime.UtcNow;
        _config = _configManager.LoadSectionAsync<VTubeStudioPhoneClientConfig>().GetAwaiter().GetResult();
        if (string.IsNullOrWhiteSpace(_config.IphoneIpAddress))
            throw new InvalidOperationException("iPhone IP address must not be empty in configuration");
        _logger.Debug("VTubeStudioPhoneClient initialized with iPhone IP: {0}, port: {1}", _config.IphoneIpAddress ?? "not configured", _config.IphonePort);

        // Subscribe to application config changes if watcher is provided
        if (_appConfigWatcher != null)
        {
            _appConfigWatcher.FileChanged += OnApplicationConfigChanged;
        }
    }

    /// <summary>
    /// Releases all resources used by the phone client
    /// </summary>
    public void Dispose()
    {
        _logger.Debug("Disposing VTubeStudioPhoneClient");

        // Unsubscribe from file watcher events
        if (_appConfigWatcher != null)
        {
            _appConfigWatcher.FileChanged -= OnApplicationConfigChanged;
        }

        _udpClient.Dispose();
    }

    /// <summary>
    /// Gets the last error that occurred during initialization
    /// </summary>
    public string LastInitializationError => _lastInitializationError;

    /// <summary>
    /// Gets whether the configuration has changed (for testing purposes)
    /// </summary>
    public bool ConfigChanged => _configChanged;

    /// <summary>
    /// Attempts to initialize the client
    /// </summary>
    public async Task<bool> TryInitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            _lastInitializationError = string.Empty;
            _status = PhoneClientStatus.Initializing;

            // Send initial tracking request
            await SendTrackingRequestAsync();

            // Wait for first response
            var received = await ReceiveResponseAsync(cancellationToken);
            if (!received)
            {
                _lastInitializationError = "Failed to receive initial response from iPhone";
                _status = PhoneClientStatus.InitializationFailed;
                return false;
            }

            _lastSuccessfulOperation = DateTime.UtcNow;
            _status = PhoneClientStatus.Connected;

            // Reset config changed flag on successful initialization
            _configChanged = false;

            return true;
        }
        catch (Exception ex)
        {
            _lastInitializationError = ex.Message;
            _status = PhoneClientStatus.InitializationFailed;
            _logger.Error("Initialization failed: {0}", ex.Message);
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
            { "Total Frames", _totalFramesReceived },
            { "Failed Frames", _failedFrames },
            { "Uptime (seconds)", (long)(DateTime.UtcNow - _startTime).TotalSeconds }
        };

        if (_lastTrackingData != null && _totalFramesReceived > 0)
        {
            var timeSinceStart = (DateTime.UtcNow - _startTime).TotalSeconds;
            if (timeSinceStart > 0)
            {
                counters["FPS"] = (long)(_totalFramesReceived / timeSinceStart);
            }
        }

        var isHealthy = _totalFramesReceived > 0 &&
                _lastSuccessfulOperation != default(DateTime) &&
                (DateTime.UtcNow - _lastSuccessfulOperation).TotalSeconds < HEALTH_TIMEOUT_SECONDS &&
                !_configChanged;

        // Only provide current entity if the client is in a healthy/operational state
        var currentEntity = isHealthy ? _lastTrackingData
                           : null;

        return new ServiceStats(
            "Phone Client",
            _status.ToString(),
            currentEntity,
            isHealthy: isHealthy,
            lastSuccessfulOperation: _lastSuccessfulOperation,
            lastError: _lastInitializationError,
            counters: counters);
    }

    /// <summary>
    /// Sends a tracking request to the iPhone.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendTrackingRequestAsync()
    {
        try
        {
            _status = PhoneClientStatus.SendingRequests;
            var request = new
            {
                messageType = "iOSTrackingDataRequest",
                sentBy = "SharpBridge",
                sendForSeconds = _config.SendForSeconds,
                ports = new[] { _config.LocalPort }
            };

            var json = JsonSerializer.Serialize(request);
            var data = Encoding.UTF8.GetBytes(json);
            await _udpClient.SendAsync(data, data.Length, _config.IphoneIpAddress ?? "127.0.0.1", _config.IphonePort);
        }
        catch (Exception ex)
        {
            _failedFrames++;
            _status = PhoneClientStatus.SendError;
            _logger.Error("Error sending tracking request: {0}", ex.Message);
            throw; // Let the orchestrator handle this error
        }
    }

    /// <summary>
    /// Processes received UDP data.
    /// </summary>
    /// <param name="data">The received data bytes.</param>
    private void ProcessReceivedData(byte[] data)
    {
        try
        {
            var json = Encoding.UTF8.GetString(data);
            var response = JsonSerializer.Deserialize<PhoneTrackingInfo>(json, _jsonOptions);

            if (response != null)
            {
                _totalFramesReceived++;
                _lastTrackingData = response;
                _lastSuccessfulOperation = DateTime.UtcNow;
                _status = PhoneClientStatus.ReceivingData;
                TrackingDataReceived?.Invoke(this, response);
            }
        }
        catch (Exception ex)
        {
            _failedFrames++;
            _status = PhoneClientStatus.ProcessingError;
            _logger.Error("Error processing received data: {0}", ex.Message);
        }
    }

    /// <summary>
    /// Attempts to receive a single response from the iPhone
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>True if data was received, false on timeout</returns>
    public async Task<bool> ReceiveResponseAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Use a separate cancellation token with timeout to prevent blocking indefinitely
            using var receiveTimeoutCts = new CancellationTokenSource(_config.ReceiveTimeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, receiveTimeoutCts.Token);

            var result = await _udpClient.ReceiveAsync(linkedCts.Token);
            ProcessReceivedData(result.Buffer);
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // This is just a timeout, not a cancellation request
            return false;
        }
        catch (Exception ex)
        {
            _failedFrames++;
            _status = PhoneClientStatus.ReceiveError;
            _logger.Error("Error receiving data: {0}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Handles application configuration changes
    /// </summary>
    /// <param name="sender">The event sender</param>
    /// <param name="e">The file change event arguments</param>
    private async void OnApplicationConfigChanged(object? sender, FileChangeEventArgs e)
    {
        try
        {
            _logger.Debug("Application config changed, checking if phone client config was affected");

            // Load new phone client config and compare
            var newPhoneConfig = await _configManager.LoadSectionAsync<VTubeStudioPhoneClientConfig>();
            if (!ConfigComparers.PhoneClientConfigsEqual(_config, newPhoneConfig))
            {
                _logger.Info("Phone client configuration changed, updating internal config");
                _config = newPhoneConfig;
                _configChanged = true;

                _logger.Debug("Phone client config updated - IP: {0}:{1}, LocalPort: {2}",
                    _config.IphoneIpAddress ?? "not configured", _config.IphonePort, _config.LocalPort);
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Error handling application config change: {0}", ex.Message);
        }
    }
}