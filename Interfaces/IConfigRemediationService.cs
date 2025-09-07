using System.Threading.Tasks;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for the configuration remediation service that orchestrates validation and remediation.
    /// </summary>
    public interface IConfigRemediationService
    {
        /// <summary>
        /// Runs the complete configuration remediation process.
        /// Validates all sections and runs remediation for any invalid ones.
        /// </summary>
        /// <returns>True if all configuration issues were resolved, false otherwise</returns>
        Task<bool> RemediateConfigurationAsync();
    }
}
