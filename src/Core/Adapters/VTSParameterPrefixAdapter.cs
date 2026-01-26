// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Core.Adapters;
using SharpBridge.Models;
using SharpBridge.Models.Configuration;
using SharpBridge.Models.Domain;

namespace SharpBridge.Core.Adapters
{
    /// <summary>
    /// Adapter that applies parameter prefixing to VTS parameters before sending to VTube Studio PC
    /// </summary>
    public class VTSParameterPrefixAdapter : IVTSParameterAdapter
    {
        private readonly VTubeStudioPCConfig _config;

        /// <summary>
        /// Creates a new instance of the VTSParameterPrefixAdapter
        /// </summary>
        /// <param name="config">VTube Studio PC configuration containing the parameter prefix</param>
        public VTSParameterPrefixAdapter(VTubeStudioPCConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);
            _config = config;
        }

        /// <summary>
        /// Adapts a collection of VTS parameters by applying the configured prefix to their names
        /// </summary>
        /// <param name="parameters">Original parameters</param>
        /// <param name="defaultParameterNames">Existing default parameter names</param>
        /// <returns>Adapted parameters with prefixed names ready for VTube Studio PC</returns>
        public IEnumerable<VTSParameter> AdaptParameters(IEnumerable<VTSParameter> parameters, IEnumerable<string> defaultParameterNames) => parameters.Select(p => AdaptParameter(p, defaultParameterNames));

        /// <summary>
        /// Adapts a single VTS parameter by applying the configured prefix to its name
        /// </summary>
        /// <param name="parameter">Original parameter</param>
        /// <param name="defaultParameterNames">Existing default parameters</param>
        /// <returns>Adapted parameter with prefixed name</returns>
        public VTSParameter AdaptParameter(VTSParameter parameter, IEnumerable<string> defaultParameterNames)
        {
            ArgumentNullException.ThrowIfNull(parameter);
            ArgumentNullException.ThrowIfNull(defaultParameterNames);

            if (defaultParameterNames.Contains(parameter.Name))
            {
                return parameter;
            }

            // Create a new VTSParameter with the prefixed name, preserving all other properties
            return new VTSParameter(
                AdaptParameterName(parameter.Name),
                parameter.Min,
                parameter.Max,
                parameter.DefaultValue
            );
        }

        /// <summary>
        /// Adapts a collection of tracking parameters by applying the configured prefix to their IDs.
        /// Creates new TrackingParam instances to avoid mutating the originals.
        /// </summary>
        /// <param name="trackingParams">Original tracking parameters.</param>
        /// <param name="defaultParameterNames">Existing default parameter names</param>
        /// <returns>Adapted tracking parameters with prefixed IDs, ready for VTube Studio PC.</returns>
        public IEnumerable<TrackingParam> AdaptTrackingParameters(IEnumerable<TrackingParam> trackingParams, IEnumerable<string> defaultParameterNames)
        {
            if (trackingParams == null)
            {
                return [];
            }

            return [.. trackingParams.Select(tp => new TrackingParam
            {
                Id = defaultParameterNames.Contains(tp.Id) ? tp.Id : AdaptParameterName(tp.Id),
                Value = tp.Value,
                Weight = tp.Weight
            })];
        }

        /// <summary>
        /// Adapts a parameter name by applying the configured prefix
        /// </summary>
        /// <param name="parameterName">Original parameter name</param>
        /// <returns>Adapted parameter name with prefixed name</returns>
        private string AdaptParameterName(string parameterName)
        {
            return _config.ParameterPrefix + parameterName;
        }
    }
}
