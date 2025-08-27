using System.Collections.Generic;
using System.Threading.Tasks;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Service for handling first-time setup of missing configuration fields
    /// </summary>
    public interface IFirstTimeSetupService
    {
        /// <summary>
        /// Prompts the user to provide values for missing configuration fields
        /// </summary>
        /// <param name="missingFields">Collection of fields that need to be configured</param>
        /// <param name="currentConfig">Current application configuration (immutable)</param>
        /// <returns>Task with setup result and updated configuration if successful</returns>
        Task<(bool Success, ApplicationConfig? UpdatedConfig)> RunSetupAsync(IEnumerable<MissingField> missingFields, ApplicationConfig currentConfig);
    }
}
