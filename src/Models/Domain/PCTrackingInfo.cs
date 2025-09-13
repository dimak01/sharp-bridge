// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.UI.Components;

namespace SharpBridge.Models.Domain
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
        /// Parameter extremums by ID, tracking min/max values observed during runtime
        /// </summary>
        public IDictionary<string, ParameterExtremums> ParameterExtremums { get; set; }

        /// <summary>
        /// Parameter interpolation information by ID, containing the interpolation method used
        /// </summary>
        public IDictionary<string, IInterpolationDefinition> ParameterInterpolations { get; set; }

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
            ParameterExtremums = new Dictionary<string, ParameterExtremums>();
            ParameterInterpolations = new Dictionary<string, IInterpolationDefinition>();
        }
    }
}