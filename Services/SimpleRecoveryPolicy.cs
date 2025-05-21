using System;

namespace SharpBridge.Services
{
    /// <summary>
    /// Simple recovery policy that uses a consistent retry interval
    /// </summary>
    public class SimpleRecoveryPolicy
    {
        private readonly TimeSpan _retryInterval;
        
        /// <summary>
        /// Creates a new instance of SimpleRecoveryPolicy
        /// </summary>
        /// <param name="retryInterval">The interval between recovery attempts</param>
        public SimpleRecoveryPolicy(TimeSpan retryInterval)
        {
            _retryInterval = retryInterval;
        }
        
        /// <summary>
        /// Gets the delay until the next recovery attempt
        /// </summary>
        /// <returns>The time to wait before the next recovery attempt</returns>
        public TimeSpan GetNextDelay()
        {
            return _retryInterval;
        }
    }
} 