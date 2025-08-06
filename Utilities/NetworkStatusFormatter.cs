using System;
using System.Collections.Generic;
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
            sb.AppendLine("IPHONE CONNECTION");
            sb.AppendLine("───────────────────────────────────");

            // Get configuration values for display
            var phoneConfig = applicationConfig.PhoneClient;

            // Inbound section (consolidated)
            if (networkStatus.IPhone.InboundFirewallAnalysis != null)
            {
                sb.AppendLine($"  {ConsoleColors.Colorize($"Local UDP Port {phoneConfig.LocalPort}", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(networkStatus.IPhone.InboundFirewallAnalysis.IsAllowed ? "Allowed" : "Blocked")} {GetStatusIndicator(networkStatus.IPhone.InboundFirewallAnalysis.IsAllowed)}");
                sb.AppendLine($"    ▸ Default inbound action ({networkStatus.IPhone.InboundFirewallAnalysis.ProfileName}): {(networkStatus.IPhone.InboundFirewallAnalysis.DefaultActionAllowed ? "Allow" : "Deny")} {GetStatusIndicator(networkStatus.IPhone.InboundFirewallAnalysis.DefaultActionAllowed)}");
                AppendFirewallRules(sb, networkStatus.IPhone.InboundFirewallAnalysis, "    ");
            }

            // Outbound section (consolidated)
            if (networkStatus.IPhone.OutboundFirewallAnalysis != null)
            {
                sb.AppendLine($"  {ConsoleColors.Colorize($"Outbound UDP to {phoneConfig.IphoneIpAddress}", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(networkStatus.IPhone.OutboundFirewallAnalysis.IsAllowed ? "Allowed" : "Blocked")} {GetStatusIndicator(networkStatus.IPhone.OutboundFirewallAnalysis.IsAllowed)}");
                sb.AppendLine($"    ▸ Default outbound action ({networkStatus.IPhone.OutboundFirewallAnalysis.ProfileName}): {(networkStatus.IPhone.OutboundFirewallAnalysis.DefaultActionAllowed ? "Allow" : "Deny")} {GetStatusIndicator(networkStatus.IPhone.OutboundFirewallAnalysis.DefaultActionAllowed)}");
                AppendFirewallRules(sb, networkStatus.IPhone.OutboundFirewallAnalysis, "    ");
            }
            sb.AppendLine();

            // PC Connection Status
            sb.AppendLine("PC VTube Studio CONNECTION");
            sb.AppendLine("───────────────────────────────────");

            // Get configuration values for display
            var pcConfig = applicationConfig.PCClient;

            // WebSocket section (consolidated)
            if (networkStatus.PC.WebSocketFirewallAnalysis != null)
            {
                sb.AppendLine($"  {ConsoleColors.Colorize($"WebSocket TCP to {pcConfig.Host}:{pcConfig.Port}", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(networkStatus.PC.WebSocketFirewallAnalysis.IsAllowed ? "Allowed" : "Blocked")} {GetStatusIndicator(networkStatus.PC.WebSocketFirewallAnalysis.IsAllowed)}");
                sb.AppendLine($"    ▸ Default outbound action ({networkStatus.PC.WebSocketFirewallAnalysis.ProfileName}): {(networkStatus.PC.WebSocketFirewallAnalysis.DefaultActionAllowed ? "Allow" : "Deny")} {GetStatusIndicator(networkStatus.PC.WebSocketFirewallAnalysis.DefaultActionAllowed)}");
                AppendFirewallRules(sb, networkStatus.PC.WebSocketFirewallAnalysis, "    ");
            }

            // Discovery section (consolidated, only if enabled)
            if (networkStatus.PC.DiscoveryFirewallAnalysis != null)
            {
                var discoveryPort = "47779"; // VTube Studio discovery port
                sb.AppendLine($"  {ConsoleColors.Colorize($"Discovery UDP to {pcConfig.Host}:{discoveryPort}", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(networkStatus.PC.DiscoveryFirewallAnalysis.IsAllowed ? "Allowed" : "Blocked")} {GetStatusIndicator(networkStatus.PC.DiscoveryFirewallAnalysis.IsAllowed)}");
                sb.AppendLine($"    ▸ Default outbound action ({networkStatus.PC.DiscoveryFirewallAnalysis.ProfileName}): {(networkStatus.PC.DiscoveryFirewallAnalysis.DefaultActionAllowed ? "Allow" : "Deny")} {GetStatusIndicator(networkStatus.PC.DiscoveryFirewallAnalysis.DefaultActionAllowed)}");
                AppendFirewallRules(sb, networkStatus.PC.DiscoveryFirewallAnalysis, "    ");
            }
            sb.AppendLine();

            // Commands Section
            sb.AppendLine("TROUBLESHOOTING COMMANDS:");
            sb.AppendLine("─────────────────────────");
            sb.AppendLine($"  {ConsoleColors.Colorize("Copy and paste these commands in an elevated Command Prompt", ConsoleColors.ConfigPropertyName)}:");
            sb.AppendLine();

            // Get real configuration values for commands section
            var phoneConfigForCommands = applicationConfig.PhoneClient;
            var pcConfigForCommands = applicationConfig.PCClient;
            var discoveryPortForCommands = "47779"; // VTube Studio discovery port (hardcoded for now, enhancement later)

            // iPhone Commands
            sb.AppendLine($"  {ConsoleColors.Colorize("iPhone UDP Commands", ConsoleColors.ConfigPropertyName)}:");
            sb.AppendLine($"    {ConsoleColors.Colorize("Check local port", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(_commandProvider.GetCheckPortStatusCommand(phoneConfigForCommands.LocalPort.ToString(), "UDP"))}");
            sb.AppendLine($"    {ConsoleColors.Colorize("Add inbound rule", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(_commandProvider.GetAddFirewallRuleCommand("SharpBridge iPhone UDP Inbound", "in", "allow", "UDP", phoneConfigForCommands.LocalPort.ToString(), null, null))}");
            sb.AppendLine($"    {ConsoleColors.Colorize("Add outbound rule", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(_commandProvider.GetAddFirewallRuleCommand("SharpBridge iPhone UDP", "out", "allow", "UDP", null, phoneConfigForCommands.IphonePort.ToString(), phoneConfigForCommands.IphoneIpAddress))}");
            sb.AppendLine($"    {ConsoleColors.Colorize("Remove inbound rule", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(_commandProvider.GetRemoveFirewallRuleCommand("SharpBridge iPhone UDP Inbound"))}");
            sb.AppendLine($"    {ConsoleColors.Colorize("Remove outbound rule", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(_commandProvider.GetRemoveFirewallRuleCommand("SharpBridge iPhone UDP"))}");
            sb.AppendLine();

            // PC Commands
            sb.AppendLine($"  {ConsoleColors.Colorize("PC VTube Studio Commands", ConsoleColors.ConfigPropertyName)}:");
            sb.AppendLine($"    {ConsoleColors.Colorize("Check WebSocket port", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(_commandProvider.GetCheckPortStatusCommand(pcConfigForCommands.Port.ToString(), "TCP"))}");
            sb.AppendLine($"    {ConsoleColors.Colorize("Test connectivity", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(_commandProvider.GetTestConnectivityCommand(pcConfigForCommands.Host, pcConfigForCommands.Port.ToString()))}");
            sb.AppendLine($"    {ConsoleColors.Colorize("Add WebSocket rule", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(_commandProvider.GetAddFirewallRuleCommand("SharpBridge PC WebSocket", "out", "allow", "TCP", null, pcConfigForCommands.Port.ToString(), pcConfigForCommands.Host))}");
            sb.AppendLine($"    {ConsoleColors.Colorize("Add discovery rule", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(_commandProvider.GetAddFirewallRuleCommand("SharpBridge PC Discovery", "out", "allow", "UDP", null, discoveryPortForCommands, pcConfigForCommands.Host))}");
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
                sb.AppendLine($"{indent}▸ No explicit rules found – default action applied");
                return;
            }

            // Show matching rules header with count
            var ruleCount = firewallAnalysis.RelevantRules.Count;
            var hasMoreRules = ruleCount > 5;
            var rulesToShow = firewallAnalysis.RelevantRules.Take(5).ToList();

            if (hasMoreRules)
            {
                sb.AppendLine($"{indent}▸ Matching rules (top 5 of {ruleCount}):");
            }
            else
            {
                var ruleText = ruleCount == 1 ? "rule" : "rules";
                sb.AppendLine($"{indent}▸ Matching {ruleText} ({ruleCount} found):");
            }

            foreach (var rule in rulesToShow)
            {
                var statusIndicator = rule.IsEnabled ? ConsoleColors.Colorize("✓", ConsoleColors.Success) : ConsoleColors.Colorize("✗", ConsoleColors.Error);
                var actionColor = rule.Action.ToLowerInvariant() == "allow" ? ConsoleColors.Success : ConsoleColors.Error;
                var actionText = ConsoleColors.Colorize(rule.Action, actionColor);

                // Build readable rule description
                var ruleDescription = FormatRuleDescription(rule);
                sb.AppendLine($"{indent}    {statusIndicator} {actionText} {rule.Protocol} {ruleDescription}");
            }

            if (hasMoreRules)
            {
                var remainingCount = ruleCount - 5;
                sb.AppendLine($"{indent}    {ConsoleColors.Colorize($"… and {remainingCount} more rules", ConsoleColors.Warning)}");
            }
        }

        /// <summary>
        /// Formats a firewall rule into a readable description
        /// </summary>
        private static string FormatRuleDescription(FirewallRule rule)
        {
            var parts = new List<string>();

            // Add port info if available
            if (!string.IsNullOrEmpty(rule.LocalPort) && rule.LocalPort != "*" && rule.LocalPort.ToLower() != "any")
            {
                parts.Add(rule.LocalPort);
            }

            // Add direction info with arrow
            var direction = "";
            if (!string.IsNullOrEmpty(rule.RemoteAddress) && rule.RemoteAddress != "*" && rule.RemoteAddress.ToLower() != "any")
            {
                var source = rule.RemoteAddress == "0.0.0.0" ? "Any" : rule.RemoteAddress;
                direction = rule.Direction.ToLower() == "inbound" ? $"({source} → ThisDevice)" : $"(ThisDevice → {source})";
            }
            else
            {
                direction = rule.Direction.ToLower() == "inbound" ? "(Any → ThisDevice)" : "(ThisDevice → Any)";
            }

            if (!string.IsNullOrEmpty(direction))
            {
                parts.Add(direction);
            }

            // Add app info if available and not global
            if (!string.IsNullOrEmpty(rule.ApplicationName))
            {
                var appName = System.IO.Path.GetFileName(rule.ApplicationName);
                parts.Add($"App: {appName}");
            }
            else
            {
                parts.Add("Global");
            }

            return string.Join("  ", parts);
        }

        private static string GetStatusIndicator(bool isGood)
        {
            return isGood
                ? ConsoleColors.Colorize("✓", ConsoleColors.Success)
                : ConsoleColors.Colorize("✗", ConsoleColors.Error);
        }
    }
}