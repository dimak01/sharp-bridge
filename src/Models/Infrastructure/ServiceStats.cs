// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Infrastructure.Services;
using SharpBridge.Interfaces.UI.Components;

namespace SharpBridge.Models.Infrastructure
{
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
        public IFormattableObject? CurrentEntity { get; }

        /// <summary>
        /// Gets whether the service is currently healthy
        /// </summary>
        public bool IsHealthy { get; }

        /// <summary>
        /// Gets the timestamp of the last successful operation
        /// </summary>
        public DateTime LastSuccessfulOperation { get; }

        /// <summary>
        /// Gets the last error that occurred in the service
        /// </summary>
        public string? LastError { get; }

        /// <summary>
        /// Creates a new instance of ServiceStats
        /// </summary>
        public ServiceStats(
            string serviceName,
            string status,
            IFormattableObject? currentEntity,
            bool isHealthy = true,
            DateTime lastSuccessfulOperation = default,
            string? lastError = null,
            Dictionary<string, long>? counters = null)
        {
            ServiceName = serviceName;
            Status = status;
            CurrentEntity = currentEntity;
            IsHealthy = isHealthy;
            LastSuccessfulOperation = lastSuccessfulOperation;
            LastError = lastError;
            Counters = counters ?? new Dictionary<string, long>();
        }
    }
}