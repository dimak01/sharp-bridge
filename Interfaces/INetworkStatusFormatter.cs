using SharpBridge.Models;

namespace SharpBridge.Interfaces
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
        /// <returns>Formatted network troubleshooting content</returns>
        string RenderNetworkTroubleshooting(NetworkStatus networkStatus);
    }
}