using SharpBridge.Models;
using SharpBridge.Models.Configuration;
using SharpBridge.Models.Infrastructure;

namespace SharpBridge.Interfaces.UI.Formatters
{
    /// <summary>
    /// Interface for network troubleshooting display in system help
    /// </summary>
    public interface INetworkStatusFormatter
    {
        /// <summary>
        /// Renders network troubleshooting section for system help
        /// </summary>
        /// <param name="networkStatus">Current network status to display</param>
        /// <param name="applicationConfig">Application configuration containing connection settings</param>
        /// <returns>Formatted network troubleshooting content</returns>
        string RenderNetworkTroubleshooting(NetworkStatus networkStatus, ApplicationConfig applicationConfig);
    }
}