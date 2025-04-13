using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using SharpBridge.Models;
using SharpBridge.Interfaces;

namespace SharpBridge.Services;

/// <summary>
/// Receives tracking data from iPhone VTube Studio via UDP.
/// </summary>
public class TrackingReceiver : ITrackingReceiver, IDisposable
{
    private readonly IUdpClientWrapper _udpClient;
    private readonly TrackingReceiverConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;
    
    /// <summary>
    /// Event triggered when new tracking data is received.
    /// </summary>
    public event EventHandler<TrackingResponse> TrackingDataReceived;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TrackingReceiver"/> class.
    /// </summary>
    /// <param name="udpClient">The UDP client to use.</param>
    /// <param name="config">The configuration for the tracking receiver.</param>
    public TrackingReceiver(IUdpClientWrapper udpClient, TrackingReceiverConfig config)
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
    }

    public void Dispose()
    {
        _udpClient.Dispose();
    }

    /// <summary>
    /// Starts listening for tracking data from the iPhone configured in TrackingReceiverConfig.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the operation is cancelled.</returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var nextRequestTime = DateTime.UtcNow;
        var buffer = new byte[_config.ReceiveBufferSize];

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

                // Check if there's data to receive (with a short timeout)
                if (!_udpClient.Poll(_config.PollTimeoutMs * 1000, SelectMode.SelectRead))
                {
                    continue;
                }

                // Check if there's actually data available
                if (_udpClient.Available == 0)
                {
                    continue;
                }

                var result = await _udpClient.ReceiveAsync(cancellationToken);
                ProcessReceivedData(result.Buffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in tracking receiver: {ex.Message}");
                await Task.Delay(1000, cancellationToken);
            }
        }
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
                sendForSeconds = _config.RequestIntervalSeconds,
                ports = new[] { _config.LocalPort }
            };

            var json = JsonSerializer.Serialize(request);
            var data = Encoding.UTF8.GetBytes(json);
            await _udpClient.SendAsync(data, data.Length, _config.IphoneIpAddress, _config.IphonePort);
        }
        catch (Exception ex)
        {
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
            var response = JsonSerializer.Deserialize<TrackingResponse>(json, _jsonOptions);

            if (response != null)
            {
                TrackingDataReceived?.Invoke(this, response);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing received data: {ex.Message}");
        }
    }
} 