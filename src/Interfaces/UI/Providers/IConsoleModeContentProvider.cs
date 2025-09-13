// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Threading.Tasks;
using SharpBridge.Interfaces.UI.Components;
using SharpBridge.Models;
using SharpBridge.Models.Domain;
using SharpBridge.Models.UI;

namespace SharpBridge.Interfaces.UI.Providers
{
    /// <summary>
    /// Unified interface for all console UI mode renderers.
    /// Implementations are responsible for fully rendering their view.
    /// </summary>
    public interface IConsoleModeContentProvider
    {
        /// <summary>
        /// Mode identifier implemented by this renderer.
        /// </summary>
        ConsoleMode Mode { get; }

        /// <summary>
        /// Human-readable name of the mode.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Shortcut action that toggles this mode (as configured by the user).
        /// </summary>
        ShortcutAction ToggleAction { get; }

        /// <summary>
        /// Attempts to open the context-appropriate configuration file in the external editor.
        /// Implementations decide what to open (e.g., transformation vs application config).
        /// </summary>
        Task<bool> TryOpenInExternalEditorAsync();

        /// <summary>
        /// Called when the mode becomes active.
        /// </summary>
        /// <param name="console">Console abstraction to use for output/clearing.</param>
        void Enter(IConsole console);

        /// <summary>
        /// Called when the mode becomes inactive.
        /// </summary>
        /// <param name="console">Console abstraction to use for cleanup/clearing.</param>
        void Exit(IConsole console);

        /// <summary>
        /// Renders the current view for this mode. Implementations should write directly via <see cref="IConsole"/>.
        /// </summary>
        /// <param name="context">Rendering context containing live data and environment details.</param>
        string[] GetContent(ConsoleRenderContext context);

        /// <summary>
        /// Preferred update cadence for this mode. The mode manager may clamp or align this interval.
        /// </summary>
        TimeSpan PreferredUpdateInterval { get; }
    }
}


