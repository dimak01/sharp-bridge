// Copyright 2025 Dimak@Shift
// SPDX-License-Identifier: MIT

using System;

namespace SharpBridge.Models.Configuration
{
    /// <summary>
    /// Result of loading a configuration with migration information
    /// </summary>
    /// <typeparam name="T">The configuration type</typeparam>
    public record ConfigLoadResult<T>(
        T Config,
        bool WasCreated,
        bool WasMigrated,
        int OriginalVersion
    ) where T : class;
}
