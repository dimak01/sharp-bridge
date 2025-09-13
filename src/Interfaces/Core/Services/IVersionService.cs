// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

namespace SharpBridge.Interfaces.Core.Services
{
    /// <summary>
    /// Service for retrieving application version information.
    /// </summary>
    public interface IVersionService
    {
        /// <summary>
        /// Gets the full semantic version string (e.g., "0.5.0-beta.1" or "0.5.0-dev").
        /// </summary>
        /// <returns>The semantic version string.</returns>
        string GetVersion();

        /// <summary>
        /// Gets the formatted display version string (e.g., "Sharp Bridge v0.5.0-beta.1").
        /// </summary>
        /// <returns>The formatted version string for display.</returns>
        string GetDisplayVersion();
    }
}
