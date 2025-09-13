// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Models;
using SharpBridge.Models.Api;

namespace SharpBridge.Interfaces.Core.Services
{
    /// <summary>
    /// Service for discovering VTube Studio's WebSocket port via UDP broadcast
    /// </summary>
    public interface IPortDiscoveryService
    {
        /// <summary>
        /// Discovers the port VTube Studio is running on
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>The discovered port, or null if not found</returns>
        Task<DiscoveryResponse?> DiscoverAsync(int timeoutMs, CancellationToken cancellationToken);
    }
}