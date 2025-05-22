using SharpBridge.Interfaces;
using System;

namespace SharpBridge.Services
{
    /// <summary>
    /// A simple recovery policy that uses a fixed delay between recovery attempts
    /// </summary>
    public class SimpleRecoveryPolicy : IRecoveryPolicy
    {
        private readonly TimeSpan _delay;

        /// <summary>
        /// Creates a new instance of SimpleRecoveryPolicy
        /// </summary>
        /// <param name="delay">The fixed delay between recovery attempts</param>
        public SimpleRecoveryPolicy(TimeSpan delay)
        {
            _delay = delay;
        }

        /// <inheritdoc/>
        public TimeSpan GetNextDelay()
        {
            return _delay;
        }
    }
} 