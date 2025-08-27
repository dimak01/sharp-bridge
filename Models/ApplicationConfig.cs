using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SharpBridge.Models
{
    /// <summary>
    /// Configuration for general application settings
    /// </summary>
    public class GeneralSettingsConfig
    {
        /// <summary>
        /// Command to execute when opening files in external editor.
        /// Use %f as placeholder for file path.
        /// </summary>
        [Description("External Editor Command")]
        public string EditorCommand { get; set; } = "notepad.exe \"%f\"";

        /// <summary>
        /// Dictionary of keyboard shortcuts mapped to actions.
        /// Key is the ShortcutAction name, value is the shortcut string (e.g., "Alt+T").
        /// </summary>
        [Description("Keyboard Shortcuts")]
        public Dictionary<string, string> Shortcuts { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Consolidated application configuration that aggregates all configuration sections
    /// </summary>
    public class ApplicationConfig
    {
        /// <summary>
        /// Configuration version for migration support
        /// </summary>
        public const int CurrentVersion = 1;

        /// <summary>
        /// Version of this configuration instance
        /// </summary>
        public int Version { get; init; } = CurrentVersion;
        /// <summary>
        /// General application settings (editor, shortcuts)
        /// </summary>
        public GeneralSettingsConfig GeneralSettings { get; set; } = new();

        /// <summary>
        /// Phone client settings for connecting to iPhone VTube Studio
        /// </summary>
        public VTubeStudioPhoneClientConfig PhoneClient { get; set; } = new();

        /// <summary>
        /// PC client settings for connecting to VTube Studio on PC
        /// </summary>
        public VTubeStudioPCConfig PCClient { get; set; } = new();

        /// <summary>
        /// Transformation engine settings (config path, max iterations)
        /// </summary>
        public TransformationEngineConfig TransformationEngine { get; set; } = new();
    }
}