using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SharpBridge.Models;

namespace SharpBridge.Interfaces
{
    /// <summary>
    /// Interface for transforming tracking data into VTube Studio parameters
    /// </summary>
    public interface ITransformationEngine
    {
        /// <summary>
        /// Loads transformation rules from the specified file
        /// </summary>
        /// <param name="filePath">Path to the transformation rules JSON file</param>
        /// <returns>An asynchronous operation that completes when rules are loaded</returns>
        Task LoadRulesAsync(string filePath);
        
        /// <summary>
        /// Transforms tracking data into VTube Studio parameters
        /// </summary>
        /// <param name="trackingData">The tracking data to transform</param>
        /// <returns>Collection of transformed parameters</returns>
        IEnumerable<TrackingParam> TransformData(TrackingResponse trackingData);
    }
} 