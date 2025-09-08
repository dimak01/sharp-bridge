using System;

namespace SharpBridge.Models
{
    /// <summary>
    /// Contains detailed information about a specific initialization step
    /// </summary>
    public class StepInfo
    {
        /// <summary>
        /// Gets or sets the current status of this step
        /// </summary>
        public StepStatus Status { get; set; } = StepStatus.Pending;

        /// <summary>
        /// Gets or sets when this step started
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Gets or sets when this step completed or failed
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Gets or sets the error message if this step failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets the duration of this step
        /// </summary>
        public TimeSpan? Duration => EndTime - StartTime;
    }
}
