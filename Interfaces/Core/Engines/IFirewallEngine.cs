using System.Collections.Generic;
using SharpBridge.Models.Infrastructure;

namespace SharpBridge.Interfaces.Core.Engines
{
    /// <summary>
    /// Primary firewall analysis interface responsible for enumerating, normalizing,
    /// and filtering Windows Firewall rules for connectivity troubleshooting.
    /// </summary>
    /// <remarks>
    /// Implementations should abstract all OS-specific details (COM/NLM/Win32) behind
    /// injected dependencies so the engine itself stays deterministic and testable.
    /// Direction values are 1 = Inbound, 2 = Outbound. Protocol values follow Windows
    /// conventions: 6 = TCP, 17 = UDP, 256 = Any.
    /// </remarks>
    public interface IFirewallEngine
    {
        /// <summary>
        /// Gets all relevant firewall rules for a specific connection scenario.
        /// </summary>
        /// <param name="direction">Firewall direction. 1 = Inbound, 2 = Outbound.</param>
        /// <param name="protocol">Protocol. 6 = TCP, 17 = UDP, 256 = Any.</param>
        /// <param name="profile">Network profile bitmask (see <c>NetFwProfile2</c>).</param>
        /// <param name="targetHost">Target IPv4 address or special values handled by normalization (e.g., "*", "any"). Optional.</param>
        /// <param name="targetPort">Target port. Supports single port or wildcard ("*") depending on rule. Optional.</param>
        /// <param name="localPort">Local port for inbound connections. Optional.</param>
        /// <returns>List of relevant firewall rules matching the given constraints.</returns>
        List<FirewallRule> GetRelevantRules(
            int direction,
            int protocol,
            int profile,
            string? targetHost = null,
            string? targetPort = null,
            string? localPort = null);

        /// <summary>
        /// Checks if a firewall rule applies to the current application (SharpBridge.exe).
        /// </summary>
        /// <param name="rule">The firewall rule (may be a COM rule or mapped model).</param>
        /// <returns>True if the rule applies to the current SharpBridge executable.</returns>
        bool IsApplicationRule(object rule);

        /// <summary>
        /// Checks if a firewall rule is enabled and should be considered in analysis.
        /// </summary>
        /// <param name="rule">The firewall rule to check</param>
        /// <returns>True if the rule is enabled</returns>
        bool IsRuleEnabled(object rule);

        /// <summary>
        /// Checks if a firewall rule applies to a specific network profile.
        /// </summary>
        /// <param name="rule">The firewall rule to check</param>
        /// <param name="profile">The network profile to check against</param>
        /// <returns>True if the rule applies to the specified profile</returns>
        bool IsProfileRule(FirewallRule rule, int profile);

        /// <summary>
        /// Checks if a firewall rule applies to a specific protocol.
        /// </summary>
        /// <param name="rule">The firewall rule to check</param>
        /// <param name="protocol">The protocol to check against (6 = TCP, 17 = UDP, 256 = Any).</param>
        /// <returns>True if the rule applies to the specified protocol</returns>
        bool IsProtocolRule(FirewallRule rule, int protocol);

        /// <summary>
        /// Checks if a firewall rule matches a specific target host and port.
        /// </summary>
        /// <param name="rule">The firewall rule to check</param>
        /// <param name="targetHost">Target IPv4 address (after normalization) to compare with rule's remote address constraint.</param>
        /// <param name="targetPort">Target port to compare with rule's remote port constraint.</param>
        /// <returns>True if the rule matches the target</returns>
        bool IsTargetMatch(FirewallRule rule, string targetHost, string targetPort);

        /// <summary>
        /// Checks if a port is within a port range specified in a firewall rule.
        /// </summary>
        /// <param name="port">Port to check.</param>
        /// <param name="rulePort">Rule remote port specification. Supports single (e.g., "28960"), range (e.g., "28960-28970"), or wildcard ("*", "any").</param>
        /// <returns>True if the port is within the rule's port range</returns>
        bool IsPortInRange(string port, string rulePort);

        /// <summary>
        /// Checks if a host IP is within a subnet specified in a firewall rule.
        /// </summary>
        /// <param name="host">Host IPv4 address to check.</param>
        /// <param name="subnet">Subnet specification from rule. Supports wildcard ("*", "any", "0.0.0.0") and CIDR (e.g., "192.168.1.0/24").</param>
        /// <returns>True if the host is within the rule's subnet</returns>
        bool IsHostInSubnet(string host, string subnet);

        /// <summary>
        /// Normalizes address values from Windows Firewall rules.
        /// Handles wildcards and special values like "*", "any", "&lt;localsubnet&gt;", etc.
        /// </summary>
        /// <param name="address">Address to normalize.</param>
        /// <returns>Normalized address suitable for rule matching.</returns>
        string NormalizeAddress(string address);

        /// <summary>
        /// Gets the default firewall action for a specific direction and profile.
        /// </summary>
        /// <param name="direction">1 for Inbound, 2 for Outbound.</param>
        /// <param name="profile">Profile type (Domain/Private/Public).</param>
        /// <returns><c>true</c> if default action is Allow; <c>false</c> if Block.</returns>
        bool GetDefaultAction(int direction, int profile);

        /// <summary>
        /// Gets the current Windows Firewall state.
        /// Checks if the firewall is enabled for any active network profile.
        /// </summary>
        /// <returns>True if firewall is enabled, false if disabled</returns>
        bool GetFirewallState();

        /// <summary>
        /// Gets the current active network profiles.
        /// </summary>
        /// <returns>Bitwise combination of active profiles (Domain=1, Private=2, Public=4)</returns>
        int GetCurrentProfiles();

        /// <summary>
        /// Gets the network profile for a specific network interface.
        /// Maps Windows interface index to firewall profile using Network List Manager.
        /// </summary>
        /// <param name="interfaceIndex">Windows interface index from <see cref="GetBestInterface(string)"/>.</param>
        /// <returns>Network profile (Domain=1, Private=2, Public=4)</returns>
        int GetInterfaceProfile(int interfaceIndex);

        /// <summary>
        /// Gets the best network interface for routing to a specific target host.
        /// Uses Windows GetBestInterface() API to determine the optimal routing interface.
        /// </summary>
        /// <param name="targetHost">Target host IPv4 address or hostname.</param>
        /// <returns>Windows interface index; 1 for loopback; 0 if unknown/unresolved.</returns>
        int GetBestInterface(string targetHost);
    }
}