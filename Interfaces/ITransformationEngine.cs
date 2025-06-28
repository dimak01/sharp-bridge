using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for transforming tracking data into VTube Studio parameters
    /// </summary>
    public interface ITransformationEngine : IServiceStatsProvider
    {
        /// <summary>
        /// Loads transformation rules from the configured file path
        /// </summary>
        /// <returns>An asynchronous operation that completes when rules are loaded</returns>
        Task LoadRulesAsync();

        /// <summary>
        /// Transforms tracking data into VTube Studio parameters
        /// </summary>
        /// <param name="trackingData">The tracking data to transform</param>
        /// <returns>Collection of transformed parameters</returns>
        PCTrackingInfo TransformData(PhoneTrackingInfo trackingData);

        /// <summary>
        /// Gets all parameters defined in the loaded transformation rules
        /// </summary>
        /// <returns>Collection of parameter definitions</returns>
        IEnumerable<VTSParameter> GetParameterDefinitions();
    }
}