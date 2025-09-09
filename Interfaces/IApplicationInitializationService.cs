using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Service responsible for handling application initialization
    /// </summary>
    public interface IApplicationInitializationService
    {
        /// <summary>
        /// Initializes the application by setting up all required components
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <param name="finalSetupActions">Optional list of actions to execute during final setup phase</param>
        /// <returns>A task that completes when initialization is done</returns>
        Task InitializeAsync(CancellationToken cancellationToken, List<Action>? finalSetupActions = null);
    }
}
