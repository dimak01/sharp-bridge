// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SharpBridge.Interfaces.Core.Services
{
    /// <summary>
    /// Service responsible for handling application initialization
    /// </summary>
    public interface IApplicationInitializationService
    {
        /// <summary>
        /// Initializes the application by setting up all required components
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <param name="preActions">List of actions to execute after console setup (defaults to empty list)</param>
        /// <param name="postActions">List of actions to execute during final setup phase (defaults to empty list)</param>
        /// <returns>A task that completes when initialization is done</returns>
        Task InitializeAsync(CancellationToken cancellationToken, List<Action>? preActions = null, List<Action>? postActions = null);
    }
}
