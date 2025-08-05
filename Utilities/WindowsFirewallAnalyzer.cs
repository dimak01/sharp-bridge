using System.Collections.Generic;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Dummy implementation of Windows Firewall Analyzer for end-to-end testing
    /// </summary>
    public class WindowsFirewallAnalyzer : IFirewallAnalyzer
    {
        /// <summary>
        /// Analyzes firewall rules to determine if outbound connectivity is allowed
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
            // Dummy implementation for end-to-end testing
            var result = new FirewallAnalysisResult
            {
                IsAllowed = true, // Assume allowed for now
                RelevantRules = new List<FirewallRule>
                {
                    new FirewallRule
                    {
                        Name = "SharpBridge UDP Outbound",
                        IsEnabled = true,
                        Direction = "Outbound",
                        Action = "Allow",
                        Protocol = protocol,
                        RemotePort = remotePort,
                        RemoteAddress = remoteHost
                    },
                    new FirewallRule
                    {
                        Name = "Default Outbound UDP",
                        IsEnabled = true,
                        Direction = "Outbound",
                        Action = "Allow",
                        Protocol = "UDP"
                    }
                }
            };

            return result;
        }
    }
}