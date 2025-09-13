// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Threading.Tasks;
using SharpBridge.Interfaces.Infrastructure.Services;
using SharpBridge.Interfaces.UI.Components;
using SharpBridge.Models;
using SharpBridge.Models.UI;

namespace SharpBridge.Interfaces.UI.Managers
{
    /// <summary>
    /// Interface for managing console UI modes and delegating rendering to active mode renderers
    /// </summary>
    public interface IConsoleModeManager
    {
        /// <summary>
        /// Gets the currently active console mode
        /// </summary>
        ConsoleMode CurrentMode { get; }

        /// <summary>
        /// Gets the main status renderer for accessing formatters (temporary compatibility)
        /// </summary>
        IMainStatusRenderer MainStatusRenderer { get; }

        /// <summary>
        /// Toggles the specified mode. If the mode is already active, returns to Main mode.
        /// If the mode is not active, switches to that mode.
        /// </summary>
        /// <param name="mode">The mode to toggle</param>
        void Toggle(ConsoleMode mode);

        /// <summary>
        /// Forces the console to the specified mode
        /// </summary>
        /// <param name="mode">The mode to set as active</param>
        void SetMode(ConsoleMode mode);

        /// <summary>
        /// Updates the active mode renderer with current service statistics and configuration.
        /// Respects each renderer's preferred update interval to avoid over-rendering.
        /// </summary>
        /// <param name="stats">Current service statistics</param>
        void Update(IEnumerable<IServiceStats> stats);

        /// <summary>
        /// Clears the console display
        /// </summary>
        void Clear();

        /// <summary>
        /// Forwards the "open in external editor" request to the currently active mode renderer
        /// </summary>
        /// <returns>True if the editor was successfully opened, false otherwise</returns>
        Task<bool> TryOpenActiveModeInEditorAsync();
    }
}
