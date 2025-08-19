using System.Threading.Tasks;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for background port status monitoring service
    /// </summary>
    public interface IPortStatusMonitorService
    {
        /// <summary>
        /// Gets the current network status for all monitored connections
        /// </summary>
        /// <returns>Current network status with port and firewall information</returns>
        Task<NetworkStatus> GetNetworkStatusAsync();
    }
}