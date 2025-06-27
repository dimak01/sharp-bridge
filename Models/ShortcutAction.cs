using System.ComponentModel;

namespace SharpBridge.Models
{
    /// <summary>
    /// Enumeration of all available keyboard shortcut actions in the application
    /// </summary>
    public enum ShortcutAction
    {
        /// <summary>
        /// Cycle the Transformation Engine display verbosity (Basic → Normal → Detailed)
        /// </summary>
        [Description("Cycle Transformation Engine Verbosity")]
        CycleTransformationEngineVerbosity,

        /// <summary>
        /// Cycle the PC client display verbosity (Basic → Normal → Detailed)
        /// </summary>
        [Description("Cycle PC Client Verbosity")]
        CyclePCClientVerbosity,

        /// <summary>
        /// Cycle the Phone client display verbosity (Basic → Normal → Detailed)
        /// </summary>
        [Description("Cycle Phone Client Verbosity")]
        CyclePhoneClientVerbosity,

        /// <summary>
        /// Reload the transformation configuration from disk
        /// </summary>
        [Description("Reload Transformation Config")]
        ReloadTransformationConfig,

        /// <summary>
        /// Open the transformation configuration file in external editor
        /// </summary>
        [Description("Open Config in External Editor")]
        OpenConfigInEditor,

        /// <summary>
        /// Show the system help information (F1)
        /// </summary>
        [Description("Show System Help")]
        ShowSystemHelp
    }
}