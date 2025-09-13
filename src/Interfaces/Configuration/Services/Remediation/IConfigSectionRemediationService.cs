// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Threading.Tasks;
using SharpBridge.Models.Configuration;

namespace SharpBridge.Interfaces.Configuration.Services.Remediation
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
        /// <returns>A tuple indicating the remediation result and the updated configuration section (if changes were made)</returns>
        Task<(RemediationResult Result, IConfigSection? UpdatedConfig)> Remediate(List<ConfigFieldState> fieldsState);
    }
}
