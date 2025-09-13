// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System.Threading.Tasks;

namespace SharpBridge.Interfaces.Configuration.Services.Remediation
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
