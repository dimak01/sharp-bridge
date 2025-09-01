using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for first-time setup services that remediate configuration issues.
    /// Each configuration section type should have its own setup service implementation.
    /// </summary>
    public interface IConfigSectionFirstTimeSetupService
    {
        /// <summary>
        /// Runs the first-time setup for a configuration section to fix missing or invalid fields.
        /// </summary>
        /// <param name="fieldsState">The raw state of all fields in the configuration section</param>
        /// <returns>A tuple indicating success and the updated configuration section</returns>
        Task<(bool Success, IConfigSection? UpdatedConfig)> RunSetupAsync(List<ConfigFieldState> fieldsState);
    }
}
