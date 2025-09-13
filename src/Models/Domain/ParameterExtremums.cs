using System;

namespace SharpBridge.Models.Domain
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
        private bool _hasBeenInitialized = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterExtremums"/> class.
        /// </summary>
        public ParameterExtremums()
        {
            // Min and Max will be set to the first value when UpdateExtremums is called
        }

        /// <summary>
        /// Updates extremums with a new value if it exceeds current min/max
        /// </summary>
        /// <param name="value">The new value to check</param>
        public void UpdateExtremums(double value)
        {
            // If we haven't been initialized yet, this is the first value
            if (!_hasBeenInitialized)
            {
                Min = value;
                Max = value;
                _hasBeenInitialized = true;
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
            Min = 0;
            Max = 0;
            _hasBeenInitialized = false;
        }

        /// <summary>
        /// Gets whether extremums have been initialized with actual values
        /// </summary>
        public bool HasExtremums => _hasBeenInitialized;
    }
}