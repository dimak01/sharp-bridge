// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using SharpBridge.Models;

namespace SharpBridge.Interfaces.Infrastructure.Services
{
    /// <summary>
    /// Interface for components that provide service statistics
    /// </summary>
    public interface IServiceStatsProvider
    {
        /// <summary>
        /// Gets the current service statistics
        /// </summary>
        IServiceStats GetServiceStats();
    }
} 