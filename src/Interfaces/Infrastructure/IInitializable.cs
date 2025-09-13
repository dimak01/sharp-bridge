// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System.Threading;
using System.Threading.Tasks;

namespace SharpBridge.Interfaces.Infrastructure
{
    /// <summary>
    /// Interface for components that can be initialized
    /// </summary>
    public interface IInitializable
    {
        /// <summary>
        /// Attempts to initialize the component
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>True if initialization was successful</returns>
        Task<bool> TryInitializeAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the last error that occurred during initialization
        /// </summary>
        string LastInitializationError { get; }
    }
}