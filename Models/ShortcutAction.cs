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
        CycleTransformationEngineVerbosity,

        /// <summary>
        /// Cycle the PC client display verbosity (Basic → Normal → Detailed)
        /// </summary>
        CyclePCClientVerbosity,

        /// <summary>
        /// Cycle the Phone client display verbosity (Basic → Normal → Detailed)
        /// </summary>
        CyclePhoneClientVerbosity,

        /// <summary>
        /// Reload the transformation configuration from disk
        /// </summary>
        ReloadTransformationConfig,

        /// <summary>
        /// Open the transformation configuration file in external editor
        /// </summary>
        OpenConfigInEditor,

        /// <summary>
        /// Show the system help information (F1)
        /// </summary>
        ShowSystemHelp
    }
}