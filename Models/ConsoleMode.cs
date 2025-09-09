using System;

namespace SharpBridge.Models
{
    /// <summary>
    /// Represents available console UI modes.
    /// </summary>
    public enum ConsoleMode
    {
        /// <summary>
        /// Default dashboard with live service status.
        /// </summary>
        Main = 0,

        /// <summary>
        /// System help screen with configuration and shortcuts.
        /// </summary>
        SystemHelp = 1,

        /// <summary>
        /// Network status and troubleshooting mode.
        /// </summary>
        NetworkStatus = 2,

        /// <summary>
        /// Application initialization progress display.
        /// </summary>
        Initialization = 3
    }
}


