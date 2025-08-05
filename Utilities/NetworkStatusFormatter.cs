using System;
using System.Linq;
using System.Text;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
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
        /// <param name="applicationConfig">Application configuration containing connection settings</param>
        /// <returns>Formatted network troubleshooting content</returns>
        public string RenderNetworkTroubleshooting(NetworkStatus networkStatus, ApplicationConfig applicationConfig)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("NETWORK TROUBLESHOOTING:");
            sb.AppendLine("─────────────────────────");
            sb.AppendLine($"  Platform: {_commandProvider.GetPlatformName()}");
            sb.AppendLine($"  Last Updated: {networkStatus.LastUpdated:HH:mm:ss}");
            sb.AppendLine();

            // iPhone Connection Status
            sb.AppendLine("IPHONE CONNECTION:");
            sb.AppendLine("──────────────────");
            sb.AppendLine($"  Local Port (UDP {GetStatusIndicator(networkStatus.IPhone.LocalPortOpen)}): {GetLocalPortStatus(networkStatus.IPhone)}");
            sb.AppendLine($"  Outbound (UDP {GetStatusIndicator(networkStatus.IPhone.OutboundAllowed)}): {GetOutboundStatus(networkStatus.IPhone)}");

            if (networkStatus.IPhone.FirewallAnalysis != null)
            {
                sb.AppendLine($"  Firewall Rules: {GetStatusIndicator(networkStatus.IPhone.FirewallAnalysis.IsAllowed)}");
                AppendFirewallRules(sb, networkStatus.IPhone.FirewallAnalysis, "    ");
            }
            sb.AppendLine();

            // PC Connection Status
            sb.AppendLine("PC VTube Studio CONNECTION:");
            sb.AppendLine("───────────────────────────");
            sb.AppendLine($"  WebSocket (TCP {GetStatusIndicator(networkStatus.PC.WebSocketAllowed)}): {GetWebSocketStatus(networkStatus.PC)}");

            if (networkStatus.PC.DiscoveryAllowed)
            {
                sb.AppendLine($"  Discovery (UDP {GetStatusIndicator(networkStatus.PC.DiscoveryAllowed)}): {GetDiscoveryStatus(networkStatus.PC)}");
            }

            if (networkStatus.PC.WebSocketFirewallAnalysis != null)
            {
                sb.AppendLine($"  WebSocket Firewall: {GetStatusIndicator(networkStatus.PC.WebSocketFirewallAnalysis.IsAllowed)}");
                AppendFirewallRules(sb, networkStatus.PC.WebSocketFirewallAnalysis, "    ");
            }

            if (networkStatus.PC.DiscoveryFirewallAnalysis != null)
            {
                sb.AppendLine($"  Discovery Firewall: {GetStatusIndicator(networkStatus.PC.DiscoveryFirewallAnalysis.IsAllowed)}");
                AppendFirewallRules(sb, networkStatus.PC.DiscoveryFirewallAnalysis, "    ");
            }
            sb.AppendLine();

            // Commands Section
            sb.AppendLine("TROUBLESHOOTING COMMANDS:");
            sb.AppendLine("─────────────────────────");
            sb.AppendLine("  Copy and paste these commands in an elevated Command Prompt:");
            sb.AppendLine();

            // Get real configuration values
            var phoneConfig = applicationConfig.PhoneClient;
            var pcConfig = applicationConfig.PCClient;
            var discoveryPort = "47779"; // VTube Studio discovery port (hardcoded for now, enhancement later)

            // iPhone Commands
            sb.AppendLine("  iPhone UDP Commands:");
            sb.AppendLine($"    Check local port: {_commandProvider.GetCheckPortStatusCommand(phoneConfig.LocalPort.ToString(), "UDP")}");
            sb.AppendLine($"    Add inbound rule: {_commandProvider.GetAddFirewallRuleCommand("SharpBridge iPhone UDP Inbound", "in", "allow", "UDP", phoneConfig.LocalPort.ToString(), null, null)}");
            sb.AppendLine($"    Add outbound rule: {_commandProvider.GetAddFirewallRuleCommand("SharpBridge iPhone UDP", "out", "allow", "UDP", null, phoneConfig.IphonePort.ToString(), phoneConfig.IphoneIpAddress)}");
            sb.AppendLine($"    Remove inbound rule: {_commandProvider.GetRemoveFirewallRuleCommand("SharpBridge iPhone UDP Inbound")}");
            sb.AppendLine($"    Remove outbound rule: {_commandProvider.GetRemoveFirewallRuleCommand("SharpBridge iPhone UDP")}");
            sb.AppendLine();

            // PC Commands
            sb.AppendLine("  PC VTube Studio Commands:");
            sb.AppendLine($"    Check WebSocket port: {_commandProvider.GetCheckPortStatusCommand(pcConfig.Port.ToString(), "TCP")}");
            sb.AppendLine($"    Test connectivity: {_commandProvider.GetTestConnectivityCommand(pcConfig.Host, pcConfig.Port.ToString())}");
            sb.AppendLine($"    Add WebSocket rule: {_commandProvider.GetAddFirewallRuleCommand("SharpBridge PC WebSocket", "out", "allow", "TCP", null, pcConfig.Port.ToString(), pcConfig.Host)}");
            sb.AppendLine($"    Add discovery rule: {_commandProvider.GetAddFirewallRuleCommand("SharpBridge PC Discovery", "out", "allow", "UDP", null, discoveryPort, pcConfig.Host)}");
            sb.AppendLine($"    Remove WebSocket rule: {_commandProvider.GetRemoveFirewallRuleCommand("SharpBridge PC WebSocket")}");
            sb.AppendLine($"    Remove discovery rule: {_commandProvider.GetRemoveFirewallRuleCommand("SharpBridge PC Discovery")}");

            return sb.ToString();
        }

        /// <summary>
        /// Appends formatted firewall rules to the string builder
        /// </summary>
        /// <param name="sb">StringBuilder to append to</param>
        /// <param name="firewallAnalysis">Firewall analysis result</param>
        /// <param name="indent">Indentation prefix</param>
        private static void AppendFirewallRules(StringBuilder sb, FirewallAnalysisResult firewallAnalysis, string indent)
        {
            if (firewallAnalysis.RelevantRules.Count == 0)
            {
                sb.AppendLine($"{indent}No relevant rules found");
                return;
            }

            var rulesToShow = firewallAnalysis.RelevantRules.Take(5).ToList();
            var hasMoreRules = firewallAnalysis.RelevantRules.Count > 5;

            foreach (var rule in rulesToShow)
            {
                var statusIndicator = rule.IsEnabled ? ConsoleColors.Colorize("✓", ConsoleColors.Success) : ConsoleColors.Colorize("✗", ConsoleColors.Error);
                var actionColor = rule.Action.ToLowerInvariant() == "allow" ? ConsoleColors.Success : ConsoleColors.Error;
                var actionText = ConsoleColors.Colorize(rule.Action, actionColor);

                sb.AppendLine($"{indent}{statusIndicator} {rule.Name} ({rule.Direction} {actionText} {rule.Protocol})");
            }

            if (hasMoreRules)
            {
                var remainingCount = firewallAnalysis.RelevantRules.Count - 5;
                sb.AppendLine($"{indent}... and {remainingCount} more rules (use 'netsh advfirewall firewall show rule name=all' to see all)");
            }
        }

        private static string GetStatusIndicator(bool isGood)
        {
            return isGood
                ? ConsoleColors.Colorize("✓", ConsoleColors.Success)
                : ConsoleColors.Colorize("✗", ConsoleColors.Error);
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