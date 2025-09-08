using System;
using System.ComponentModel;

namespace SharpBridge.Models
{
    /// <summary>
    /// Represents the different steps during application initialization
    /// </summary>
    public enum InitializationStep
    {
        /// <summary>
        /// Console window setup and configuration
        /// </summary>
        [Description("Console Setup")]
        ConsoleSetup,

        /// <summary>
        /// Loading transformation rules from configuration
        /// </summary>
        [Description("Loading Transformation Rules")]
        TransformationEngine,

        /// <summary>
        /// Setting up file watchers for configuration changes
        /// </summary>
        [Description("Setting up File Watchers")]
        FileWatchers,

        /// <summary>
        /// Initializing VTube Studio PC client connection
        /// </summary>
        [Description("PC Client")]
        PCClient,

        /// <summary>
        /// Initializing VTube Studio Phone client connection
        /// </summary>
        [Description("Phone Client")]
        PhoneClient,

        /// <summary>
        /// Synchronizing parameters with VTube Studio
        /// </summary>
        [Description("Parameter Sync")]
        ParameterSync,

        /// <summary>
        /// Final setup tasks (keyboard shortcuts, etc.)
        /// </summary>
        [Description("Final Setup")]
        FinalSetup
    }
}
