// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Core.Engines;
using SharpBridge.Interfaces.Infrastructure.Services;
using SharpBridge.Models;
using SharpBridge.Models.Infrastructure;

namespace SharpBridge.Infrastructure.Services
{
    /// <summary>
    /// Windows Firewall Analyzer that uses the rule engine for analysis
    /// </summary>
    public class WindowsFirewallAnalyzer : IFirewallAnalyzer
    {
        private readonly IFirewallEngine _ruleEngine;
        private readonly IAppLogger _logger;

        /// <summary>
        /// Creates a new instance of the Windows firewall analyzer.
        /// </summary>
        /// <param name="ruleEngine">The firewall engine to use for analysis.</param>
        /// <param name="logger">The logger to use for logging.</param>
        public WindowsFirewallAnalyzer(IFirewallEngine ruleEngine, IAppLogger logger)
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
                var firewallState = _ruleEngine.GetFirewallState();
                var targetInterface = _ruleEngine.GetBestInterface(remoteHost);
                var interfaceProfile = _ruleEngine.GetInterfaceProfile(targetInterface);

                // 2. Get Default Actions for both directions
                var defaultInboundAction = _ruleEngine.GetDefaultAction(1, interfaceProfile); // Inbound
                var defaultOutboundAction = _ruleEngine.GetDefaultAction(2, interfaceProfile); // Outbound

                // 3. Connection Direction Analysis
                var direction = localPort != null ? 1 : 2; // 1 = Inbound, 2 = Outbound
                var protocolValue = GetProtocolValue(protocol);

                _logger.Debug($"Analyzing {protocol} connection: direction={(direction == 1 ? "inbound" : "outbound")}, " +
                             $"target={remoteHost}:{remotePort}, interface={targetInterface}, profile={interfaceProfile}, firewall={firewallState}");

                // 4. Get Relevant Rules
                var relevantRules = _ruleEngine.GetRelevantRules(
                    direction: direction,
                    protocol: protocolValue,
                    profile: interfaceProfile,
                    targetHost: remoteHost,
                    targetPort: remotePort,
                    localPort: localPort);

                // 5. Apply Windows Firewall Precedence Logic
                var isAllowed = EvaluatePrecedence(relevantRules, firewallState, direction, defaultInboundAction, defaultOutboundAction);

                _logger.Debug($"Found {relevantRules.Count} relevant rules, connection allowed: {isAllowed}");

                return new FirewallAnalysisResult
                {
                    IsAllowed = isAllowed,
                    RelevantRules = relevantRules,
                    DefaultActionAllowed = (direction == 1 ? defaultInboundAction : defaultOutboundAction),
                    ProfileName = interfaceProfile switch
                    {
                        1 => "Domain",
                        2 => "Private",
                        4 => "Public",
                        _ => "Unknown"
                    }
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
        /// Evaluates firewall rules using Windows Firewall precedence logic
        /// </summary>
        private bool EvaluatePrecedence(List<FirewallRule> rules, bool firewallEnabled, int direction, bool defaultInboundAction, bool defaultOutboundAction)
        {
            if (!firewallEnabled)
            {
                _logger.Debug("Firewall disabled - allowing all connections");
                return true;
            }

            if (rules.Count == 0)
            {
                // No explicit rules found - use the appropriate default action
                var defaultAction = direction == 1 ? defaultInboundAction : defaultOutboundAction;
                var directionName = direction == 1 ? "inbound" : "outbound";
                var actionName = defaultAction ? "allow" : "deny";
                _logger.Debug($"No relevant rules found - using default {actionName} for {directionName} connections");
                return defaultAction;
            }

            // Windows Firewall precedence: Block > Allow by specificity
            var blockRules = rules.Where(r => r.Action == "Block" && r.IsEnabled).ToList();
            if (blockRules.Count > 0)
            {
                _logger.Debug($"Found {blockRules.Count} blocking rules - connection denied");
                return false; // Block takes precedence
            }

            var allowRules = rules.Where(r => r.Action == "Allow" && r.IsEnabled).ToList();
            if (allowRules.Count > 0)
            {
                _logger.Debug($"Found {allowRules.Count} allowing rules - connection allowed");
                return true;
            }

            // No explicit allow/block rules found - use the appropriate default action
            var defaultActionForNoAllowBlock = direction == 1 ? defaultInboundAction : defaultOutboundAction;
            var directionName2 = direction == 1 ? "inbound" : "outbound";
            var actionName2 = defaultActionForNoAllowBlock ? "allow" : "deny";
            _logger.Debug($"No relevant allow/block rules found - using default {actionName2} for {directionName2} connections");
            return defaultActionForNoAllowBlock;
        }
    }
}