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
    /// Implementation of port discovery service using UDP broadcast
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
                // Listen for broadcast
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
                var response = JsonSerializer.Deserialize<DiscoveryResponse>(json);
                
                if (response != null && response.Active)
                {
                    // Verify this is actually VTube Studio
                    if (string.IsNullOrEmpty(response.InstanceId) || 
                        string.IsNullOrEmpty(response.WindowTitle) ||
                        !response.WindowTitle.Contains("VTube Studio", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.Warning("Found service but it doesn't appear to be VTube Studio");
                        return null;
                    }
                    
                    _logger.Info("Found VTube Studio (Instance: {0}, Title: {1}) on port {2}", 
                        response.InstanceId, response.WindowTitle, response.Port);
                    return response;
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
    }
} 