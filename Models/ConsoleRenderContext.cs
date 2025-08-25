using System;
using System.Collections.Generic;
using System.Threading;
using SharpBridge.Interfaces;

namespace SharpBridge.Models
{
    /// <summary>
    /// Context object passed to console mode renderers for each render tick.
    /// </summary>
    public class ConsoleRenderContext
    {
        /// <summary>
        /// Optional live service statistics for renderers that display streaming status (e.g., Main mode).
        /// </summary>
        public IEnumerable<IServiceStats>? ServiceStats { get; set; }

        /// <summary>
        /// Consolidated application configuration.
        /// </summary>
        public ApplicationConfig ApplicationConfig { get; set; } = null!;

        /// <summary>
        /// User preferences that can influence rendering.
        /// </summary>
        public UserPreferences UserPreferences { get; set; } = null!;

        /// <summary>
        /// Current console size available to the renderer.
        /// </summary>
        public (int Width, int Height) ConsoleSize { get; set; }

        /// <summary>
        /// Cancellation token for cooperative cancellation of long operations.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }
    }
}


