using System;
using System.Text;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Services
{
    /// <summary>
    /// Formats network troubleshooting information for system help display
    /// </summary>
    public class NetworkStatusFormatter : INetworkStatusFormatter
    {
        private readonly INetworkCommandProvider _commandProvider;

        /// <summary>
        /// Initializes a new instance of the NetworkStatusFormatter
        /// </summary>
        /// <param name="commandProvider">Provider for platform-specific commands</param>
        public NetworkStatusFormatter(INetworkCommandProvider commandProvider)
        {
            _commandProvider = commandProvider ?? throw new ArgumentNullException(nameof(commandProvider));
        }

        /// <summary>
        /// Renders network troubleshooting section for system help
        /// </summary>
        /// <param name="networkStatus">Current network status to display</param>
        /// <returns>Formatted network troubleshooting content</returns>
        public string RenderNetworkTroubleshooting(NetworkStatus networkStatus)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("=== Network Troubleshooting ===");
            sb.AppendLine($"Platform: {_commandProvider.GetPlatformName()}");
            sb.AppendLine($"Last Updated: {networkStatus.LastUpdated:HH:mm:ss}");
            sb.AppendLine();

            // iPhone Connection Status
            sb.AppendLine("📱 iPhone Connection:");
            sb.AppendLine($"   Local Port (UDP {GetStatusIndicator(networkStatus.IPhone.LocalPortOpen)}): {GetLocalPortStatus(networkStatus.IPhone)}");
            sb.AppendLine($"   Outbound (UDP {GetStatusIndicator(networkStatus.IPhone.OutboundAllowed)}): {GetOutboundStatus(networkStatus.IPhone)}");

            if (networkStatus.IPhone.FirewallAnalysis != null)
            {
                sb.AppendLine($"   Firewall Rules: {GetStatusIndicator(networkStatus.IPhone.FirewallAnalysis.IsAllowed)} {networkStatus.IPhone.FirewallAnalysis.RelevantRules.Count} rules");
            }
            sb.AppendLine();

            // PC Connection Status
            sb.AppendLine("💻 PC VTube Studio Connection:");
            sb.AppendLine($"   WebSocket (TCP {GetStatusIndicator(networkStatus.PC.WebSocketAllowed)}): {GetWebSocketStatus(networkStatus.PC)}");

            if (networkStatus.PC.DiscoveryAllowed)
            {
                sb.AppendLine($"   Discovery (UDP {GetStatusIndicator(networkStatus.PC.DiscoveryAllowed)}): {GetDiscoveryStatus(networkStatus.PC)}");
            }

            if (networkStatus.PC.WebSocketFirewallAnalysis != null)
            {
                sb.AppendLine($"   WebSocket Firewall: {GetStatusIndicator(networkStatus.PC.WebSocketFirewallAnalysis.IsAllowed)} {networkStatus.PC.WebSocketFirewallAnalysis.RelevantRules.Count} rules");
            }

            if (networkStatus.PC.DiscoveryFirewallAnalysis != null)
            {
                sb.AppendLine($"   Discovery Firewall: {GetStatusIndicator(networkStatus.PC.DiscoveryFirewallAnalysis.IsAllowed)} {networkStatus.PC.DiscoveryFirewallAnalysis.RelevantRules.Count} rules");
            }
            sb.AppendLine();

            // Commands Section
            sb.AppendLine("🔧 Troubleshooting Commands:");
            sb.AppendLine("Copy and paste these commands in an elevated Command Prompt:");
            sb.AppendLine();

            // iPhone Commands
            sb.AppendLine("iPhone UDP Commands:");
            sb.AppendLine($"   Check local port: {_commandProvider.GetCheckPortStatusCommand("28964", "UDP")}");
            sb.AppendLine($"   Add outbound rule: {_commandProvider.GetAddFirewallRuleCommand("SharpBridge iPhone UDP", "out", "allow", "UDP", null, "21412", "192.168.1.178")}");
            sb.AppendLine($"   Remove rule: {_commandProvider.GetRemoveFirewallRuleCommand("SharpBridge iPhone UDP")}");
            sb.AppendLine();

            // PC Commands
            sb.AppendLine("PC VTube Studio Commands:");
            sb.AppendLine($"   Check WebSocket port: {_commandProvider.GetCheckPortStatusCommand("8001", "TCP")}");
            sb.AppendLine($"   Test connectivity: {_commandProvider.GetTestConnectivityCommand("localhost", "8001")}");
            sb.AppendLine($"   Add WebSocket rule: {_commandProvider.GetAddFirewallRuleCommand("SharpBridge PC WebSocket", "out", "allow", "TCP", null, "8001", "localhost")}");
            sb.AppendLine($"   Add discovery rule: {_commandProvider.GetAddFirewallRuleCommand("SharpBridge PC Discovery", "out", "allow", "UDP", null, "47779", "localhost")}");
            sb.AppendLine($"   Remove WebSocket rule: {_commandProvider.GetRemoveFirewallRuleCommand("SharpBridge PC WebSocket")}");
            sb.AppendLine($"   Remove discovery rule: {_commandProvider.GetRemoveFirewallRuleCommand("SharpBridge PC Discovery")}");

            return sb.ToString();
        }

        private static string GetStatusIndicator(bool isGood)
        {
            return isGood ? "🟢" : "🔴";
        }

        private static string GetLocalPortStatus(IPhoneConnectionStatus status)
        {
            return status.LocalPortOpen ? "Open" : "Closed";
        }

        private static string GetOutboundStatus(IPhoneConnectionStatus status)
        {
            return status.OutboundAllowed ? "Allowed" : "Blocked";
        }

        private static string GetWebSocketStatus(PCConnectionStatus status)
        {
            return status.WebSocketAllowed ? "Allowed" : "Blocked";
        }

        private static string GetDiscoveryStatus(PCConnectionStatus status)
        {
            return status.DiscoveryAllowed ? "Allowed" : "Blocked";
        }
    }
}