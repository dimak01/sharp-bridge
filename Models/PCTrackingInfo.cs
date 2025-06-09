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
        /// Parameter calculation expressions by ID, containing the original transformation expressions
        /// </summary>
        public IDictionary<string, string> ParameterCalculationExpressions { get; set; }
        
        /// <summary>
        /// Whether a face is detected
        /// </summary>
        public bool FaceFound { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PCTrackingInfo"/> class.
        /// </summary>
        public PCTrackingInfo()
        {
            Parameters = new List<TrackingParam>();
            ParameterDefinitions = new Dictionary<string, VTSParameter>();
            ParameterCalculationExpressions = new Dictionary<string, string>();
        }
    }
} 