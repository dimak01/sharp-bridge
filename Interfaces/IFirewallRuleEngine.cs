using System.Collections.Generic;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for Windows Firewall rule enumeration and filtering.
    /// Handles the core rule analysis engine functionality for firewall analysis.
    /// </summary>
    public interface IFirewallRuleEngine
    {
        /// <summary>
        /// Gets all relevant firewall rules for a specific connection scenario.
        /// </summary>
        /// <param name="direction">Firewall direction (inbound/outbound)</param>
        /// <param name="protocol">Protocol (UDP/TCP)</param>
        /// <param name="profile">Network profile (Domain/Private/Public/All)</param>
        /// <param name="targetHost">Target host IP address (optional for protocol-only rules)</param>
        /// <param name="targetPort">Target port (optional for protocol-only rules)</param>
        /// <param name="localPort">Local port for inbound connections (optional)</param>
        /// <returns>List of relevant firewall rules</returns>
        List<Models.FirewallRule> GetRelevantRules(
            int direction,
            int protocol,
            int profile,
            string? targetHost = null,
            string? targetPort = null,
            string? localPort = null);

        /// <summary>
        /// Checks if a firewall rule applies to the current application (SharpBridge.exe).
        /// </summary>
        /// <param name="rule">The firewall rule to check</param>
        /// <returns>True if the rule applies to SharpBridge.exe</returns>
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
        bool IsProfileRule(Models.FirewallRule rule, int profile);

        /// <summary>
        /// Checks if a firewall rule applies to a specific protocol.
        /// </summary>
        /// <param name="rule">The firewall rule to check</param>
        /// <param name="protocol">The protocol to check against</param>
        /// <returns>True if the rule applies to the specified protocol</returns>
        bool IsProtocolRule(Models.FirewallRule rule, int protocol);

        /// <summary>
        /// Checks if a firewall rule matches a specific target host and port.
        /// </summary>
        /// <param name="rule">The firewall rule to check</param>
        /// <param name="targetHost">Target host IP address</param>
        /// <param name="targetPort">Target port</param>
        /// <returns>True if the rule matches the target</returns>
        bool IsTargetMatch(Models.FirewallRule rule, string targetHost, string targetPort);

        /// <summary>
        /// Checks if a port is within a port range specified in a firewall rule.
        /// </summary>
        /// <param name="port">Port to check</param>
        /// <param name="rulePort">Port specification from rule (may be range like "28960-28970")</param>
        /// <returns>True if the port is within the rule's port range</returns>
        bool IsPortInRange(string port, string rulePort);

        /// <summary>
        /// Checks if a host IP is within a subnet specified in a firewall rule.
        /// </summary>
        /// <param name="host">Host IP to check</param>
        /// <param name="subnet">Subnet specification from rule (may be CIDR like "192.168.1.0/24")</param>
        /// <returns>True if the host is within the rule's subnet</returns>
        bool IsHostInSubnet(string host, string subnet);

        /// <summary>
        /// Normalizes address values from Windows Firewall rules.
        /// Handles wildcards and special values like "*", "any", "&lt;localsubnet&gt;", etc.
        /// </summary>
        /// <param name="address">Address to normalize</param>
        /// <returns>Normalized address</returns>
        string NormalizeAddress(string address);

        /// <summary>
        /// Gets the default firewall action for a specific direction and profile.
        /// </summary>
        /// <param name="direction">1 for Inbound, 2 for Outbound</param>
        /// <param name="profile">Profile type (Domain/Private/Public)</param>
        /// <returns>True if default action is Allow, false if Block</returns>
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
        /// <param name="interfaceIndex">Windows interface index from GetBestInterface()</param>
        /// <returns>Network profile (Domain=1, Private=2, Public=4)</returns>
        int GetInterfaceProfile(int interfaceIndex);
    }
}