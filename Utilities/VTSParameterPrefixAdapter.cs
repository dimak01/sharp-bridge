using System;
using System.Collections.Generic;
using System.Linq;
using SharpBridge.Interfaces;
using SharpBridge.Models;

namespace SharpBridge.Utilities
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
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Adapts a collection of VTS parameters by applying the configured prefix to their names
        /// </summary>
        /// <param name="parameters">Original parameters</param>
        /// <returns>Adapted parameters with prefixed names ready for VTube Studio PC</returns>
        public IEnumerable<VTSParameter> AdaptParameters(IEnumerable<VTSParameter> parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            // If no prefix is configured, return parameters unchanged
            if (string.IsNullOrEmpty(_config.ParameterPrefix))
            {
                return parameters;
            }

            return parameters.Select(AdaptParameter);
        }

        /// <summary>
        /// Adapts a single VTS parameter by applying the configured prefix to its name
        /// </summary>
        /// <param name="parameter">Original parameter</param>
        /// <returns>Adapted parameter with prefixed name</returns>
        public VTSParameter AdaptParameter(VTSParameter parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
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
        /// Adapts a parameter name by applying the configured prefix
        /// </summary>
        /// <param name="parameterName">Original parameter name</param>
        /// <returns>Adapted parameter name with prefixed name</returns>
        public string AdaptParameterName(string parameterName)
        {
            return _config.ParameterPrefix + parameterName;
        }
    }
}
