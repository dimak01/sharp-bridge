using System;

namespace SharpBridge.Models
{
    /// <summary>
    /// Indicates which configuration file should be opened by the external editor for the active UI mode.
    /// </summary>
    public enum ExternalEditorTarget
    {
        /// <summary>
        /// No associated editor target.
        /// </summary>
        None = 0,

        /// <summary>
        /// Transformation rules configuration.
        /// </summary>
        TransformationConfig = 1,

        /// <summary>
        /// Application configuration.
        /// </summary>
        ApplicationConfig = 2
    }
}


