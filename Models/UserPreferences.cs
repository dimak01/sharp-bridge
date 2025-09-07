using System;
using SharpBridge.Interfaces;

namespace SharpBridge.Models
{
    /// <summary>
    /// User preferences that can be modified at runtime and easily reset
    /// </summary>
    public class UserPreferences
    {

        /// <summary>
        /// Verbosity level for the Phone Client display
        /// </summary>
        public VerbosityLevel PhoneClientVerbosity { get; set; } = VerbosityLevel.Normal;

        /// <summary>
        /// Verbosity level for the PC Client display
        /// </summary>
        public VerbosityLevel PCClientVerbosity { get; set; } = VerbosityLevel.Normal;

        /// <summary>
        /// Verbosity level for the Transformation Engine display
        /// </summary>
        public VerbosityLevel TransformationEngineVerbosity { get; set; } = VerbosityLevel.Normal;

        /// <summary>
        /// Preferred console window width
        /// </summary>
        public int PreferredConsoleWidth { get; set; } = 150;

        /// <summary>
        /// Preferred console window height
        /// </summary>
        public int PreferredConsoleHeight { get; set; } = 60;

        /// <summary>
        /// Customizable columns for PC parameter table display
        /// </summary>
        public ParameterTableColumn[] PCParameterTableColumns { get; set; } = [];
    }
}