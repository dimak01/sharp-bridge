// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System.Threading.Tasks;

namespace SharpBridge.Interfaces.Core.Services
{
    /// <summary>
    /// Service for opening files in external editors
    /// </summary>
    public interface IExternalEditorService
    {
        /// <summary>
        /// Attempts to open the transformation configuration file in the configured external editor
        /// </summary>
        /// <returns>True if the editor was launched successfully, false otherwise</returns>
        Task<bool> TryOpenTransformationConfigAsync();

        /// <summary>
        /// Attempts to open the application configuration file in the configured external editor
        /// </summary>
        /// <returns>True if the editor was launched successfully, false otherwise</returns>
        Task<bool> TryOpenApplicationConfigAsync();
    }
}