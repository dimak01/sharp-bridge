using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharpBridge.Interfaces.Infrastructure.Services;
using SharpBridge.Models;
using SharpBridge.Models.Domain;

namespace SharpBridge.Interfaces.Core.Engines
{
    /// <summary>
    /// Interface for transforming tracking data into VTube Studio parameters
    /// </summary>
    public interface ITransformationEngine : IServiceStatsProvider
    {
        /// <summary>
        /// Gets whether the configuration has changed (for testing purposes)
        /// </summary>
        bool ConfigChanged { get; }

        /// <summary>
        /// Gets whether the currently loaded configuration is up to date with the file on disk
        /// </summary>
        bool IsConfigUpToDate { get; }

        /// <summary>
        /// Loads transformation rules from the configured file path
        /// </summary>
        /// <returns>An asynchronous operation that completes when rules are loaded</returns>
        Task LoadRulesAsync();

        /// <summary>
        /// Transforms tracking data into VTube Studio parameters according to loaded rules
        /// </summary>
        /// <param name="trackingData">The tracking data to transform</param>
        /// <returns>Collection of transformed parameters</returns>
        PCTrackingInfo TransformData(PhoneTrackingInfo trackingData);

        /// <summary>
        /// Gets the parameter definitions for all loaded transformation rules
        /// </summary>
        /// <returns>Collection of parameter definitions</returns>
        IEnumerable<VTSParameter> GetParameterDefinitions();
    }
}