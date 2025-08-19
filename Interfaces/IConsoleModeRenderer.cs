using System;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Unified interface for all console UI mode renderers.
    /// Implementations are responsible for fully rendering their view.
    /// </summary>
    public interface IConsoleModeRenderer
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
        /// The external editor target associated with this mode.
        /// </summary>
        ExternalEditorTarget EditorTarget { get; }

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
        void Render(ConsoleRenderContext context);

        /// <summary>
        /// Preferred update cadence for this mode. The mode manager may clamp or align this interval.
        /// </summary>
        TimeSpan PreferredUpdateInterval { get; }
    }
}


