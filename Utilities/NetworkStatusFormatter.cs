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
            sb.AppendLine($"  {ConsoleColors.Colorize("Local Port (UDP)", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(GetLocalPortStatus(networkStatus.IPhone))} {GetStatusIndicator(networkStatus.IPhone.LocalPortOpen)}");
            sb.AppendLine($"  {ConsoleColors.Colorize("Outbound (UDP)", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(GetOutboundStatus(networkStatus.IPhone))} {GetStatusIndicator(networkStatus.IPhone.OutboundAllowed)}");

            if (networkStatus.IPhone.FirewallAnalysis != null)
            {
                sb.AppendLine($"  {ConsoleColors.Colorize("Firewall Rules", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(networkStatus.IPhone.FirewallAnalysis.IsAllowed ? "Allowed" : "Blocked")} {GetStatusIndicator(networkStatus.IPhone.FirewallAnalysis.IsAllowed)}");
                AppendFirewallRules(sb, networkStatus.IPhone.FirewallAnalysis, "    ");
            }
            sb.AppendLine();

            // PC Connection Status
            sb.AppendLine("PC VTube Studio CONNECTION:");
            sb.AppendLine("───────────────────────────");
            sb.AppendLine($"  {ConsoleColors.Colorize("WebSocket (TCP)", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(GetWebSocketStatus(networkStatus.PC))} {GetStatusIndicator(networkStatus.PC.WebSocketAllowed)}");

            if (networkStatus.PC.DiscoveryAllowed)
            {
                sb.AppendLine($"  {ConsoleColors.Colorize("Discovery (UDP)", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(GetDiscoveryStatus(networkStatus.PC))} {GetStatusIndicator(networkStatus.PC.DiscoveryAllowed)}");
            }

            if (networkStatus.PC.WebSocketFirewallAnalysis != null)
            {
                sb.AppendLine($"  {ConsoleColors.Colorize("WebSocket Firewall", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(networkStatus.PC.WebSocketFirewallAnalysis.IsAllowed ? "Allowed" : "Blocked")} {GetStatusIndicator(networkStatus.PC.WebSocketFirewallAnalysis.IsAllowed)}");
                AppendFirewallRules(sb, networkStatus.PC.WebSocketFirewallAnalysis, "    ");
            }

            if (networkStatus.PC.DiscoveryFirewallAnalysis != null)
            {
                sb.AppendLine($"  {ConsoleColors.Colorize("Discovery Firewall", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(networkStatus.PC.DiscoveryFirewallAnalysis.IsAllowed ? "Allowed" : "Blocked")} {GetStatusIndicator(networkStatus.PC.DiscoveryFirewallAnalysis.IsAllowed)}");
                AppendFirewallRules(sb, networkStatus.PC.DiscoveryFirewallAnalysis, "    ");
            }
            sb.AppendLine();

            // Commands Section
            sb.AppendLine("TROUBLESHOOTING COMMANDS:");
            sb.AppendLine("─────────────────────────");
            sb.AppendLine($"  {ConsoleColors.Colorize("Copy and paste these commands in an elevated Command Prompt", ConsoleColors.ConfigPropertyName)}:");
            sb.AppendLine();

            // Get real configuration values
            var phoneConfig = applicationConfig.PhoneClient;
            var pcConfig = applicationConfig.PCClient;
            var discoveryPort = "47779"; // VTube Studio discovery port (hardcoded for now, enhancement later)

            // iPhone Commands
            sb.AppendLine($"  {ConsoleColors.Colorize("iPhone UDP Commands", ConsoleColors.ConfigPropertyName)}:");
            sb.AppendLine($"    {ConsoleColors.Colorize("Check local port", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(_commandProvider.GetCheckPortStatusCommand(phoneConfig.LocalPort.ToString(), "UDP"))}");
            sb.AppendLine($"    {ConsoleColors.Colorize("Add inbound rule", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(_commandProvider.GetAddFirewallRuleCommand("SharpBridge iPhone UDP Inbound", "in", "allow", "UDP", phoneConfig.LocalPort.ToString(), null, null))}");
            sb.AppendLine($"    {ConsoleColors.Colorize("Add outbound rule", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(_commandProvider.GetAddFirewallRuleCommand("SharpBridge iPhone UDP", "out", "allow", "UDP", null, phoneConfig.IphonePort.ToString(), phoneConfig.IphoneIpAddress))}");
            sb.AppendLine($"    {ConsoleColors.Colorize("Remove inbound rule", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(_commandProvider.GetRemoveFirewallRuleCommand("SharpBridge iPhone UDP Inbound"))}");
            sb.AppendLine($"    {ConsoleColors.Colorize("Remove outbound rule", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(_commandProvider.GetRemoveFirewallRuleCommand("SharpBridge iPhone UDP"))}");
            sb.AppendLine();

            // PC Commands
            sb.AppendLine($"  {ConsoleColors.Colorize("PC VTube Studio Commands", ConsoleColors.ConfigPropertyName)}:");
            sb.AppendLine($"    {ConsoleColors.Colorize("Check WebSocket port", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(_commandProvider.GetCheckPortStatusCommand(pcConfig.Port.ToString(), "TCP"))}");
            sb.AppendLine($"    {ConsoleColors.Colorize("Test connectivity", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(_commandProvider.GetTestConnectivityCommand(pcConfig.Host, pcConfig.Port.ToString()))}");
            sb.AppendLine($"    {ConsoleColors.Colorize("Add WebSocket rule", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(_commandProvider.GetAddFirewallRuleCommand("SharpBridge PC WebSocket", "out", "allow", "TCP", null, pcConfig.Port.ToString(), pcConfig.Host))}");
            sb.AppendLine($"    {ConsoleColors.Colorize("Add discovery rule", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(_commandProvider.GetAddFirewallRuleCommand("SharpBridge PC Discovery", "out", "allow", "UDP", null, discoveryPort, pcConfig.Host))}");
            sb.AppendLine($"    {ConsoleColors.Colorize("Remove WebSocket rule", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(_commandProvider.GetRemoveFirewallRuleCommand("SharpBridge PC WebSocket"))}");
            sb.AppendLine($"    {ConsoleColors.Colorize("Remove discovery rule", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(_commandProvider.GetRemoveFirewallRuleCommand("SharpBridge PC Discovery"))}");

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