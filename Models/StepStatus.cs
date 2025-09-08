using System;

namespace SharpBridge.Models
{
    /// <summary>
    /// Represents the status of an initialization step
    /// </summary>
    public enum StepStatus
    {
        /// <summary>
        /// Step has not yet started
        /// </summary>
        Pending,

        /// <summary>
        /// Step is currently in progress
        /// </summary>
        InProgress,

        /// <summary>
        /// Step completed successfully
        /// </summary>
        Completed,

        /// <summary>
        /// Step failed with an error
        /// </summary>
        Failed
    }
}
