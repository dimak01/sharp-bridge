using System;

namespace SharpBridge.Models
{
    /// <summary>
    /// Tracks minimum and maximum values for a parameter
    /// </summary>
    public class ParameterExtremums
    {
        /// <summary>
        /// Minimum value observed for this parameter
        /// </summary>
        public double Min { get; set; }

        /// <summary>
        /// Maximum value observed for this parameter
        /// </summary>
        public double Max { get; set; }

        /// <summary>
        /// Whether extremums have been initialized with actual values
        /// </summary>
        public bool IsInitialized { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterExtremums"/> class.
        /// </summary>
        public ParameterExtremums()
        {
            IsInitialized = false;
        }

        /// <summary>
        /// Updates extremums with a new value if it exceeds current min/max
        /// </summary>
        /// <param name="value">The new value to check</param>
        public void UpdateExtremums(double value)
        {
            if (!IsInitialized)
            {
                Min = value;
                Max = value;
                IsInitialized = true;
                return;
            }

            if (value < Min)
            {
                Min = value;
            }

            if (value > Max)
            {
                Max = value;
            }
        }

        /// <summary>
        /// Resets extremums to uninitialized state
        /// </summary>
        public void Reset()
        {
            IsInitialized = false;
        }
    }
}