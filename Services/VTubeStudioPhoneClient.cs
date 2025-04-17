using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Collections.Generic;
using SharpBridge.Models;
using SharpBridge.Interfaces;
using SharpBridge.Utilities;

namespace SharpBridge.Services;

/// <summary>
/// Client for receiving tracking data from VTube Studio on iPhone via UDP.
/// </summary>
public class VTubeStudioPhoneClient : IVTubeStudioPhoneClient, IServiceStatsProvider<PhoneTrackingInfo>, IDisposable
{
    private readonly IUdpClientWrapper _udpClient;
    private readonly VTubeStudioPhoneClientConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;
    
    private long _totalFramesReceived = 0;
    private long _failedFrames = 0;
    private DateTime _startTime;
    private PhoneTrackingInfo _lastTrackingData;
    private string _status = "Initializing";
    
    /// <summary>
    /// Event triggered when new tracking data is received.
    /// </summary>
    public event EventHandler<PhoneTrackingInfo> TrackingDataReceived;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="VTubeStudioPhoneClient"/> class.
    /// </summary>
    /// <param name="udpClient">The UDP client to use.</param>
    /// <param name="config">The configuration for the phone client.</param>
    public VTubeStudioPhoneClient(IUdpClientWrapper udpClient, VTubeStudioPhoneClientConfig config)
    {
        _udpClient = udpClient ?? throw new ArgumentNullException(nameof(udpClient));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        
        if (string.IsNullOrWhiteSpace(_config.IphoneIpAddress))
        {
            throw new ArgumentException("iPhone IP address cannot be null or empty", nameof(config));
        }
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        _startTime = DateTime.UtcNow;
    }

    public void Dispose()
    {
        _udpClient.Dispose();
    }

    /// <summary>
    /// Gets the current service statistics
    /// </summary>
    public ServiceStats<PhoneTrackingInfo> GetServiceStats()
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
        
        return new ServiceStats<PhoneTrackingInfo>(
            "Phone Client", 
            _status,
            _lastTrackingData,
            counters);
    }

    /// <summary>
    /// Starts listening for tracking data from the iPhone.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the operation is cancelled.</returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _status = "Running";
        var nextRequestTime = DateTime.UtcNow;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Check if it's time to send another request
                if (DateTime.UtcNow >= nextRequestTime)
                {
                    await SendTrackingRequestAsync();
                    nextRequestTime = DateTime.UtcNow.AddSeconds(_config.RequestIntervalSeconds);
                }

                // Continuously try to receive data (with a short timeout)
                try
                {
                    // Use a separate cancellation token with timeout to prevent blocking indefinitely
                    using var receiveTimeoutCts = new CancellationTokenSource(_config.ReceiveTimeoutMs);
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, receiveTimeoutCts.Token);
                    
                    var result = await _udpClient.ReceiveAsync(linkedCts.Token);
                    ProcessReceivedData(result.Buffer);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"Socket error in tracking receiver: Timed out");
                    // This is just a timeout, not a cancellation request - continue
                    continue;
                }
            }
            catch (SocketException ex)
            {
                _status = $"Error: {ex.Message}";
                Console.WriteLine($"Socket error in tracking receiver: {ex.Message}");
                await Task.Delay(1000, cancellationToken);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
            {
                _status = $"Error: {ex.Message}";
                Console.WriteLine($"Error in tracking receiver: {ex.Message}");
                await Task.Delay(1000, cancellationToken);
            }
        }
        
        _status = "Stopped";
    }

    /// <summary>
    /// Sends a tracking request to the iPhone.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task SendTrackingRequestAsync()
    {
        try
        {
            var request = new
            {
                messageType = "iOSTrackingDataRequest",
                sentBy = "SharpBridge",
                sendForSeconds = _config.SendForSeconds,
                ports = new[] { _config.LocalPort }
            };

            var json = JsonSerializer.Serialize(request);
            var data = Encoding.UTF8.GetBytes(json);
            await _udpClient.SendAsync(data, data.Length, _config.IphoneIpAddress, _config.IphonePort);
        }
        catch (Exception ex)
        {
            _status = $"Error sending request: {ex.Message}";
            Console.WriteLine($"Error sending tracking request: {ex.Message}");
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
                TrackingDataReceived?.Invoke(this, response);
            }
        }
        catch (Exception ex)
        {
            _failedFrames++;
            _status = $"Error processing data: {ex.Message}";
            Console.WriteLine($"Error processing received data: {ex.Message}");
        }
    }
} 