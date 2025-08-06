using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using SharpBridge.Interfaces;
using SharpBridge.Models;
using SharpBridge.Utilities.ComInterop;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Windows Firewall Analyzer that uses the rule engine for analysis
    /// </summary>
    public class WindowsFirewallAnalyzer : IFirewallAnalyzer
    {
        private readonly IFirewallRuleEngine _ruleEngine;
        private readonly IAppLogger _logger;

        public WindowsFirewallAnalyzer(IFirewallRuleEngine ruleEngine, IAppLogger logger)
        {
            _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Analyzes firewall rules to determine if connectivity is allowed
        /// </summary>
        /// <param name="localPort">Local port number (can be null for outbound-only analysis)</param>
        /// <param name="remoteHost">Remote host address</param>
        /// <param name="remotePort">Remote port number</param>
        /// <param name="protocol">Protocol (UDP, TCP, etc.)</param>
        /// <returns>Firewall analysis result with allowed status and relevant rules</returns>
        public FirewallAnalysisResult AnalyzeFirewallRules(
            string? localPort,
            string remoteHost,
            string remotePort,
            string protocol)
        {
            try
            {
                // 1. Environment Detection
                var firewallState = GetFirewallState();
                var targetInterface = GetBestInterface(remoteHost);
                var interfaceProfile = GetInterfaceProfile(targetInterface);

                // 2. Connection Direction Analysis
                var direction = localPort != null ? 1 : 2; // 1 = Inbound, 2 = Outbound
                var protocolValue = GetProtocolValue(protocol);

                _logger.Debug($"Analyzing {protocol} connection: direction={(direction == 1 ? "inbound" : "outbound")}, " +
                             $"target={remoteHost}:{remotePort}, interface={targetInterface}, profile={interfaceProfile}, firewall={firewallState}");

                // 3. Get Relevant Rules
                var relevantRules = _ruleEngine.GetRelevantRules(
                    direction: direction,
                    protocol: protocolValue,
                    profile: interfaceProfile,
                    targetHost: remoteHost,
                    targetPort: remotePort,
                    localPort: localPort);

                // 4. Apply Windows Firewall Precedence Logic
                var isAllowed = EvaluatePrecedence(relevantRules, firewallState, direction);

                _logger.Debug($"Found {relevantRules.Count} relevant rules, connection allowed: {isAllowed}");

                return new FirewallAnalysisResult
                {
                    IsAllowed = isAllowed,
                    RelevantRules = relevantRules.Take(5).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error analyzing firewall rules: {ex.Message}");
                return new FirewallAnalysisResult
                {
                    IsAllowed = false,
                    RelevantRules = new List<FirewallRule>()
                };
            }
        }

        private static int GetProtocolValue(string protocol)
        {
            return protocol.ToUpper() switch
            {
                "TCP" => 6,
                "UDP" => 17,
                _ => 256 // Any
            };
        }

        /// <summary>
        /// Gets the current firewall state for the private profile
        /// </summary>
        private bool GetFirewallState()
        {
            try
            {
                // For now, assume firewall is enabled
                // TODO: Implement actual firewall state detection via COM
                return true;
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error getting firewall state: {ex.Message}");
                return true; // Default to enabled for safety
            }
        }

        /// <summary>
        /// Gets the best interface for routing to the target host
        /// </summary>
        private int GetBestInterface(string targetHost)
        {
            try
            {
                if (string.IsNullOrEmpty(targetHost) || targetHost == "0.0.0.0" || targetHost == "localhost")
                {
                    return 0; // Loopback interface
                }

                // Use Windows API to get best interface
                var targetIP = IPAddress.Parse(targetHost);
                var targetAddr = BitConverter.ToUInt32(targetIP.GetAddressBytes(), 0);

                var result = NativeMethods.GetBestInterface(targetAddr, out uint bestInterface);
                if (result == NativeMethods.ErrorCodes.NO_ERROR)
                {
                    return (int)bestInterface;
                }

                _logger.Debug($"GetBestInterface failed with error code {result}");
                return 0; // Default to loopback
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error getting best interface for {targetHost}: {ex.Message}");
                return 0; // Default to loopback
            }
        }

        /// <summary>
        /// Gets the network profile for the specified interface
        /// </summary>
        private int GetInterfaceProfile(int interfaceIndex)
        {
            try
            {
                // For now, use Private profile (2) as default
                // TODO: Implement actual interface profile detection via Network List Manager COM
                return 2; // NET_FW_PROFILE2_PRIVATE
            }
            catch (Exception ex)
            {
                _logger.Debug($"Error getting interface profile: {ex.Message}");
                return 2; // Default to Private profile
            }
        }

        /// <summary>
        /// Evaluates firewall rules using Windows Firewall precedence logic
        /// </summary>
        private bool EvaluatePrecedence(List<FirewallRule> rules, bool firewallEnabled, int direction)
        {
            if (!firewallEnabled)
            {
                _logger.Debug("Firewall disabled - allowing all connections");
                return true;
            }

            if (!rules.Any())
            {
                _logger.Debug("No relevant rules found - default deny");
                return false;
            }

            // Windows Firewall precedence: Block > Allow by specificity
            var blockRules = rules.Where(r => r.Action == "Block" && r.IsEnabled).ToList();
            if (blockRules.Any())
            {
                _logger.Debug($"Found {blockRules.Count} blocking rules - connection denied");
                return false; // Block takes precedence
            }

            var allowRules = rules.Where(r => r.Action == "Allow" && r.IsEnabled).ToList();
            if (allowRules.Any())
            {
                _logger.Debug($"Found {allowRules.Count} allowing rules - connection allowed");
                return true;
            }

            _logger.Debug("No relevant allow/block rules found - default deny");
            return false; // Default deny
        }
    }
}