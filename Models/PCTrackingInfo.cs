using System.Collections.Generic;
using SharpBridge.Interfaces;

namespace SharpBridge.Models
{
    /// <summary>
    /// Complete tracking data for VTube Studio PC, including both runtime values and parameter definitions
    /// </summary>
    public class PCTrackingInfo : IFormattableObject
    {
        /// <summary>
        /// Current parameter values for VTube Studio PC
        /// </summary>
        public IEnumerable<TrackingParam> Parameters { get; set; }
        
        /// <summary>
        /// Parameter definitions by ID, containing creation metadata and bounds
        /// </summary>
        public IDictionary<string, VTSParameter> ParameterDefinitions { get; set; }
        
        /// <summary>
        /// Whether a face is detected
        /// </summary>
        public bool FaceFound { get; set; }

        public PCTrackingInfo()
        {
            Parameters = new List<TrackingParam>();
            ParameterDefinitions = new Dictionary<string, VTSParameter>();
        }

        /// <summary>
        /// Gets the full parameter definition for a given tracking parameter
        /// </summary>
        /// <param name="paramId">The parameter ID to look up</param>
        /// <returns>The parameter definition if found, null otherwise</returns>
        public VTSParameter GetDefinition(string paramId)
        {
            return ParameterDefinitions.TryGetValue(paramId, out var definition) ? definition : null;
        }
    }
} 