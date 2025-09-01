using System.Collections.Generic;
using System.Threading.Tasks;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for configuration section remediation services that fix configuration issues.
    /// Each configuration section type should have its own remediation service implementation.
    /// </summary>
    public interface IConfigSectionRemediationService
    {
        /// <summary>
        /// Remediates configuration issues for a configuration section by analyzing field states,
        /// validating them, and prompting the user to fix any problems.
        /// </summary>
        /// <param name="fieldsState">The raw state of all fields in the configuration section</param>
        /// <returns>A tuple indicating success and the updated configuration section</returns>
        Task<(bool Success, IConfigSection? UpdatedConfig)> Remediate(List<ConfigFieldState> fieldsState);
    }
}
