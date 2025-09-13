// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

namespace SharpBridge.Interfaces.Core.Services
{
    /// <summary>
    /// Abstraction for process-related information.
    /// This interface allows for testing without dependencies on Process.GetCurrentProcess().
    /// </summary>
    public interface IProcessInfo
    {
        /// <summary>
        /// Gets the full path to the current executable.
        /// </summary>
        /// <returns>Full path to the current process executable, or null if unavailable</returns>
        string? GetCurrentExecutablePath();
    }
}














