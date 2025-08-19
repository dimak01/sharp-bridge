using SharpBridge.Interfaces;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Windows-specific network command generation
    /// </summary>
    public class WindowsNetworkCommandProvider : INetworkCommandProvider
    {
        /// <summary>
        /// Gets the platform name for display purposes
        /// </summary>
        public string GetPlatformName() => "Windows";

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
        public string GetAddFirewallRuleCommand(
            string ruleName,
            string direction,
            string action,
            string protocol,
            string? localPort = null,
            string? remotePort = null,
            string? remoteAddress = null)
        {
            var command = $"netsh advfirewall firewall add rule name=\"{ruleName}\" dir={direction} action={action} protocol={protocol}";

            if (!string.IsNullOrEmpty(localPort))
            {
                command += $" localport={localPort}";
            }

            if (!string.IsNullOrEmpty(remotePort))
            {
                command += $" remoteport={remotePort}";
            }

            if (!string.IsNullOrEmpty(remoteAddress))
            {
                command += $" remoteip={remoteAddress}";
            }

            return command;
        }

        /// <summary>
        /// Generates a command to remove a firewall rule
        /// </summary>
        /// <param name="ruleName">Name of the firewall rule to remove</param>
        /// <returns>Copy-paste ready command string</returns>
        public string GetRemoveFirewallRuleCommand(string ruleName)
        {
            return $"netsh advfirewall firewall delete rule name=\"{ruleName}\"";
        }

        /// <summary>
        /// Generates a command to check port status
        /// </summary>
        /// <param name="port">Port number to check</param>
        /// <param name="protocol">Protocol (UDP/TCP)</param>
        /// <returns>Copy-paste ready command string</returns>
        public string GetCheckPortStatusCommand(string port, string protocol)
        {
            return $"netstat -an | findstr :{port}";
        }

        /// <summary>
        /// Generates a command to test connectivity to a host
        /// </summary>
        /// <param name="host">Host address to test</param>
        /// <param name="port">Port number to test</param>
        /// <returns>Copy-paste ready command string</returns>
        public string GetTestConnectivityCommand(string host, string port)
        {
            return $"Test-NetConnection -ComputerName {host} -Port {port} -InformationLevel Detailed";
        }
    }
}