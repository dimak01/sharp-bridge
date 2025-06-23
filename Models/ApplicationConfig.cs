using System;
using System.Collections.Generic;

namespace SharpBridge.Models
{
    /// <summary>
    /// Configuration for general application settings
    /// </summary>
    public class ApplicationConfig
    {
        /// <summary>
        /// Command to execute when opening files in external editor.
        /// Use %f as placeholder for file path.
        /// </summary>
        public string EditorCommand { get; set; } = "notepad.exe \"%f\"";

        /// <summary>
        /// Dictionary of keyboard shortcuts mapped to actions.
        /// Key is the ShortcutAction name, value is the shortcut string (e.g., "Alt+T").
        /// </summary>
        public Dictionary<string, string> Shortcuts { get; set; } = new Dictionary<string, string>();
    }
}