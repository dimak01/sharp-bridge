using System;
using System.Threading;
using System.Threading.Tasks;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Main bridge service connecting iPhone tracking to VTube Studio
    /// </summary>
    public interface IBridgeService
    {
        /// <summary>
        /// Starts the bridge service
        /// </summary>
        /// <param name="iphoneIp">IP address of the iPhone</param>
        /// <param name="transformConfigPath">Path to the transformation configuration file</param>
        /// <param name="cancellationToken">Cancellation token to stop the service</param>
        /// <returns>An asynchronous operation that completes when stopped</returns>
        Task RunAsync(string iphoneIp, string transformConfigPath, CancellationToken cancellationToken);
    }
} 