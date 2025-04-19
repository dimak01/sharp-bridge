using System.Collections.Generic;
using SharpBridge.Interfaces;

namespace SharpBridge.Models
{
    /// <summary>
    /// Interface for service statistics with covariance support
    /// </summary>
    public interface IServiceStats
    {
        /// <summary>
        /// The name of the service
        /// </summary>
        string ServiceName { get; }
        
        /// <summary>
        /// The current status of the service
        /// </summary>
        string Status { get; }
        
        /// <summary>
        /// Service-specific counters and metrics
        /// </summary>
        Dictionary<string, long> Counters { get; }
        
        /// <summary>
        /// The current entity being processed by the service
        /// </summary>
        IFormattableObject CurrentEntity { get; }
    }

    /// <summary>
    /// Container for service statistics
    /// </summary>
    public class ServiceStats : IServiceStats
    {
        /// <summary>
        /// The name of the service
        /// </summary>
        public string ServiceName { get; }
        
        /// <summary>
        /// The current status of the service
        /// </summary>
        public string Status { get; }
        
        /// <summary>
        /// Service-specific counters and metrics
        /// </summary>
        public Dictionary<string, long> Counters { get; }
        
        /// <summary>
        /// The current entity being processed by the service
        /// </summary>
        public IFormattableObject CurrentEntity { get; }
        
        /// <summary>
        /// Creates a new instance of ServiceStats
        /// </summary>
        public ServiceStats(string serviceName, string status, IFormattableObject currentEntity, Dictionary<string, long> counters = null)
        {
            ServiceName = serviceName;
            Status = status;
            CurrentEntity = currentEntity;
            Counters = counters ?? new Dictionary<string, long>();
        }
    }
} 