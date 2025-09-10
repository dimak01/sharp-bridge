using System;
using System.Collections.Generic;
using SharpBridge.Interfaces.UI.Components;
using SharpBridge.Models;

namespace SharpBridge.Interfaces.Infrastructure.Services
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
        IFormattableObject? CurrentEntity { get; }

        /// <summary>
        /// Gets whether the service is currently healthy
        /// </summary>
        bool IsHealthy { get; }

        /// <summary>
        /// Gets the timestamp of the last successful operation
        /// </summary>
        DateTime LastSuccessfulOperation { get; }

        /// <summary>
        /// Gets the last error that occurred in the service
        /// </summary>
        string? LastError { get; }
    }
}