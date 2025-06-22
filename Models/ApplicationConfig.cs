using System;

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
    }
} 