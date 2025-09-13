using System;
using System.ComponentModel;

namespace SharpBridge.Models.UI
{
    /// <summary>
    /// Represents the status of an initialization step
    /// </summary>
    public enum StepStatus
    {
        /// <summary>
        /// Step has not yet started
        /// </summary>
        [Description("Pending")]
        Pending,

        /// <summary>
        /// Step is currently in progress
        /// </summary>
        [Description("In Progress")]
        InProgress,

        /// <summary>
        /// Step completed successfully
        /// </summary>
        [Description("Completed")]
        Completed,

        /// <summary>
        /// Step failed with an error
        /// </summary>
        [Description("Failed")]
        Failed
    }
}
