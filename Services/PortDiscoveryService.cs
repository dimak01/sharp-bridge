using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services
{
    /// <summary>
    /// Implementation of port discovery service using UDP broadcast listening
    /// </summary>
    public class PortDiscoveryService : IPortDiscoveryService
    {
        private readonly IAppLogger _logger;
        private readonly IUdpClientWrapper _udpClient;
        private const int VTubeStudioDiscoveryPort = 47779;

        public PortDiscoveryService(IAppLogger logger, IUdpClientWrapper udpClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _udpClient = udpClient ?? throw new ArgumentNullException(nameof(udpClient));
        }

        public async Task<DiscoveryResponse> DiscoverAsync(int timeoutMs, CancellationToken cancellationToken)
        {
            try
            {
                _logger.Debug("Listening for VTube Studio broadcast on port {0}", VTubeStudioDiscoveryPort);

                // Listen for broadcast response (VTube Studio broadcasts every 2 seconds)
                var receiveTask = _udpClient.ReceiveAsync(cancellationToken);
                var timeoutTask = Task.Delay(timeoutMs, cancellationToken);
                
                var completedTask = await Task.WhenAny(receiveTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    _logger.Warning("Port discovery timed out after {0}ms", timeoutMs);
                    return null;
                }
                
                var result = await receiveTask;
                var json = Encoding.UTF8.GetString(result.Buffer);
                
                _logger.Debug("Received broadcast data: {0}", json);
                
                var response = JsonSerializer.Deserialize<VTSApiResponse<DiscoveryResponse>>(json);
                
                if (response?.Data != null && response.Data.Active)
                {
                    // Verify this is actually VTube Studio
                    if (string.IsNullOrEmpty(response.Data.InstanceId) || 
                        string.IsNullOrEmpty(response.Data.WindowTitle) ||
                        !response.Data.WindowTitle.StartsWith("VTube Studio", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.Warning("Found service but it doesn't appear to be VTube Studio");
                        return null;
                    }
                    
                    _logger.Info("Found VTube Studio (Instance: {0}, Title: {1}) on port {2}", 
                        response.Data.InstanceId, response.Data.WindowTitle, response.Data.Port);
                    return response.Data;
                }
                
                _logger.Warning("No active VTube Studio instance found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error("Error during port discovery: {0}", ex.Message);
                return null;
            }
        }
        
        public void Dispose()
        {
            _udpClient?.Dispose();
        }
    }
} 