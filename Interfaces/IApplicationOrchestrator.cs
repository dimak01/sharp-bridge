using System;
using System.Threading;
using System.Threading.Tasks;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Orchestrates the application lifecycle and coordinates component interactions
    /// </summary>
    public interface IApplicationOrchestrator : IDisposable
    {
        /// <summary>
        /// Initializes components and establishes connections
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>A task that completes when initialization and connection are done</returns>
        Task InitializeAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Starts the data flow between components and runs until cancelled
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>A task that completes when the orchestrator is stopped</returns>
        Task RunAsync(CancellationToken cancellationToken);
    }
}