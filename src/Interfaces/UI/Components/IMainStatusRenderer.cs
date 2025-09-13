// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using SharpBridge.Interfaces.UI.Formatters;
using SharpBridge.Models;

namespace SharpBridge.Interfaces.UI.Components
{
    /// <summary>
    /// Interface for rendering service information to the console or other display
    /// </summary>
    public interface IMainStatusRenderer
    {
        /// <summary>
        /// Registers a formatter for a specific entity type
        /// </summary>
        void RegisterFormatter<T>(IFormatter formatter) where T : IFormattableObject;

        /// <summary>
        /// Gets a formatter for the specified type
        /// </summary>
        IFormatter? GetFormatter<T>() where T : IFormattableObject;


        /// <summary>
        /// Clears the console
        /// </summary>
        void ClearConsole();
    }
}