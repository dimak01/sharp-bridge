using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for platform-specific firewall rule analysis
    /// </summary>
    public interface IFirewallAnalyzer
    {
        /// <summary>
        /// Analyzes firewall rules to determine if outbound connectivity is allowed
        /// </summary>
        /// <param name="localPort">Local port number (can be null for outbound-only analysis)</param>
        /// <param name="remoteHost">Remote host address</param>
        /// <param name="remotePort">Remote port number</param>
        /// <param name="protocol">Protocol (UDP, TCP, etc.)</param>
        /// <returns>Firewall analysis result with allowed status and relevant rules</returns>
        FirewallAnalysisResult AnalyzeFirewallRules(
            string? localPort,
            string remoteHost,
            string remotePort,
            string protocol);
    }
}