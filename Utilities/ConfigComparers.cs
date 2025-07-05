using SharpBridge.Models;

namespace SharpBridge.Utilities
{
    /// <summary>
    /// Static class providing explicit configuration comparison methods
    /// to avoid implicit reference type comparison
    /// </summary>
    public static class ConfigComparers
    {
        /// <summary>
        /// Compares two VTubeStudioPhoneClientConfig instances for equality
        /// </summary>
        /// <param name="x">First configuration to compare</param>
        /// <param name="y">Second configuration to compare</param>
        /// <returns>True if configurations are equal, false otherwise</returns>
        public static bool PhoneClientConfigsEqual(VTubeStudioPhoneClientConfig? x, VTubeStudioPhoneClientConfig? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.IphoneIpAddress == y.IphoneIpAddress &&
                   x.IphonePort == y.IphonePort &&
                   x.LocalPort == y.LocalPort &&
                   x.RequestIntervalSeconds == y.RequestIntervalSeconds &&
                   x.SendForSeconds == y.SendForSeconds &&
                   x.ReceiveTimeoutMs == y.ReceiveTimeoutMs &&
                   x.ErrorDelayMs == y.ErrorDelayMs;
        }

        /// <summary>
        /// Compares two VTubeStudioPCConfig instances for equality
        /// </summary>
        /// <param name="x">First configuration to compare</param>
        /// <param name="y">Second configuration to compare</param>
        /// <returns>True if configurations are equal, false otherwise</returns>
        public static bool PCClientConfigsEqual(VTubeStudioPCConfig? x, VTubeStudioPCConfig? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.Host == y.Host &&
                   x.Port == y.Port &&
                   x.PluginName == y.PluginName &&
                   x.PluginDeveloper == y.PluginDeveloper &&
                   x.TokenFilePath == y.TokenFilePath &&
                   x.ConnectionTimeoutMs == y.ConnectionTimeoutMs &&
                   x.ReconnectionDelayMs == y.ReconnectionDelayMs &&
                   x.UsePortDiscovery == y.UsePortDiscovery;
        }

        /// <summary>
        /// Compares two GeneralSettingsConfig instances for equality
        /// </summary>
        /// <param name="x">First configuration to compare</param>
        /// <param name="y">Second configuration to compare</param>
        /// <returns>True if configurations are equal, false otherwise</returns>
        public static bool GeneralSettingsEqual(GeneralSettingsConfig? x, GeneralSettingsConfig? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            // Compare EditorCommand
            if (x.EditorCommand != y.EditorCommand) return false;

            // Compare Shortcuts dictionary
            if (x.Shortcuts == null && y.Shortcuts == null) return true;
            if (x.Shortcuts == null || y.Shortcuts == null) return false;
            if (x.Shortcuts.Count != y.Shortcuts.Count) return false;

            foreach (var kvp in x.Shortcuts)
            {
                if (!y.Shortcuts.TryGetValue(kvp.Key, out var value) || value != kvp.Value)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Compares two TransformationEngineConfig instances for equality
        /// </summary>
        /// <param name="x">First configuration to compare</param>
        /// <param name="y">Second configuration to compare</param>
        /// <returns>True if configurations are equal, false otherwise</returns>
        public static bool TransformationEngineConfigsEqual(TransformationEngineConfig? x, TransformationEngineConfig? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.ConfigPath == y.ConfigPath &&
                   x.MaxEvaluationIterations == y.MaxEvaluationIterations;
        }
    }
}