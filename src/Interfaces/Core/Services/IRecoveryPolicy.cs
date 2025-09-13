// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;

namespace SharpBridge.Interfaces.Core.Services
{
    /// <summary>
    /// Defines the interface for recovery policies that determine when and how to attempt recovery
    /// </summary>
    public interface IRecoveryPolicy
    {
        /// <summary>
        /// Gets the next delay before attempting recovery
        /// </summary>
        /// <returns>The time to wait before the next recovery attempt</returns>
        TimeSpan GetNextDelay();
    }
} 