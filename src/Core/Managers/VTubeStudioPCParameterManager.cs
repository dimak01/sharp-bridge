// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Interfaces;
using SharpBridge.Interfaces.Core.Adapters;
using SharpBridge.Interfaces.Core.Managers;
using SharpBridge.Interfaces.Infrastructure.Services;
using SharpBridge.Interfaces.Infrastructure.Wrappers;
using SharpBridge.Models;
using SharpBridge.Models.Api;
using SharpBridge.Models.Domain;

namespace SharpBridge.Core.Managers
{
    /// <summary>
    /// Manages VTube Studio PC parameters
    /// </summary>
    public class VTubeStudioPCParameterManager : IVTubeStudioPCParameterManager
    {
        private readonly IWebSocketWrapper _webSocket;
        private readonly IAppLogger _logger;
        private readonly IVTSParameterAdapter _parameterAdapter;
        /// <summary>
        /// Creates a new instance of the VTubeStudioPCParameterManager
        /// </summary>
        /// <param name="webSocket">WebSocket wrapper for communication with VTube Studio</param>
        /// <param name="logger">Application logger</param>
        /// <param name="parameterAdapter">Adapter for transforming parameters before sending to VTube Studio</param>
        public VTubeStudioPCParameterManager(IWebSocketWrapper webSocket, IAppLogger logger, IVTSParameterAdapter parameterAdapter)
        {
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _parameterAdapter = parameterAdapter ?? throw new ArgumentNullException(nameof(parameterAdapter));
        }

        /// <summary>
        /// Gets all existing parameters from VTube Studio
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of existing parameters</returns>
        public async Task<InputParameterListResponse> GetParametersAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await _webSocket.SendRequestAsync<InputParameterListRequest, InputParameterListResponse>(
                    "InputParameterListRequest", new InputParameterListRequest(), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to get parameters: {0}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Creates or updates a parameter in VTube Studio
        /// </summary>
        /// <param name="parameter">Parameter to create or update</param>
        /// <param name="isUpdate">Whether this is an update operation</param>
        /// <param name="defaultParameterNames">Existing default parameter names</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if operation was successful</returns>
        private async Task<bool> CreateOrUpdateParameterAsync(VTSParameter parameter,
                                                                bool isUpdate,
                                                                IEnumerable<string> defaultParameterNames,
                                                                CancellationToken cancellationToken)
        {
            try 
            {
                if (defaultParameterNames.Contains(parameter.Name))
                {
                    _logger.Warning("Cannot create or update default VTS parameter: {0}", parameter.Name);
                    return false;
                }

                var request = new ParameterCreationRequest
                {
                    ParameterName = parameter.Name,
                    Explanation = "Custom parameter created by SharpBridge",
                    Min = parameter.Min,
                    Max = parameter.Max,
                    DefaultValue = parameter.DefaultValue
                };

                var response = await _webSocket.SendRequestAsync<ParameterCreationRequest, ParameterCreationResponse>(
                    "ParameterCreationRequest", request, cancellationToken);

                return response.ParameterName == parameter.Name;
            }
            catch (Exception ex)
            {
                var operation = isUpdate ? "update" : "create";
                _logger.Error("Failed to {0} parameter {1}: {2}", operation, parameter.Name, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Deletes a parameter from VTube Studio
        /// </summary>
        /// <param name="parameterName">Name of the parameter to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if parameter was deleted successfully</returns>
        public async Task<bool> DeleteParameterAsync(VTSParameter parameter, CancellationToken cancellationToken)
        {
            try
            {
                var parametersInfo = await GetExistingParametersAsync(cancellationToken);
                var existingDefaultParameterNames = new HashSet<string>(parametersInfo.DefaultParameters.Select(p => p.Name));
                var adaptedParameter = _parameterAdapter.AdaptParameter(parameter, existingDefaultParameterNames);

                if (existingDefaultParameterNames.Contains(adaptedParameter.Name))
                {
                    _logger.Warning("Cannot delete default VTS parameter: {0}", adaptedParameter.Name);
                    return false;
                }

                var request = new ParameterDeletionRequest
                {
                    ParameterName = adaptedParameter.Name
                };

                await _webSocket.SendRequestAsync<ParameterDeletionRequest, object>(
                    "ParameterDeletionRequest", request, cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to delete parameter {0}: {1}", parameter.Name, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Attempts to synchronize the desired parameters with VTube Studio
        /// </summary>
        /// <param name="desiredParameters">Collection of parameters that should exist in VTube Studio</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if synchronization was successful, false if it failed</returns>
        public async Task<bool> TrySynchronizeParametersAsync(IEnumerable<VTSParameter> desiredParameters, CancellationToken cancellationToken)
        {
            try
            {
                // Apply parameter adapter to get prefixed parameters for VTube Studio

                var parametersInfo = await GetExistingParametersAsync(cancellationToken);

                var existingCustomParameterNames = new HashSet<string>(parametersInfo.CustomParameters.Select(p => p.Name));
                var existingDefaultParameterNames = new HashSet<string>(parametersInfo.DefaultParameters.Select(p => p.Name));

                var adaptedParameters = _parameterAdapter.AdaptParameters(desiredParameters, existingDefaultParameterNames);

                foreach (var parameter in adaptedParameters)
                {
                    if (existingDefaultParameterNames.Contains(parameter.Name))
                    {
                        _logger.Error("Parameter {0} was found in default parameters, skipping", parameter.Name);
                        continue;
                    }

                    if (existingCustomParameterNames.Contains(parameter.Name))
                    {
                        var updateSuccess = await CreateOrUpdateParameterAsync(parameter, isUpdate: true, existingDefaultParameterNames, cancellationToken);
                        if (!updateSuccess)
                        {
                            _logger.Error("Failed to update parameter: {0}", parameter.Name);
                            return false;
                        }
                    }
                    else
                    {
                        var createSuccess = await CreateOrUpdateParameterAsync(parameter, isUpdate: false, existingDefaultParameterNames, cancellationToken);
                        if (!createSuccess)
                        {
                            _logger.Error("Failed to create parameter: {0}", parameter.Name);
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to synchronize parameters: {0}", ex.Message);
                return false;
            }
        }

        private async Task<InputParameterListResponse> GetExistingParametersAsync(CancellationToken cancellationToken)
        {
            var existingParameters = await GetParametersAsync(cancellationToken);
            return existingParameters;
        }
    }
}