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
        private const string AllowedText = "Allowed";
        private const string BlockedText = "Blocked";
        private const string AllowText = "Allow";
        private const string AllowLowerText = "allow";
        private readonly INetworkCommandProvider _commandProvider;
        private readonly ITableFormatter _tableFormatter;

        /// <summary>
        /// Initializes a new instance of the NetworkStatusFormatter
        /// </summary>
        /// <param name="commandProvider">Provider for platform-specific commands</param>
        /// <param name="tableFormatter">Table formatter for rendering firewall rules</param>
        public NetworkStatusFormatter(INetworkCommandProvider commandProvider, ITableFormatter tableFormatter)
        {
            _commandProvider = commandProvider ?? throw new ArgumentNullException(nameof(commandProvider));
            _tableFormatter = tableFormatter ?? throw new ArgumentNullException(nameof(tableFormatter));
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

            AppendHeader(sb, networkStatus);
            AppendIPhoneConnectionStatus(sb, networkStatus, applicationConfig);
            AppendPCConnectionStatus(sb, networkStatus, applicationConfig);
            AppendTroubleshootingCommands(sb, networkStatus, applicationConfig);

            return sb.ToString();
        }

        /// <summary>
        /// Appends the header section with platform info and timestamp
        /// </summary>
        private void AppendHeader(StringBuilder sb, NetworkStatus networkStatus)
        {
            sb.AppendLine("NETWORK TROUBLESHOOTING:");
            sb.AppendLine("─────────────────────────");
            sb.AppendLine($"  Platform: {ConsoleColors.ColorizeBasicType(_commandProvider.GetPlatformName())}");
            sb.AppendLine($"  Last Updated: " + ConsoleColors.ColorizeBasicType($"{networkStatus.LastUpdated:HH:mm:ss}"));
            sb.AppendLine();
        }

        /// <summary>
        /// Appends iPhone connection status section
        /// </summary>
        private void AppendIPhoneConnectionStatus(StringBuilder sb, NetworkStatus networkStatus, ApplicationConfig applicationConfig)
        {
            sb.AppendLine("IPHONE CONNECTION");
            sb.AppendLine("───────────────────────────────────");

            var phoneConfig = applicationConfig.PhoneClient;

            // Inbound section
            if (networkStatus.IPhone.InboundFirewallAnalysis != null)
            {
                var analysis = networkStatus.IPhone.InboundFirewallAnalysis;
                sb.AppendLine($"  {ConsoleColors.Colorize($"Local UDP Port {phoneConfig.LocalPort}", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(analysis.IsAllowed ? AllowedText : BlockedText)} {GetStatusIndicator(analysis.IsAllowed)}");
                sb.AppendLine($"    - Default inbound action ({analysis.ProfileName}): {(analysis.DefaultActionAllowed ? AllowText : "Deny")} {GetStatusIndicator(analysis.DefaultActionAllowed)}");
                this.AppendFirewallRules(sb, analysis);
            }

            // Outbound section
            if (networkStatus.IPhone.OutboundFirewallAnalysis != null)
            {
                var analysis = networkStatus.IPhone.OutboundFirewallAnalysis;
                sb.AppendLine($"  {ConsoleColors.Colorize($"Outbound UDP to {phoneConfig.IphoneIpAddress}", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(analysis.IsAllowed ? AllowedText : BlockedText)} {GetStatusIndicator(analysis.IsAllowed)}");
                sb.AppendLine($"    - Default outbound action ({analysis.ProfileName}): {(analysis.DefaultActionAllowed ? AllowText : "Deny")} {GetStatusIndicator(analysis.DefaultActionAllowed)}");
                this.AppendFirewallRules(sb, analysis);
            }

            sb.AppendLine();
        }

        /// <summary>
        /// Appends PC connection status section
        /// </summary>
        private void AppendPCConnectionStatus(StringBuilder sb, NetworkStatus networkStatus, ApplicationConfig applicationConfig)
        {
            sb.AppendLine("PC VTube Studio CONNECTION");
            sb.AppendLine("───────────────────────────────────");

            var pcConfig = applicationConfig.PCClient;

            // WebSocket section
            if (networkStatus.PC.WebSocketFirewallAnalysis != null)
            {
                var analysis = networkStatus.PC.WebSocketFirewallAnalysis;
                sb.AppendLine($"  {ConsoleColors.Colorize($"WebSocket TCP to {pcConfig.Host}:{pcConfig.Port}", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(analysis.IsAllowed ? AllowedText : BlockedText)} {GetStatusIndicator(analysis.IsAllowed)}");
                sb.AppendLine($"    - Default outbound action ({analysis.ProfileName}): {(analysis.DefaultActionAllowed ? AllowText : "Deny")} {GetStatusIndicator(analysis.DefaultActionAllowed)}");
                this.AppendFirewallRules(sb, analysis);
            }

            // Discovery section
            if (networkStatus.PC.DiscoveryFirewallAnalysis != null)
            {
                var analysis = networkStatus.PC.DiscoveryFirewallAnalysis;
                var discoveryPort = "47779"; // VTube Studio discovery port
                sb.AppendLine($"  {ConsoleColors.Colorize($"Discovery UDP to {pcConfig.Host}:{discoveryPort}", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(analysis.IsAllowed ? AllowedText : BlockedText)} {GetStatusIndicator(analysis.IsAllowed)}");
                sb.AppendLine($"    - Default outbound action ({analysis.ProfileName}): {(analysis.DefaultActionAllowed ? AllowText : "Deny")} {GetStatusIndicator(analysis.DefaultActionAllowed)}");
                this.AppendFirewallRules(sb, analysis);
            }

            sb.AppendLine();
        }

        /// <summary>
        /// Appends troubleshooting commands section
        /// </summary>
        private void AppendTroubleshootingCommands(StringBuilder sb, NetworkStatus networkStatus, ApplicationConfig applicationConfig)
        {
            sb.AppendLine("TROUBLESHOOTING COMMANDS:");
            sb.AppendLine("─────────────────────────");
            sb.AppendLine($"  {ConsoleColors.Colorize("Copy and paste these commands in an elevated Command Prompt", ConsoleColors.ConfigPropertyName)}:");
            sb.AppendLine();

            AppendIPhoneCommands(sb, networkStatus, applicationConfig);
            AppendPCCommands(sb, networkStatus, applicationConfig);
        }

        /// <summary>
        /// Appends iPhone-specific troubleshooting commands
        /// </summary>
        private void AppendIPhoneCommands(StringBuilder sb, NetworkStatus networkStatus, ApplicationConfig applicationConfig)
        {
            sb.AppendLine($"  {ConsoleColors.Colorize("iPhone UDP Commands", ConsoleColors.ConfigPropertyName)}:");

            var phoneConfig = applicationConfig.PhoneClient;
            var inboundBlocked = !(networkStatus.IPhone.InboundFirewallAnalysis?.IsAllowed ?? true);
            var outboundBlocked = !(networkStatus.IPhone.OutboundFirewallAnalysis?.IsAllowed ?? true);

            // Check command
            var checkCmd = _commandProvider.GetCheckPortStatusCommand(phoneConfig.LocalPort.ToString(), "UDP");
            sb.AppendLine($"    {ConsoleColors.Colorize("Check local port", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(checkCmd)}");

            // Add commands (with conditional coloring)
            AppendAddFirewallCommand(sb, "Add inbound rule", inboundBlocked,
                _commandProvider.GetAddFirewallRuleCommand("SharpBridge iPhone UDP Inbound", "in", AllowLowerText, "UDP", phoneConfig.LocalPort.ToString(), null, null));

            AppendAddFirewallCommand(sb, "Add outbound rule", outboundBlocked,
                _commandProvider.GetAddFirewallRuleCommand("SharpBridge iPhone UDP", "out", AllowLowerText, "UDP", null, phoneConfig.IphonePort.ToString(), phoneConfig.IphoneIpAddress));

            // Remove commands
            var removeInboundCmd = _commandProvider.GetRemoveFirewallRuleCommand("SharpBridge iPhone UDP Inbound");
            var removeOutboundCmd = _commandProvider.GetRemoveFirewallRuleCommand("SharpBridge iPhone UDP");
            sb.AppendLine($"    {ConsoleColors.Colorize("Remove inbound rule", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(removeInboundCmd)}");
            sb.AppendLine($"    {ConsoleColors.Colorize("Remove outbound rule", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(removeOutboundCmd)}");
            sb.AppendLine();
        }

        /// <summary>
        /// Appends PC-specific troubleshooting commands
        /// </summary>
        private void AppendPCCommands(StringBuilder sb, NetworkStatus networkStatus, ApplicationConfig applicationConfig)
        {
            sb.AppendLine($"  {ConsoleColors.Colorize("PC VTube Studio Commands", ConsoleColors.ConfigPropertyName)}:");

            var pcConfig = applicationConfig.PCClient;
            var webSocketBlocked = !(networkStatus.PC.WebSocketFirewallAnalysis?.IsAllowed ?? true);
            var discoveryBlocked = !(networkStatus.PC.DiscoveryFirewallAnalysis?.IsAllowed ?? true);
            var discoveryPort = "47779";

            // Check/Test commands
            var checkCmd = _commandProvider.GetCheckPortStatusCommand(pcConfig.Port.ToString(), "TCP");
            var testCmd = _commandProvider.GetTestConnectivityCommand(pcConfig.Host ?? "localhost", pcConfig.Port.ToString());
            sb.AppendLine($"    {ConsoleColors.Colorize("Check WebSocket port", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(checkCmd)}");
            sb.AppendLine($"    {ConsoleColors.Colorize("Test connectivity", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(testCmd)}");

            // Add commands (with conditional coloring)
            AppendAddFirewallCommand(sb, "Add WebSocket rule", webSocketBlocked,
                _commandProvider.GetAddFirewallRuleCommand("SharpBridge PC WebSocket", "out", AllowLowerText, "TCP", null, pcConfig.Port.ToString(), pcConfig.Host));

            AppendAddFirewallCommand(sb, "Add discovery rule", discoveryBlocked,
                _commandProvider.GetAddFirewallRuleCommand("SharpBridge PC Discovery", "out", AllowLowerText, "UDP", null, discoveryPort, pcConfig.Host));

            // Remove commands
            var removeWebSocketCmd = _commandProvider.GetRemoveFirewallRuleCommand("SharpBridge PC WebSocket");
            var removeDiscoveryCmd = _commandProvider.GetRemoveFirewallRuleCommand("SharpBridge PC Discovery");
            sb.AppendLine($"    {ConsoleColors.Colorize("Remove WebSocket rule", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(removeWebSocketCmd)}");
            sb.AppendLine($"    {ConsoleColors.Colorize("Remove discovery rule", ConsoleColors.ConfigPropertyName)}: {ConsoleColors.ColorizeBasicType(removeDiscoveryCmd)}");
        }

        /// <summary>
        /// Appends an "Add firewall rule" command with conditional coloring based on blocked status
        /// </summary>
        private static void AppendAddFirewallCommand(StringBuilder sb, string description, bool isBlocked, string command)
        {
            sb.AppendLine($"    {ConsoleColors.Colorize(description, ConsoleColors.ConfigPropertyName)}: {(isBlocked ? ConsoleColors.Colorize(command, ConsoleColors.Error) : ConsoleColors.ColorizeBasicType(command))}");
        }

        /// <summary>
        /// Appends formatted firewall rules to the string builder using table format
        /// </summary>
        /// <param name="sb">StringBuilder to append to</param>
        /// <param name="firewallAnalysis">Firewall analysis result</param>
        /// <param name="indent">Indentation prefix</param>
        private void AppendFirewallRules(StringBuilder sb, FirewallAnalysisResult firewallAnalysis, int indent = 4)
        {
            if (firewallAnalysis.RelevantRules.Count == 0)
            {
                sb.AppendLine($"{new string(' ', indent)}- No explicit rules found – default action applied");
                return;
            }

            // Show matching rules header with count
            var ruleCount = firewallAnalysis.RelevantRules.Count;
            var hasMoreRules = ruleCount > 10;
            var rulesToShow = firewallAnalysis.RelevantRules.Take(10).ToList();

            string title;
            if (hasMoreRules)
            {
                title = $"Matching rules (top 10 of {ruleCount}):";
            }
            else
            {
                var ruleText = ruleCount == 1 ? "rule" : "rules";
                title = $"Matching {ruleText} ({ruleCount} found):";
            }

            // Create column formatters for the firewall rules table
            var columnFormatters = FirewallRuleTableFormatters.CreateColumnFormatters();

            // Render the table with indentation
            _tableFormatter.AppendTable(
                sb,
                title,
                rulesToShow,
                columnFormatters,
                targetColumnCount: 1,
                consoleWidth: 120, // Use a reasonable default console width
                singleColumnBarWidth: 20,
                singleColumnMaxItems: null,
                indent: indent
            );

            if (hasMoreRules)
            {
                var remainingCount = ruleCount - 10;
                sb.AppendLine($"{new string(' ', indent)}    {ConsoleColors.Colorize($"… and {remainingCount} more rules", ConsoleColors.Warning)}");
            }
        }


        private static string GetStatusIndicator(bool isGood)
        {
            return isGood
                ? ConsoleColors.Colorize("✓", ConsoleColors.Success)
                : ConsoleColors.Colorize("X", ConsoleColors.Error);
        }
    }
}