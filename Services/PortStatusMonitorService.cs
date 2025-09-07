using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services
{
    /// <summary>
    /// Background port status monitoring service
    /// </summary>
    public class PortStatusMonitorService : IPortStatusMonitorService
    {
        private readonly IFirewallAnalyzer _firewallAnalyzer;
        private readonly IConfigManager _configManager;

        /// <summary>
        /// Initializes a new instance of the PortStatusMonitor
        /// </summary>
        /// <param name="firewallAnalyzer">Firewall analyzer for connectivity analysis</param>
        /// <param name="configManager">Configuration manager for getting connection settings</param>
        public PortStatusMonitorService(IFirewallAnalyzer firewallAnalyzer, IConfigManager configManager)
        {
            _firewallAnalyzer = firewallAnalyzer ?? throw new ArgumentNullException(nameof(firewallAnalyzer));
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
        }

        /// <summary>
        /// Gets the current network status for all monitored connections
        /// </summary>
        /// <returns>Current network status with port and firewall information</returns>
        public async Task<NetworkStatus> GetNetworkStatusAsync()
        {
            var status = new NetworkStatus();

            // Get real configuration values
            var phoneConfig = await _configManager.LoadSectionAsync<VTubeStudioPhoneClientConfig>();
            var pcConfig = await _configManager.LoadSectionAsync<VTubeStudioPCConfig>();

            var iphoneHost = phoneConfig.IphoneIpAddress;
            var iphonePort = phoneConfig.IphonePort.ToString();
            var pcHost = pcConfig.Host ?? "localhost"; // Default if not configured
            var pcPort = pcConfig.Port.ToString();
            var discoveryPort = "47779"; // VTube Studio discovery port
            var usePortDiscovery = pcConfig.UsePortDiscovery;

            // Check iPhone connection
            status.IPhone = await CheckIPhoneConnectionAsync(iphoneHost, iphonePort);

            // Check PC connection
            status.PC = await CheckPCConnectionAsync(pcHost, pcPort, discoveryPort, usePortDiscovery);

            status.LastUpdated = DateTime.UtcNow;
            return status;
        }

        private async Task<IPhoneConnectionStatus> CheckIPhoneConnectionAsync(string host, string port)
        {
            var status = new IPhoneConnectionStatus
            {
                LastChecked = DateTime.UtcNow
            };

            // Get local port from configuration
            var phoneConfig = await _configManager.LoadSectionAsync<VTubeStudioPhoneClientConfig>();
            var localPort = phoneConfig.LocalPort.ToString();

            // Check if firewall allows inbound traffic to our local port (for receiving)
            var inboundAnalysis = _firewallAnalyzer.AnalyzeFirewallRules(
                localPort: localPort,
                remoteHost: "0.0.0.0", // Any remote host (wildcard)
                remotePort: "0",        // Any remote port
                protocol: "UDP");
            status.LocalPortOpen = inboundAnalysis.IsAllowed;
            status.InboundFirewallAnalysis = inboundAnalysis;

            // Check if firewall allows outbound traffic to iPhone (for sending)
            var outboundAnalysis = _firewallAnalyzer.AnalyzeFirewallRules(
                localPort: null,
                remoteHost: host,
                remotePort: port,
                protocol: "UDP");
            status.OutboundAllowed = outboundAnalysis.IsAllowed;
            status.OutboundFirewallAnalysis = outboundAnalysis;

            return status;
        }

        private async Task<PCConnectionStatus> CheckPCConnectionAsync(string host, string port, string discoveryPort, bool usePortDiscovery)
        {
            var status = new PCConnectionStatus
            {
                LastChecked = DateTime.UtcNow
            };

            // Check WebSocket port connectivity
            var webSocketAnalysis = _firewallAnalyzer.AnalyzeFirewallRules(
                localPort: null,
                remoteHost: host,
                remotePort: port,
                protocol: "TCP");
            status.WebSocketAllowed = webSocketAnalysis.IsAllowed;
            status.WebSocketFirewallAnalysis = webSocketAnalysis;

            // Check discovery port if enabled
            if (usePortDiscovery)
            {
                var discoveryAnalysis = _firewallAnalyzer.AnalyzeFirewallRules(
                    localPort: null,
                    remoteHost: host,
                    remotePort: discoveryPort,
                    protocol: "UDP");
                status.DiscoveryAllowed = discoveryAnalysis.IsAllowed;
                status.DiscoveryFirewallAnalysis = discoveryAnalysis;
            }

            return status;
        }
    }
}