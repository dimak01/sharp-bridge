using System.Net.NetworkInformation;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Abstraction for Windows system API calls.
    /// This interface allows for testing without actual Windows API dependencies.
    /// </summary>
    public interface IWindowsSystemApi
    {
        /// <summary>
        /// Checks if the Windows Firewall service (mpssvc) is running.
        /// </summary>
        /// <returns>True if the firewall service is running, false otherwise</returns>
        bool IsFirewallServiceRunning();

        /// <summary>
        /// Gets the best network interface for reaching a target host using Windows GetBestInterface API.
        /// </summary>
        /// <param name="targetHost">Target IP address or hostname</param>
        /// <returns>Windows interface index, or 0 if unable to determine</returns>
        int GetBestInterface(string targetHost);

        /// <summary>
        /// Gets all network interfaces on the system.
        /// </summary>
        /// <returns>Array of NetworkInterface objects</returns>
        NetworkInterface[] GetAllNetworkInterfaces();
    }
}

