// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Models;
using SharpBridge.Models.Api;
using SharpBridge.Models.Domain;

namespace SharpBridge.Interfaces.Core.Managers
{
    /// <summary>
    /// Interface for managing VTube Studio PC parameters
    /// </summary>
    public interface IVTubeStudioPCParameterManager
    {
        /// <summary>
        /// Gets all existing parameters from VTube Studio
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of existing parameters</returns>
        Task<InputParameterListResponse> GetParametersAsync(CancellationToken cancellationToken);


        /// <summary>
        /// Attempts to synchronize the desired parameters with VTube Studio
        /// </summary>
        /// <param name="desiredParameters">Collection of parameters that should exist in VTube Studio</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if synchronization was successful, false if it failed</returns>
        Task<bool> TrySynchronizeParametersAsync(IEnumerable<VTSParameter> desiredParameters, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a parameter from VTube Studio
        /// </summary>
        /// <param name="parameter">Parameter to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if parameter was deleted successfully</returns>
        Task<bool> DeleteParameterAsync(VTSParameter parameter, CancellationToken cancellationToken);

    }
}