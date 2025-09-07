using System;

namespace SharpBridge.Models
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
