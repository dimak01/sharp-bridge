using System.Threading.Tasks;
using SharpBridge.Models;
using SharpBridge.Models.Infrastructure;

namespace SharpBridge.Interfaces.Core.Services
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