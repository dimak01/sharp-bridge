namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for platform-specific network command generation
    /// </summary>
    public interface INetworkCommandProvider
    {
        /// <summary>
        /// Gets the platform name for display purposes
        /// </summary>
        string GetPlatformName();

        /// <summary>
        /// Generates a command to add a firewall rule
        /// </summary>
        /// <param name="ruleName">Name for the firewall rule</param>
        /// <param name="direction">Direction (in/out)</param>
        /// <param name="action">Action (allow/block)</param>
        /// <param name="protocol">Protocol (UDP/TCP)</param>
        /// <param name="localPort">Local port (optional)</param>
        /// <param name="remotePort">Remote port (optional)</param>
        /// <param name="remoteAddress">Remote address (optional)</param>
        /// <returns>Copy-paste ready command string</returns>
        string GetAddFirewallRuleCommand(
            string ruleName,
            string direction,
            string action,
            string protocol,
            string? localPort = null,
            string? remotePort = null,
            string? remoteAddress = null);

        /// <summary>
        /// Generates a command to remove a firewall rule
        /// </summary>
        /// <param name="ruleName">Name of the firewall rule to remove</param>
        /// <returns>Copy-paste ready command string</returns>
        string GetRemoveFirewallRuleCommand(string ruleName);

        /// <summary>
        /// Generates a command to check port status
        /// </summary>
        /// <param name="port">Port number to check</param>
        /// <param name="protocol">Protocol (UDP/TCP)</param>
        /// <returns>Copy-paste ready command string</returns>
        string GetCheckPortStatusCommand(string port, string protocol);

        /// <summary>
        /// Generates a command to test connectivity to a host
        /// </summary>
        /// <param name="host">Host address to test</param>
        /// <param name="port">Port number to test</param>
        /// <returns>Copy-paste ready command string</returns>
        string GetTestConnectivityCommand(string host, string port);
    }
}