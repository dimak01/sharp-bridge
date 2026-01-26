// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using SharpBridge.Models.Domain;

namespace SharpBridge.Interfaces.Core.Adapters
{
    /// <summary>
    /// Interface for adapting VTS parameters before sending to VTube Studio PC
    /// </summary>
    public interface IVTSParameterAdapter
    {
        /// <summary>
        /// Adapts a collection of VTS parameters by applying transformations (like prefixing)
        /// </summary>
        /// <param name="parameters">Original parameters</param>
        /// <param name="defaultParameterNames">Existing default parameter names</param>
        /// <returns>Adapted parameters ready for VTube Studio PC</returns>
        IEnumerable<VTSParameter> AdaptParameters(IEnumerable<VTSParameter> parameters, IEnumerable<string> defaultParameterNames);

        /// <summary>
        /// Adapts a collection of tracking parameters by applying transformations (like prefixing) to their IDs
        /// </summary>
        /// <param name="trackingParams">Original tracking parameters</param>
        /// <param name="defaultParameterNames">Existing default parameter names</param>
        /// <returns>Adapted tracking parameters ready for VTube Studio PC</returns>
        IEnumerable<TrackingParam> AdaptTrackingParameters(IEnumerable<TrackingParam> trackingParams, IEnumerable<string> defaultParameterNames);

        /// <summary>
        /// Adapts a single VTS parameter by applying transformations (like prefixing)
        /// </summary>
        /// <param name="parameter">Original parameter</param>
        /// <param name="defaultParameterNames">Existing default parameter names</param>
        /// <returns>Adapted parameter ready for VTube Studio PC</returns>
        VTSParameter AdaptParameter(VTSParameter parameter, IEnumerable<string> defaultParameterNames);
    }
}
