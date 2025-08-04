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
    public class PortStatusMonitor : IPortStatusMonitor
    {
        private readonly IFirewallAnalyzer _firewallAnalyzer;
        private readonly IConfigManager _configManager;

        /// <summary>
        /// Initializes a new instance of the PortStatusMonitor
        /// </summary>
        /// <param name="firewallAnalyzer">Firewall analyzer for connectivity analysis</param>
        /// <param name="configManager">Configuration manager for getting connection settings</param>
        public PortStatusMonitor(IFirewallAnalyzer firewallAnalyzer, IConfigManager configManager)
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
            var phoneConfig = await _configManager.LoadPhoneConfigAsync();
            var pcConfig = await _configManager.LoadPCConfigAsync();

            var iphoneHost = phoneConfig.IphoneIpAddress;
            var iphonePort = phoneConfig.IphonePort.ToString();
            var pcHost = pcConfig.Host;
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
            var phoneConfig = await _configManager.LoadPhoneConfigAsync();
            var localPort = phoneConfig.LocalPort.ToString();

            // Check local port
            status.LocalPortOpen = await CheckLocalPortAsync(localPort);

            // Check outbound connectivity (no actual connection test)
            status.OutboundAllowed = true; // Assume allowed for dummy implementation

            // Analyze firewall rules
            status.FirewallAnalysis = _firewallAnalyzer.AnalyzeFirewallRules(
                localPort: localPort,
                remoteHost: host,
                remotePort: port,
                protocol: "UDP");

            return status;
        }

        private async Task<PCConnectionStatus> CheckPCConnectionAsync(string host, string port, string discoveryPort, bool usePortDiscovery)
        {
            var status = new PCConnectionStatus
            {
                LastChecked = DateTime.UtcNow
            };

            // Check WebSocket port
            status.WebSocketAllowed = true; // Assume allowed for dummy implementation
            status.WebSocketFirewallAnalysis = _firewallAnalyzer.AnalyzeFirewallRules(
                localPort: null,
                remoteHost: host,
                remotePort: port,
                protocol: "TCP");

            // Check discovery port if enabled
            if (usePortDiscovery)
            {
                status.DiscoveryAllowed = true; // Assume allowed for dummy implementation
                status.DiscoveryFirewallAnalysis = _firewallAnalyzer.AnalyzeFirewallRules(
                    localPort: null,
                    remoteHost: host,
                    remotePort: discoveryPort,
                    protocol: "UDP");
            }

            return status;
        }

        private static async Task<bool> CheckLocalPortAsync(string port)
        {
            try
            {
                // Try to check if port is available (no binding, just checking if it's in use)
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Bind(new IPEndPoint(IPAddress.Any, int.Parse(port)));
                socket.Close();
                return true; // Port is available
            }
            catch
            {
                return false; // Port is in use or blocked
            }
        }
    }
}